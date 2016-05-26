Imports System.Threading
Imports System.IO
Imports System.Net.Sockets
Imports System.Collections.Concurrent
Imports System.Text.RegularExpressions

Class IRCConnectionError
    Inherits System.ApplicationException

    Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class IRCClient
    Private mainWindow As MainWindow

    Private _thread As Thread
    Private tcpClient As System.Net.Sockets.TcpClient
    Private stream As NetworkStream
    Private _streamWriter As StreamWriter = Nothing
    Private _streamReader As StreamReader = Nothing

    Private shouldQuit As Boolean = False

    Private localNodesLock As New Object()
    Private localNodes As New List(Of DSNode)
    Private selfNode As DSNode
    Public ircNodes As New ConcurrentDictionary(Of String, Tuple(Of DSNode, Date))

    Public Sub New(mainWindow As MainWindow)
        Me.mainWindow = mainWindow
        _thread = New Thread(AddressOf main)
        _thread.IsBackground = True
        _thread.Start()
    End Sub

    Public Sub setLocalNodes(self As DSNode, nodes As IEnumerable(Of DSNode))
        SyncLock localNodesLock
            localNodes = nodes.ToList()
            selfNode = self
        End SyncLock
    End Sub

    Public Sub Shutdown()
        shouldQuit = True
    End Sub

    Public Function GetNodeForConnecting(self As DSNode, blacklist As IEnumerable(Of String)) As DSNode
        ' This is called from mainthread, lock acordingly!

        Dim blackSet As New HashSet(Of String)(blacklist)
        blackSet.Add(self.SteamId)

        Dim candidates As New List(Of DSNode)
        For Each t In ircNodes.Values
            Dim node As DSNode = t.Item1
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

    Private Sub main(args As String())
        Try
            While Not shouldQuit
                Try
                    setStatus("Initiating connection ...")
                    connectToServer()
                    setStatus("Connected.")
                    messageLoop()
                Catch ex As IRCConnectionError
                    If Not shouldQuit Then
                        setStatus("Error: " & ex.Message & " – retrying in 10 seconds")
                        Thread.Sleep(10000)
                    End If
                End Try
                If tcpClient IsNot Nothing Then
                    tcpClient.Close()
                    tcpClient = Nothing
                End If
            End While
            setStatus("Disconnected.")
        Catch ex As Exception
            setStatus("DSCMNet crashed with: " & ex.Message)
        End Try
    End Sub

    Private Sub connectToServer()
        Dim nick As String = Config.IRCNick & "[" & mainWindow.Version.Replace(".", "-") & "]" & Guid.NewGuid.ToString().Split("-")(0)

        tcpClient = New System.Net.Sockets.TcpClient()
        tcpClient.Connect(Config.IRCHost, Config.IRCPort)
        If Not tcpClient.Connected Then
            Throw New IRCConnectionError("Failed to connect")
        End If
        stream = tcpClient.GetStream()
        _streamReader = New StreamReader(stream)
        _streamWriter = New StreamWriter(stream)
        _streamWriter.AutoFlush = True

        _streamWriter.Write("USER " & nick & " 0 * :" & Config.IRCOwner & vbCr & vbLf)
        _streamWriter.Write("NICK " & nick & vbCr & vbLf)
        _streamWriter.Write("MODE " & nick & " +B" & vbCr & vbLf)

        'Join channel on start
        While True
            If isConnectionDropped() Then
                Throw New IRCConnectionError("Connection was dropped during init")
            End If
            Dim buf As String = _streamReader.ReadLine()
            If buf IsNot Nothing Then
                If buf.StartsWith("PING ") Then
                    _streamWriter.Write(buf.Replace("PING", "PONG") & vbCr & vbLf)
                ElseIf buf.Contains(":MOTD") Then
                    _streamWriter.Write("JOIN " & Config.IRCChannel & vbCr & vbLf)
                    Exit While
                End If
            End If
        End While
    End Sub
    Private Sub messageLoop()
        Dim lastPublish As Date = DateTime.UtcNow
        While True
            If shouldQuit Then
                _streamWriter.Write("QUIT" & vbCr & vbLf)
                Exit While
            ElseIf isConnectionDropped() Then
                Throw New IRCConnectionError("Connection was dropped")
            ElseIf (DateTime.UtcNow - lastPublish).TotalSeconds >= Config.IRCPublishInterval Then
                lastPublish = DateTime.UtcNow
                publishLocalNodes()
                expireIrcNodes()
            ElseIf stream.DataAvailable Then
                handleIRCLine(_streamReader.ReadLine())
            Else
                Thread.Sleep(50)
            End If
        End While
    End Sub
    Private Function isConnectionDropped() As Boolean
        Return tcpClient.Client.Poll(0, SelectMode.SelectRead) AndAlso tcpClient.Client.Available = 0
    End Function
    Private Sub handleIRCLine(line As String)
        Dim prefix As String = Nothing
        If line.StartsWith(":") Then
            prefix = line.Split({" "c}, 2)(0).Substring(1)
            line = line.Split({" "c}, 2)(1)
        End If
        Dim parts = line.Split({" "c}, 2)
        Dim command As String = parts(0)
        Dim rest As String = parts.ElementAtOrDefault(1)

        If command = "PING" Then
            _streamWriter.Write("PONG " & rest & vbCr & vbLf)
        ElseIf command = "PRIVMSG" Then
            Dim msg As String = rest.Split({" "c}, 2)(1).Substring(1)
            If msg.StartsWith("REPORT|") Or msg.StartsWith("REPORTSELF|") Then
                Dim inNode As DSNode
                Try
                    inNode = parseNodeReport(msg.Split("|")(1))
                Catch ex As Exception
