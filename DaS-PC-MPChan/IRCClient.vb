Imports System.Threading
Imports System.IO
Imports System.Net.Sockets
Imports System.Collections.Concurrent

Public Class IRCClient
    Private mainWindow As DSCM

    Private _thread As Thread
    Private _streamWriter As StreamWriter = Nothing
    Private _streamReader As StreamReader = Nothing

    Private shouldQuit As Boolean = False

    Private localNodesLock As New Object()
    Private localNodes As New List(Of DSNode)
    Public ircNodes As New ConcurrentDictionary(Of String, Tuple(Of DSNode, Date))

    Public Sub New(mainWindow As DSCM)
        Me.mainWindow = mainWindow
        _thread = New Thread(AddressOf main)
        _thread.IsBackground = True
        _thread.Start()
    End Sub

    Public Sub setLocalNodes(nodes As IEnumerable(Of DSNode))
        SyncLock localNodesLock
            localNodes = nodes.ToList()
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
        Dim port As Integer
        Dim buf As String, nick As String, owner As String, server As String, chan As String
        Dim tcpClient As New System.Net.Sockets.TcpClient()
        Dim stream As NetworkStream


        Try
            nick = "DSCM-" & Guid.NewGuid.ToString()
            owner = "DSCMbot"
            server = "dscm.wulf2k.ca"
            port = 8123
            chan = "#DSCM-Main"

            setStatus("Initiating connection")
            'Connect to irc server and get input and output text streams from TcpClient.
            tcpClient.Connect(server, port)
            If Not tcpClient.Connected Then
                setStatus("Failed to connect.")
                Return
            End If
            stream = tcpClient.GetStream()
            _streamReader = New StreamReader(stream)
            _streamWriter = New StreamWriter(stream)
            _streamWriter.AutoFlush = True

            _streamWriter.Write("USER " & nick & " 0 * :" & owner & vbCr & vbLf)
            _streamWriter.Write("NICK " & nick & vbCr & vbLf)
            _streamWriter.Write("MODE " & nick & " +B" & vbCr & vbLf)

            'Join channel on start
            While True
                buf = _streamReader.ReadLine()
                If buf IsNot Nothing Then
                    If buf.StartsWith("PING ") Then
                        _streamWriter.Write(buf.Replace("PING", "PONG") & vbCr & vbLf)
                    End If

                    If buf.Contains(":MOTD") Then
                        _streamWriter.Write("JOIN " & chan & vbCr & vbLf)
                        Exit While
                    End If
                End If
            End While

            setStatus("Connected.")

            Dim lastPublish As Date = DateTime.UtcNow
            While True
                If shouldQuit Then
                    _streamWriter.Write("QUIT" & vbCr & vbLf)
                    Exit While
                ElseIf (DateTime.UtcNow - lastPublish).TotalSeconds >= 120 Then
                    lastPublish = DateTime.UtcNow
                    publishLocalNodes()
                    expireIrcNodes()
                ElseIf stream.DataAvailable Then
                    handleIRCLine(_streamReader.ReadLine())
                Else
                    Thread.Sleep(50)
                End If
            End While
            setStatus("Disconnected.")
        Catch ex As Exception
            setStatus("DSCMNet crashed with: " & ex.Message)
        End Try
    End Sub

    Private Sub handleIRCLine(line As String)
        'Send pong reply to any ping messages
        If line.StartsWith("PING ") Then
            _streamWriter.Write(line.Replace("PING", "PONG") & vbCr & vbLf)
        End If

        'Parse report commands
        If line.Contains("REPORT|") Then
            Try
                Dim inNode As DSNode
                inNode = parseNodeReport(line.Split("|")(1))
                ircNodes(inNode.SteamId) = Tuple.Create(inNode, DateTime.UtcNow)
            Catch ex As Exception
                setStatus("Error processing player report - " & ex.Message)
            End Try
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

        Return node
    End Function
    Private Sub publishLocalNodes()
        'Report your active node status
        Try
            SyncLock localNodesLock
                For Each node In localNodes
                    'Check if node was already reported in the last 3 minutes
                    'TODO: always report our own node
                    Dim networkKnowsNode = (
                        ircNodes.ContainsKey(node.SteamId) AndAlso
                        (DateTime.UtcNow - ircNodes(node.SteamId).Item2).TotalMinutes <= 3)
                    If networkKnowsNode Then Continue For

                    Dim ircName As String = node.CharacterName
                    ircName = ircName.Replace(",", "")
                    ircName = ircName.Replace("|", "")
                    Dim reportData As String = ircName & "," & node.SteamId & "," & node.SoulLevel & "," & node.PhantomType & "," & node.MPZone & "," & node.World
                    _streamWriter.Write("PRIVMSG #DSCM-Main REPORT|" & reportData & vbCr & vbLf)
                Next
            End SyncLock
        Catch ex As Exception
            setStatus("Error publishing local nodes: " & ex.Message)
        End Try
    End Sub
    Private Sub expireIrcNodes()
        Dim now As Date = DateTime.UtcNow
        For Each t In ircNodes.Values.ToList()
            If (now - t.Item2).TotalMinutes >= 5 Then
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