﻿Imports System.Threading
Imports System.IO
Imports System.Net.Sockets
Imports System.Text.RegularExpressions
Imports System.Web.Script.Serialization
Imports System.Net.Http

Public Class NetClient
    Private mainWindow As MainWindow
    Public netNodes As New Dictionary(Of String, DSNode)
    Private client As HttpClient

    Public Sub New(mainWindow As MainWindow)
        Dim handler = New HttpClientHandler()
        handler.AutomaticDecompression = Net.DecompressionMethods.GZip Or Net.DecompressionMethods.Deflate
        client = New HttpClient(handler)
        Me.mainWindow = mainWindow
    End Sub

    Public Function GetNodeForConnecting(self As DSNode, blacklist As IEnumerable(Of String)) As DSNode
        Dim blackSet As New HashSet(Of String)(blacklist)
        blackSet.Add(self.SteamId)

        Dim candidates As New List(Of DSNode)
        For Each node In netNodes.Values
            If blackSet.Contains(node.SteamId) Then Continue For
            candidates.Add(node)
        Next

        If candidates.Count = 0 Then Return Nothing

        Dim sorted As IOrderedEnumerable(Of DSNode) = candidates _
            .OrderByDescending(Function(n) (n.MPZone = self.MPZone) AndAlso self.canCoop(n)) _
            .ThenByDescending(Function(n) (n.World = self.World) AndAlso self.canCoop(n)) _
            .ThenByDescending(Function(n) (n.MPZone = self.MPZone) OrElse self.canCoop(n)) _
            .ThenByDescending(Function(n) (n.World <> "-1--1")) _
            .ThenBy(Function(n) Math.Abs(n.SoulLevel - self.SoulLevel))

        Return sorted(0)
    End Function
    Private Function JSONContent(data As Object) As StringContent
        Dim serializer As New JavaScriptSerializer()
        serializer.RegisterConverters({New DSNodeSerializer()})
        Dim str As String = serializer.Serialize(data)
        Dim content As New StringContent(str, New System.Text.UTF8Encoding())
        content.Headers.ContentType = New Headers.MediaTypeHeaderValue("application/json")
        Return content
    End Function
    Public Async Function publishLocalNodes(self As DSNode, nodes As IEnumerable(Of DSNode)) As Task
        Dim data As New Dictionary(Of String, Object)() From {
            {"self", self},
            {"nodes", nodes}
        }
        Dim content = JSONContent(data)
        Try
            Dim response As HttpResponseMessage = Await client.PostAsync("http://dscm-net.chronial.de:8811/store", content)
            response.EnsureSuccessStatusCode()
        Catch ex As Exception
            setStatus("Error publishing local nodes: " & ex.Message)
        End Try
    End Function
    Public Async Function loadNodes() As Task(Of IEnumerable(Of DSNode))
        Try
            Dim response As HttpResponseMessage = Await client.GetAsync("http://dscm-net.chronial.de:8811/list")
            response.EnsureSuccessStatusCode()
            Dim content = Await response.Content.ReadAsStringAsync()
            Dim serializer As New JavaScriptSerializer()
            Dim dsNodeSer As New DSNodeSerializer()
            Dim data As IDictionary(Of String, Object) = serializer.Deserialize(Of Dictionary(Of String, Object))(content)
            Dim nodes As IEnumerable = data("nodes")
            netNodes = New Dictionary(Of String, DSNode)
            For Each n As Dictionary(Of String, Object) In nodes
                Dim node As DSNode = dsNodeSer.NodeFromDict(n)
                netNodes(node.SteamId) = node
            Next
        Catch ex As Exception
            setStatus("Error loading node list: " & ex.Message)
        End Try
        Return Nothing
    End Function
    Private Sub setStatus(status As String)
        If mainWindow.InvokeRequired Then
            mainWindow.Invoke(New setStatusDelegate(AddressOf setStatus), {status})
        Else
            mainWindow.txtIRCDebug.Text = status
        End If
    End Sub
    Private Delegate Sub setStatusDelegate(status As String)
End Class



Public Class DSNodeSerializer
    Inherits JavaScriptConverter

    Public Function NodeFromDict(dict As IDictionary(Of String, Object)) As DSNode
        Dim node As New DSNode()
        node.SteamId = dict("steamid")
        node.CharacterName = dict("name")
        node.SoulLevel = dict("sl")
        node.PhantomType = dict("phantom_type")
        node.MPZone = dict("mp_zone")
        node.World = dict("world")
        If dict.ContainsKey("covenant") Then
            node.Covenant = dict("covenant")
            node.Indictments = dict("indictments")
        End If
        Return node
    End Function
    Public Overrides Function Deserialize(dict As IDictionary(Of String, Object), type As Type, serializer As JavaScriptSerializer) As Object
        Return NodeFromDict(dict)
    End Function
    Public Overrides Function Serialize(obj As Object, serializer As JavaScriptSerializer) As IDictionary(Of String, Object)
        Dim node As DSNode = CType(obj, DSNode)
        Dim out As New Dictionary(Of String, Object)
        out("steamid") = node.SteamId
        out("name") = node.CharacterName
        out("sl") = node.SoulLevel
        out("phantom_type") = node.PhantomType
        out("mp_zone") = node.MPZone
        out("world") = node.World
        If node.HasExtendedInfo Then
            out("covenant") = node.Covenant
            out("indictments") = node.Indictments
        End If
        Return out
    End Function
    Public Overrides ReadOnly Property SupportedTypes As IEnumerable(Of Type)
        Get
            Return New List(Of Type)({GetType(DSNode)})
        End Get
    End Property
End Class