#If DEBUG Then
                    Dim senderNick As String = Regex.Match(prefix, "^([^!@]+)").Groups.Item(1).Value
                    setStatus("Invalid: " & senderNick & "   " & msg)
#End If
                    Return
                End Try
                If (ircNodes.ContainsKey(inNode.SteamId) AndAlso
                        Not inNode.HasExtendedInfo AndAlso
                        ircNodes(inNode.SteamId).Item1.HasExtendedInfo) Then
                    inNode.Covenant = ircNodes(inNode.SteamId).Item1.Covenant
                    inNode.Indictments = ircNodes(inNode.SteamId).Item1.Indictments
                End If
                ircNodes(inNode.SteamId) = Tuple.Create(inNode, DateTime.UtcNow)
            End If
        End If
    End Sub

    Private Function parseNodeReport(text) As DSNode
        Dim node As New DSNode()
        Dim tmpFields()

        tmpFields = text.Split(",")

        node.CharacterName = tmpFields(0)
        node.SteamId = tmpFields(1)
        node.SoulLevel = tmpFields(2)
        node.PhantomType = tmpFields(3)
        node.MPZone = tmpFields(4)
        node.World = tmpFields(5)

        If tmpFields.Count > 6 Then
            node.Covenant = tmpFields(6)
            node.Indictments = tmpFields(7)
        End If

        Return node
    End Function
    Private Sub publishLocalNodes()
        'Report your active node status
        Try
            SyncLock localNodesLock
                For Each node In localNodes
                    'Check if node was already reported in the last 3 minutes
                    Dim networkKnowsNode = (
                        ircNodes.ContainsKey(node.SteamId) AndAlso
                        (DateTime.UtcNow - ircNodes(node.SteamId).Item2).TotalSeconds <= IRCNodePublishInterval)
                    If networkKnowsNode Then
                        'Publish anyways if the information in the network is incorrect
                        Dim networkInfoIsStale = (
                            Not ircNodes(node.SteamId).Item1.BasicEquals(node) AndAlso
                            (DateTime.UtcNow - ircNodes(node.SteamId).Item2).TotalSeconds >= IRCNodeUpdateInterval)
                        If Not networkInfoIsStale Then Continue For
                    End If

                    Dim ircName As String = node.CharacterName
                    ircName = ircName.Replace(",", "")
                    ircName = ircName.Replace("|", "")
                    Dim reportData As String = (
                        ircName & "," & node.SteamId & "," & node.SoulLevel & "," &
                        node.PhantomType & "," & node.MPZone & "," & node.World)
                    _streamWriter.Write("PRIVMSG #DSCM-Main REPORT|" & reportData & vbCr & vbLf)
                Next
                If selfNode IsNot Nothing Then
                    Dim ircName As String = selfNode.CharacterName
                    ircName = ircName.Replace(",", "")
                    ircName = ircName.Replace("|", "")
                    Dim reportData As String = (
                        ircName & "," & selfNode.SteamId & "," & selfNode.SoulLevel & "," &
                        selfNode.PhantomType & "," & selfNode.MPZone & "," & selfNode.World & "," &
                        selfNode.Covenant & "," & selfNode.Indictments)
                    _streamWriter.Write("PRIVMSG #DSCM-Main REPORTSELF|" & reportData & vbCr & vbLf)
                    Console.WriteLine(reportData)
                End If
            End SyncLock
        Catch ex As Exception
            setStatus("Error publishing local nodes: " & ex.Message)
        End Try
    End Sub
    Private Sub expireIrcNodes()
        Dim now As Date = DateTime.UtcNow
        For Each t In ircNodes.Values.ToList()
            If (now - t.Item2).TotalSeconds >= Config.IRCNodeTTL Then
                ircNodes.TryRemove(t.Item1.SteamId, t)
            End If
        Next
    End Sub
    Private Sub setStatus(status As String)
        If mainWindow.InvokeRequired Then
            mainWindow.Invoke(New setStatusDelegate(AddressOf setStatus), {status})
        Else
            mainWindow.txtIRCDebug.Text = status
        End If
    End Sub
    Private Delegate Sub setStatusDelegate(status As String)
End Class