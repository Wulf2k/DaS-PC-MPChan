Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Reflection
Imports System.Linq

''' <summary>
''' SortableBindingList is a list that supports sorting its items and filtering them.
''' When binding a <see cref="System.Collections.Generic.List(Of T)"/> to a <see cref="System.Windows.Forms.DataGridView"/>, you can not sort by clicking on the columns
''' or filter the list. With this list, you can.
''' 
''' Dependencies:
'''  - .NET 3.5 or higher
'''  - System.Linq.Dynamic (DynamicQuery NuGet package: http://www.nuget.org/packages/DynamicQuery/)
''' </summary>
''' <typeparam name="T">The data type represented by this SortableBindingList</typeparam>
''' <remarks></remarks>
Public Class SortableBindingList(Of T)
    Inherits BindingList(Of T)
    Implements IBindingListView


    Private _isSorted As Boolean = False
    Private _listSortDescriptors As IEnumerable(Of ListSortDescription)
    Private ReadOnly _originalData As List(Of T)

    ''' <summary>
    ''' Creates a new instance of SortableBindingList and populates it with the contents of the given list.
    ''' </summary>
    ''' <param name="list"></param>
    ''' <remarks></remarks>
    Public Sub New(list As IEnumerable(Of T))
        MyBase.New(list.ToList()) 'BindingList(Of T) requires an IList(Of T)

        _originalData = New List(Of T)(list)
    End Sub

    Public Sub AddRange(collection As IEnumerable(Of T))
        For Each tItem In collection
            Items.Add(tItem)
        Next

        _originalData.AddRange(collection)
    End Sub

    Protected Overrides Sub ApplySortCore(ByVal prop As PropertyDescriptor, ByVal direction As ListSortDirection)
        ' Check to see if the property type we are sorting by implements the IComparable interface.
        Dim interfaceType As Type = prop.PropertyType.GetInterface("IComparable")

        If interfaceType Is Nothing Then
            'Check if this is a Nullable(Of IComparable)
            If prop.PropertyType.IsGenericType AndAlso prop.PropertyType.GetGenericTypeDefinition() Is GetType(Nullable(Of )) Then
                'Check if the type parameter implements IComparable
                Dim tType As Type = prop.PropertyType.GetGenericArguments()(0)
                interfaceType = tType.GetInterface("IComparable")
            End If
        End If

        If interfaceType IsNot Nothing Then
            'If so, set the SortPropertyValue and SortDirectionValue.
            _listSortDescriptors = New List(Of ListSortDescription)({ _
                New ListSortDescription(prop, direction) _
            })

            'Sort the list using LINQ (OrderBy).
            Dim tempList = Items
            If direction = ListSortDirection.Ascending Then
                tempList = tempList.OrderBy(Function(x) prop.GetValue(x)).ToList()
            Else
                tempList = tempList.OrderByDescending(Function(x) prop.GetValue(x)).ToList()
            End If

            'Copy the sorted items back into the list.
            Items.Clear()
            For Each tItem In tempList
                Items.Add(tItem)
            Next

            _isSorted = True

            'Raise the ListChanged event so bound controls refresh their values.
            OnListChanged(New ListChangedEventArgs(ListChangedType.Reset, -1))
        Else
            'If the property type does not implement IComparable, let the user know.
            Throw New NotSupportedException(String.Format("Cannot sort by {0}. {1} does not implement IComparable.", prop.Name, _
                prop.PropertyType.ToString()))
        End If
    End Sub

    Protected Overloads Sub ApplySortCore(ByVal sorts As IEnumerable(Of ListSortDescription), Optional ByVal informListeners As Boolean = True)
        Try
            Dim orderedList As IOrderedEnumerable(Of T) = Nothing
            Dim bFirstOrder As Boolean = True
            For Each sortDescriptor As ListSortDescription In sorts
                Dim prop As PropertyDescriptor = sortDescriptor.PropertyDescriptor
                Dim interfaceType As Type = prop.PropertyType.GetInterface("IComparable")

                If interfaceType Is Nothing Then
                    'Check if this is a Nullable(Of IComparable)
                    If prop.PropertyType.IsGenericType AndAlso prop.PropertyType.GetGenericTypeDefinition() Is GetType(Nullable(Of )) Then
                        'Check if the type parameter implements IComparable
                        Dim tType As Type = prop.PropertyType.GetGenericArguments()(0)
                        interfaceType = tType.GetInterface("IComparable")
                    End If
                End If

                If interfaceType Is Nothing Then
                    'If the property type does not implement IComparable, let the user know.
                    Throw New NotSupportedException(String.Format("Cannot sort by {0}. {1} does not implement IComparable.", prop.Name, _
                        prop.PropertyType.ToString()))
                End If

                'Sort the list using LINQ (OrderBy/ThenBy). Remember if the sort operation is the first sort or not (OrderBy vs ThenBy).
                If bFirstOrder Then
                    bFirstOrder = False
                    If sortDescriptor.SortDirection = ListSortDirection.Ascending Then
                        orderedList = Items.OrderBy(Function(x) prop.GetValue(x))
                    Else
                        orderedList = Items.OrderByDescending(Function(x) prop.GetValue(x))
                    End If
                Else
                    If sortDescriptor.SortDirection = ListSortDirection.Ascending Then
                        orderedList = orderedList.ThenBy(Function(x) prop.GetValue(x))
                    Else
                        orderedList = orderedList.ThenByDescending(Function(x) prop.GetValue(x))
                    End If
                End If
            Next

            'Sorting succeeded
            Dim result = orderedList.ToList()

            'Copy the sorted items back into the list.
            Items.Clear()
            For Each tItem In result
                Items.Add(tItem)
            Next

            _isSorted = True
            _listSortDescriptors = sorts

            'Most of the times, informListeners will be true. In rare cases, this function is called from EndNew, and then the OnListChanged event should not be fired.
            If informListeners Then
                'Raise the ListChanged event so bound controls refresh their values.
                OnListChanged(New ListChangedEventArgs(ListChangedType.Reset, -1))
            End If
        Catch
            'Reset the list
            Items.Clear()
            For Each tItem As T In _originalData
                Items.Add(tItem)
            Next

            'Rethrow the error
            Throw
        End Try
    End Sub

    Protected Overrides Sub RemoveSortCore()
        If Not _isSorted Then Return

        'Restore original order
        Items.Clear()
        For Each tItem As T In _originalData
            Items.Add(tItem)
        Next
        _isSorted = False
        OnListChanged(New ListChangedEventArgs(ListChangedType.Reset, -1))
    End Sub

    Public Sub RemoveSort()
        RemoveSortCore()
    End Sub

    Public Function Find(Of TKey)(ByVal [property] As String, ByVal key As TKey) As Integer
        ' Check the properties for a property with the specified name.
        Dim properties As PropertyDescriptorCollection = TypeDescriptor.GetProperties(GetType(T))
        Dim prop As PropertyDescriptor = properties.Find([property], True)

        ' If there is not a match, return -1 otherwise pass search to FindCore method.
        If prop Is Nothing Then Return -1
        Return FindCore(prop, key)
    End Function

    Protected Overrides Function FindCore(ByVal prop As PropertyDescriptor, ByVal key As Object) As Integer
        'Get the property info for the specified property.
        Dim propInfo As PropertyInfo = GetType(T).GetProperty(prop.Name)
        Dim tItem As T

        If key IsNot Nothing Then
            'Loop through the items to see if the key
            ' value matches the property value.
            For i As Integer = 0 To Count - 1
                tItem = Items(i)
                If (propInfo.GetValue(tItem, Nothing).Equals(key)) Then Return i
            Next
        End If

        Return -1
    End Function

    Public Overrides Sub EndNew(itemIndex As Integer)
        Try
            'Check to see if the item is added to the end of the list,
            'and if so, re-sort the list.
            If _isSorted And itemIndex = Count - 1 Then
                'Reapply the sort, but do not inform listeners, because this would reset the position.
                ApplySortCore(_listSortDescriptors, False)
            End If
        Finally
            MyBase.EndNew(itemIndex)
        End Try
    End Sub

    Protected Overrides ReadOnly Property SupportsSearchingCore() As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property SupportsSortingCore() As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property IsSortedCore() As Boolean
        Get
            Return _isSorted
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

    Public Property Filter() As String Implements IBindingListView.Filter
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property

    Public ReadOnly Property SortDescriptions() As ListSortDescriptionCollection Implements IBindingListView.SortDescriptions
        Get
            Return New ListSortDescriptionCollection(_listSortDescriptors.ToArray())
        End Get
    End Property

    Public ReadOnly Property SupportsAdvancedSorting() As Boolean Implements IBindingListView.SupportsAdvancedSorting
        Get
            Return True
        End Get
    End Property

    Public ReadOnly Property SupportsFiltering() As Boolean Implements IBindingListView.SupportsFiltering
        Get
            Return False
        End Get
    End Property

    Public Sub ApplySort(ByVal sorts As ListSortDescriptionCollection) Implements IBindingListView.ApplySort
        ApplySortCore(sorts.Cast(Of ListSortDescription)())
    End Sub

    Public Sub RemoveFilter() Implements IBindingListView.RemoveFilter

    End Sub
End Class