Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Reflection
Imports System.Linq

Class SortComparer(Of T)
    Implements IComparer(Of T)
    Private _sorts As IEnumerable(Of ListSortDescription)
    Public Sub New(sorts As IEnumerable(Of ListSortDescription))
        _sorts = sorts
    End Sub
    Public Function Compare(x As T, y As T) As Integer Implements IComparer(Of T).Compare
        For Each sort In _sorts
            Dim prop As PropertyDescriptor = sort.PropertyDescriptor
            Dim sortProp = TypeDescriptor.GetProperties(GetType(T)).Find(prop.Name + "Sort", True)
            prop = If(sortProp, prop)
            Dim cmp = DirectCast(prop.GetValue(x), IComparable).CompareTo(prop.GetValue(y))
            If cmp <> 0 Then
                If sort.SortDirection = ListSortDirection.Descending Then
                    cmp *= -1
                End If
                Return cmp
            End If
        Next
        Return 0
    End Function

End Class

Public Class SortableBindingList(Of T)
    Inherits BindingList(Of T)

    Private _listSortDescriptors As New List(Of ListSortDescription)

    Public Sub ReplaceSorted(oldIndex As Integer, item As T)
        Me.Items.RemoveAt(oldIndex)
        
        Dim comparer As New SortComparer(Of T)(_listSortDescriptors)
        Dim index = Items.ToList().BinarySearch(item, comparer)
        If index < 0 Then
            index = index Xor -1
        End If
        Me.Items.Insert(index, item)
        Me.OnListChanged(New ListChangedEventArgs(ListChangedType.ItemMoved, index, oldIndex))
    End Sub

    Public Function InsertSorted(item As T) As Integer
        Dim comparer As New SortComparer(Of T)(_listSortDescriptors)
        Dim index = Items.ToList().BinarySearch(item, comparer)
        If index < 0 Then
            index = index Xor -1
        End If
        Me.InsertItem(index, item)
        Return index
    End Function

    Protected Overrides Sub ApplySortCore(ByVal prop As PropertyDescriptor, ByVal direction As ListSortDirection)
        Dim newSort As New List(Of ListSortDescription)()
        newSort.Add(New ListSortDescription(prop, direction))
        For Each sortDesc As ListSortDescription In _listSortDescriptors
            If sortDesc.PropertyDescriptor.Name <> prop.Name Then
                newSort.Add(sortDesc)
            End If
        Next
        ApplySortCore(newSort)
    End Sub

    Protected Overloads Sub ApplySortCore(sorts As IEnumerable(Of ListSortDescription), Optional informListeners As Boolean = True)
        Dim comparer As New SortComparer(Of T)(sorts)
        Dim orderedList As IOrderedEnumerable(Of T) = Items.OrderBy(Function(x) x, comparer)
        Dim result = orderedList.ToList()

        'Copy the sorted items back into the list.
        Items.Clear()
        For Each tItem In result
            Items.Add(tItem)
        Next

        _listSortDescriptors = sorts

        'Most of the times, informListeners will be true. In rare cases, this function is called from EndNew, and then the OnListChanged event should not be fired.
        If informListeners Then
            'Raise the ListChanged event so bound controls refresh their values.
            OnListChanged(New ListChangedEventArgs(ListChangedType.Reset, -1))
        End If
    End Sub


    Protected Overrides ReadOnly Property SupportsSortingCore() As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property IsSortedCore() As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property SortPropertyCore() As PropertyDescriptor
        Get
            If _listSortDescriptors Is Nothing OrElse _listSortDescriptors.Count = 0 Then Return Nothing
            Return _listSortDescriptors(0).PropertyDescriptor
        End Get
    End Property

    Protected Overrides ReadOnly Property SortDirectionCore() As ListSortDirection
        Get
            If _listSortDescriptors Is Nothing OrElse _listSortDescriptors.Count = 0 Then Return Nothing
            Return _listSortDescriptors(0).SortDirection
        End Get
    End Property
End Class

Public Class DSNodeBindingList
    Inherits SortableBindingList(Of DSNode)
    Public Sub SyncWithDict(dict As IDictionary(Of String, DSNode), _
                            Optional dgv As DataGridView = Nothing)
        SyncWithDict(dict, Function(x) x, dgv)
    End Sub
    Public Sub SyncWithDict(Of T)(dict As IDictionary(Of String, T), _
                            getValue As Func(Of T, DSNode), _
                            Optional dgv As DataGridView = Nothing)
        ' If dgv is passed, its currently displayed top row is fixed in place
        Dim firstDisplayRow As Integer = 0
        If dgv IsNot Nothing Then firstDisplayRow = dgv.FirstDisplayedScrollingRowIndex
        Dim seenNodes As New HashSet(Of String)
        Dim i As Integer = -1
        While i < Me.Count - 1
            i += 1
            Dim node As DSNode = Me(i)
            If Not dict.ContainsKey(node.SteamId) Then
                Me.RemoveAt(i)
                i -= 1
                If i < firstDisplayRow Then firstDisplayRow -= 1
            Else
                seenNodes.Add(node.SteamId)
                Dim otherNode As DSNode = getValue(dict(node.SteamId))
                If Not node.MemberwiseEquals(otherNode) Then
                    Me.ReplaceSorted(i, otherNode)
                    i -= 1
                End If
            End If
        End While
        For Each t In dict.Values
            If Not seenNodes.Contains(getValue(t).SteamId) Then
                Dim index = Me.InsertSorted(getValue(t))
                If index < firstDisplayRow Then firstDisplayRow += 1
            End If
        Next
        If dgv IsNot Nothing And firstDisplayRow >= 0 And firstDisplayRow < Me.Count Then
            dgv.FirstDisplayedScrollingRowIndex = firstDisplayRow
        End If
    End Sub
    Private Sub moved(sender As Object, e As ListChangedEventArgs) Handles Me.ListChanged
        If e.ListChangedType = ListChangedType.ItemMoved Then
            If Me.Items(e.NewIndex).CharacterName = "Chronial" Or Me.Items(e.OldIndex).CharacterName = "Chronial" Then
                Dim x = 5

            End If
            End If
    End Sub
End Class


Public Class ExtendedDataGridView
    Inherits DataGridView
    Private Sub listenToMoves() Handles Me.DataSourceChanged
        AddHandler DirectCast(Me.DataSource, IBindingList).ListChanged, AddressOf rowMoved
    End Sub
    Private Sub rowMoved(sender As Object, e As ListChangedEventArgs)
        If e.ListChangedType = ListChangedType.ItemMoved Then
            Dim selectedIndex = Me.CurrentCell.RowIndex
            If e.OldIndex = selectedIndex Then
                If e.OldIndex <> e.NewIndex Then
                    selectedIndex = e.NewIndex - 1
                End If
            ElseIf e.NewIndex <> selectedIndex  Then
                If (e.NewIndex > selectedIndex) <> (e.OldIndex > selectedIndex) Then
                    If e.NewIndex > selectedIndex Then
                        selectedIndex -= 1
                    Else
                        selectedIndex += 1
                    End If
                End If
            End If
            Me.CurrentCell = Me.Rows(selectedIndex).Cells(Me.CurrentCell.ColumnIndex)
        End If
    End Sub
End Class