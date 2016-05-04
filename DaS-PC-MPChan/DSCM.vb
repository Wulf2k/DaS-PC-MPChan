Imports System.Threading
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Net.Sockets


Public Class DSCM
    'Timers
    Private WithEvents refMpData As New System.Windows.Forms.Timer()
    Private WithEvents refTimer As New System.Windows.Forms.Timer()
    Private WithEvents ircTimer As New System.Windows.Forms.Timer()  'Report personal info to IRC
    Private WithEvents ircTimer2 As New System.Windows.Forms.Timer()  'Report secondary node info to IRC
    Private WithEvents attemptConnTimer As New System.Windows.Forms.Timer()
    Private WithEvents onlineTimer As New System.Windows.Forms.Timer()
    Private WithEvents hotkeyTimer As New System.Windows.Forms.Timer()

    'For hotkey support
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Short

    'Thread to check for updates
    Private updTrd As Thread
    Private ircTrd As Thread

    'IRC variables
    Dim trdLock As New Object
    Private Shared ircInQueue As New List(Of String)
    Private Shared ircOutQueue As New List(Of String)
    Private Shared ircDbgQueue As New List(Of String)
    Private Shared attemptConnQueue As New List(Of String)
    Private _tcpclientConnection As TcpClient = Nothing
    Private _networkStream As NetworkStream = Nothing
    Private _streamWriter As StreamWriter = Nothing
    Private _streamReader As StreamReader = Nothing

    Private Shared ircOnline As Boolean = False
    Private Shared input
    Private Shared output

    'Hotkeys
    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean


    'String lookups
    Dim hshtPhantomType As New Hashtable()
    Dim hshtWorld As New Hashtable()

    Dim hshtPhantomTypeReverse As New Hashtable()
    Dim hshtWorldReverse As New Hashtable()


    'New version of DSCM available?
    Dim newstablever As Boolean = False
    Dim newtestver As Boolean = False

    Dim dsProcess As DarkSoulsProcess


    Private Sub DSCM_Close(sender As Object, e As EventArgs) Handles MyBase.FormClosed
        chkDebugDrawing.Checked = False
        chkExpand.Checked = False
    End Sub
    Private Sub DSCM_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Start Refresh timer
        refTimer = New System.Windows.Forms.Timer
        refTimer.Interval = 200
        refTimer.Enabled = True
        refTimer.Start()

        hotkeyTimer.Enabled = True
        hotkeyTimer.Interval = 10
        hotkeyTimer.Start()

        refMpData = New System.Windows.Forms.Timer
        refMpData.Interval = 10000
        refMpData.Start()
        refTimer.Enabled = True

        Try
            dsProcess = New DarkSoulsProcess()
        Catch ex As DSProcessNotFound
        End Try

        'Set initial form size to non-expanded
        Me.Width = 450
        Me.Height = 190

        dgvMPNodes.Columns.Add("name", "Name")
        dgvMPNodes.Columns(0).Width = 180
        dgvMPNodes.Columns(0).ValueType = GetType(String)
        dgvMPNodes.Columns.Add("steamId", "Steam ID")
        dgvMPNodes.Columns(1).Width = 145
        dgvMPNodes.Columns(1).ValueType = GetType(String)
        dgvMPNodes.Columns.Add("soulLevel", "SL")
        dgvMPNodes.Columns(2).Width = 60
        dgvMPNodes.Columns(2).ValueType = GetType(Integer)
        dgvMPNodes.Columns.Add("phantomType", "Phantom Type")
        dgvMPNodes.Columns(3).Width = 80
        dgvMPNodes.Columns(3).ValueType = GetType(String)
        dgvMPNodes.Columns.Add("mpArea", "MP Area")
        dgvMPNodes.Columns(4).Width = 80
        dgvMPNodes.Columns(4).ValueType = GetType(Integer)
        dgvMPNodes.Columns.Add("world", "World")
        dgvMPNodes.Columns(5).Width = 200
        dgvMPNodes.Columns(5).ValueType = GetType(String)
        dgvMPNodes.Font = New Font("Consolas", 10)

        dgvFavoriteNodes.Columns.Add("name", "Name")
        dgvFavoriteNodes.Columns(0).Width = 180
        dgvFavoriteNodes.Columns(0).ValueType = GetType(String)
        dgvFavoriteNodes.Columns.Add("steamId", "Steam ID")
        dgvFavoriteNodes.Columns(1).Width = 145
        dgvFavoriteNodes.Columns(1).ValueType = GetType(String)
        dgvFavoriteNodes.Columns.Add("isOnline", "O")
        dgvFavoriteNodes.Columns(2).Width = 20
        dgvFavoriteNodes.Columns(2).ValueType = GetType(String)
        dgvFavoriteNodes.Font = New Font("Consolas", 10)


        dgvRecentNodes.Columns.Add("name", "Name")
        dgvRecentNodes.Columns(0).Width = 180
        dgvRecentNodes.Columns(0).ValueType = GetType(String)
        dgvRecentNodes.Columns.Add("steamId", "Steam ID")
        dgvRecentNodes.Columns(1).Width = 145
        dgvRecentNodes.Columns(1).ValueType = GetType(String)
        dgvRecentNodes.Columns.Add("orderId", "Order ID")
        dgvRecentNodes.Columns(2).Visible = False
        dgvRecentNodes.Columns(2).ValueType = GetType(Long)
        dgvRecentNodes.Columns.Add("isOnline", "O")
        dgvRecentNodes.Columns(3).Width = 20
        dgvRecentNodes.Columns(3).ValueType = GetType(String)
        dgvRecentNodes.Font = New Font("Consolas", 10)


        dgvDSCMNet.Columns.Add("name", "Name")
        dgvDSCMNet.Columns(0).Width = 180
        dgvDSCMNet.Columns(0).ValueType = GetType(String)
        dgvDSCMNet.Columns.Add("steamId", "Steam ID")
        dgvDSCMNet.Columns(1).Width = 145
        dgvDSCMNet.Columns(1).ValueType = GetType(String)
        dgvDSCMNet.Columns.Add("soulLevel", "SL")
        dgvDSCMNet.Columns(2).Width = 60
        dgvDSCMNet.Columns(2).ValueType = GetType(Integer)
        dgvDSCMNet.Columns.Add("phantomType", "Phantom Type")
        dgvDSCMNet.Columns(3).Width = 80
        dgvDSCMNet.Columns(3).ValueType = GetType(String)
        dgvDSCMNet.Columns.Add("mpArea", "MP Area")
        dgvDSCMNet.Columns(4).Width = 80
        dgvDSCMNet.Columns(4).ValueType = GetType(Integer)
        dgvDSCMNet.Columns.Add("world", "World")
        dgvDSCMNet.Columns(5).Width = 200
        dgvDSCMNet.Columns(5).ValueType = GetType(String)
        dgvDSCMNet.Columns.Add("lastReport", "Last Report")
        dgvDSCMNet.Columns(6).Width = 10
        dgvDSCMNet.Columns(6).ValueType = GetType(Integer)
        dgvDSCMNet.Columns(6).Visible = False
        dgvDSCMNet.Font = New Font("Consolas", 10)


        'Init String lookups

        hshtPhantomType.Add("-1", "Loading")
        hshtPhantomType.Add("0", "Human")
        hshtPhantomType.Add("1", "Co-op")
        hshtPhantomType.Add("2", "Invader")
        hshtPhantomType.Add("8", "Hollow")



        hshtWorld.Add("-1--1", "None")
        hshtWorld.Add("10-0", "Depths")
        hshtWorld.Add("10-1", "Undead Burg/Parish")
        hshtWorld.Add("10-2", "Firelink Shrine")
        hshtWorld.Add("11-0", "Painted World")
        hshtWorld.Add("12-0", "Darkroot Garden")
        hshtWorld.Add("12-1", "Oolacile")
        hshtWorld.Add("13-0", "Catacombs")
        hshtWorld.Add("13-1", "Tomb of the Giants")
        hshtWorld.Add("13-2", "Great Hollow / Ash Lake")
        hshtWorld.Add("14-0", "Blighttown")
        hshtWorld.Add("14-1", "Demon Ruins")
        hshtWorld.Add("15-0", "Sen's Fortress")
        hshtWorld.Add("15-1", "Anor Londo")
        hshtWorld.Add("16-0", "New Londo Ruins")
        hshtWorld.Add("17-0", "Duke's Archives / Caves")
        hshtWorld.Add("18-0", "Kiln")
        hshtWorld.Add("18-1", "Undead Asylum")

        'Set up Reverse Lookups
        For each k In hshtPhantomType.keys
            hshtPhantomTypeReverse.add(hshtPhantomType(k),k)
        Next

        For each k In hshtWorld.Keys
            hshtWorldReverse.Add(hshtWorld(k),k)
        Next

        'Check version number in new thread, so main thread isn't delayed.
        'Compares value on server to date in label on main form
        updTrd = New Thread(AddressOf updatecheck)
        updTrd.IsBackground = True
        updTrd.Start()

        'Create regkeys if they don't exist
        My.Computer.Registry.CurrentUser.CreateSubKey("Software\DSCM\FavoriteNodes")
                My.Computer.Registry.CurrentUser.CreateSubKey("Software\DSCM\RecentNodes")



        'Load favorite node list from registry
        loadFavoriteNodes()
        loadRecentNodes()
        updateOnlinestate()

        onlineTimer.Enabled = True
        onlineTimer.Interval = 10 * 60 * 1000
        onlineTimer.Start()

        attemptConnTimer.Enabled = True
        attemptConnTimer.Interval = 1000
        attemptConnTimer.Start()

        chkExpand.Checked = True
    End Sub
    Private Sub loadFavoriteNodes()
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\FavoriteNodes", True)

        For Each id As String In key.GetValueNames()
            dgvFavoriteNodes.Rows.Add(key.GetValue(id), id)
        Next
    End Sub
    Private Sub loadRecentNodes()
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\RecentNodes", True)

        Dim name As String
        Dim tmpRecentID As Long

        For Each id As String In key.GetValueNames()
            name = key.GetValue(id)
            tmpRecentID = name.Split("|")(0)
            name = name.Split("|")(1)
            dgvRecentNodes.Rows.Add(name, id, tmpRecentID)
        Next
    End Sub
    Private Sub onlineTimer_Tick() Handles onlineTimer.Tick
        'Contributed by Chronial
        updateOnlinestate()
    End Sub
    Private Sub updateOnlinestate()
        'Contributed by Chronial
        'Remote server set up and maintained by Chronial
        Dim steamIds = New HashSet(Of String)
        For Each Row In dgvRecentNodes.Rows
            steamIds.Add(Row.Cells("steamId").Value)
        Next
        For Each Row In dgvFavoriteNodes.Rows
            If steamIds.Count < 100 Then steamIds.Add(Row.Cells("steamId").Value)
        Next
        Dim converter = New Converter(Of String, String)(Function(num) Convert.ToInt64(num, 16).ToString())
        Dim idQuery = String.Join(",", Array.ConvertAll(steamIds.ToArray(), converter))
        Dim url = "http://chronial.de/scripts/dscm/is_online.php?ids=" & idQuery
        Dim client = New Net.WebClient()
        Dim onlineInfo = New Dictionary(Of Int64, Boolean)
        Try
            Dim stream = client.OpenRead(url)
            Dim parser = New FileIO.TextFieldParser(stream)
            parser.SetDelimiters({","})

            While Not parser.EndOfData
                Dim strings = parser.ReadFields()
                onlineInfo(Int64.Parse(strings(0))) = Boolean.Parse(strings(1))
            End While
        Catch
            Return
        End Try

        For Each Row In dgvRecentNodes.Rows
            Try
                If onlineInfo(converter(Row.Cells("steamId").Value())) Then
                    Row.Cells("isOnline").Value = "Y"
                Else
                    Row.Cells("isOnline").Value = "N"
                End If
            Catch ex As KeyNotFoundException
            End Try
        Next
        For Each Row In dgvFavoriteNodes.Rows
            Try
                If onlineInfo(converter(Row.Cells("steamId").Value())) Then
                    Row.Cells("isOnline").Value = "Y"
                Else
                    Row.Cells("isOnline").Value = "N"
                End If
            Catch ex As KeyNotFoundException
            End Try
        Next
    End Sub
    Private Sub updatecheck()
        Try
            'Update level is contents of remote text file compared to version label on main form.
            My.Computer.Network.DownloadFile("http://wulf2k.ca/pc/das/dscm-ver.txt", Path.GetTempPath & "\dscm-ver.txt", "", "", False, 9800, True)
            Dim stablever = File.ReadAllLines(Path.GetTempPath & "\dscm-ver.txt")(0)
            Dim testver = File.ReadAllLines(Path.GetTempPath & "\dscm-ver.txt")(1)

            newstablever = (stablever > lblVer.Text.Replace(".", ""))
            newtestver = (testver > lblVer.Text.Replace(".", ""))
        Catch ex As Exception
            'Fail silently since nobody wants to be bothered for an update check.
        End Try
    End Sub
    Private Sub ircTimer_Tick() Handles ircTimer.Tick
        'Report your status
        Try
            If Not IsNothing(dsProcess) AndAlso dsProcess.SelfNode.SteamId AndAlso dsProcess.SelfNode.CharacterName Then
                Dim str As String = "PRIVMSG #DSCM-Main REPORT|"
                Dim self As DSNode = dsProcess.SelfNode
                Dim ircName As String = self.CharacterName
                ircName = ircName.Replace(",", "")
                ircName = ircName.Replace("|", "")
                str = str & ircName & "," & self.SteamId & "," & self.SoulLevel & "," & self.PhantomType & "," & self.MPZone & "," & self.World

                ircOutWrite(str)
                ircDebugWrite("Reporting status to DSCMNet.")
            End If
        Catch ex As Exception
            ircDebugWrite("Error reporting status - " & ex.Message)
        End Try

        'Prune outdated entries
        Try
            Dim tmpNow As Integer
            Dim tmpThen As Integer
            tmpNow = Now.Minute
            If dgvDSCMNet.Rows.Count > 0 Then
                For i = 0 To dgvDSCMNet.RowCount - 1
                    tmpThen = dgvDSCMNet.Rows(i).Cells(6).Value
                    If tmpThen > tmpNow Then tmpThen -= 60
                    If (tmpNow - tmpThen) > 5 Then
                        dgvDSCMNet.Rows.Remove(dgvDSCMNet.Rows(i))
                    End If
                Next
            End If
        Catch ex As Exception
            ircDebugWrite("Error removing old entries - " & ex.Message)
        End Try

    End Sub
    Private sub irctimer2_Tick() Handles irctimer2.Tick
         'Report your active node status
        Try
            If Not IsNothing(dsProcess) Then
                Console.WriteLine("Beginning secondary report")
                For i = 0 To dgvMPNodes.Rows.Count - 1
                    Dim str As String = "PRIVMSG #DSCM-Main REPORT|"
                    Dim tmpName = dgvMPNodes.Rows(i).Cells(0).FormattedValue
                    Dim tmpSteamID = dgvMPNodes.Rows(i).Cells(1).FormattedValue
                    Dim tmpSL = dgvMPNodes.Rows(i).Cells(2).FormattedValue
                    Dim tmpPhantom = dgvMPNodes.Rows(i).Cells(3).FormattedValue
                    Dim tmpMPZone = dgvMPNodes.Rows(i).Cells(4).FormattedValue
                    Dim tmpWorld = dgvMPNodes.Rows(i).Cells(5).FormattedValue


                    'Check if node is already reported in the last 3 minutes
                    Dim reported As Boolean = False
                    For j = 0 To dgvDSCMNet.Rows.Count - 1
                        Dim tmpUpdMinute = dgvDSCMNet.Rows(i).Cells(6).FormattedValue
                        Dim tmpNow = Now.Minute
                        If tmpUpdMinute > tmpNow Then tmpUpdMinute -=60 
                        If (dgvDSCMNet.Rows(j).Cells(1).FormattedValue = tmpsteamid) And (tmpNow - tmpUpdMinute < 4) Then
                            reported = true
                        End If
                    Next

                    if Not reported then
                        tmpPhantom = hshtPhantomTypeReverse(tmpPhantom)
                        tmpWorld = hshtWorldReverse(tmpWorld)

                        str = str & tmpName & "," & tmpSteamID & "," & tmpSL & "," & tmpPhantom & "," & tmpMPZone & "," & tmpWorld
                        ircOutWrite(str)
                    End If
                Next

            End If
        Catch ex As Exception
            Console.WriteLine("Error reporting secondary status - " & ex.Message)
            ircDebugWrite("Error reporting secondary status - " & ex.Message)
        End Try
    End sub
    Private Sub AttemptConnTimer_Tick() Handles attemptConnTimer.Tick
        If attemptConnQueue.Count > 0 Then
            dsProcess.ConnectToSteamId(attemptConnQueue.Item(0))
            attemptConnQueue.Remove(attemptConnQueue.Item(0))
        End If
    End Sub
    Private Sub refTimer_Tick() Handles refTimer.Tick
        Dim dbgboost As Integer = 0
        Dim tmpptr As Integer = 0

        'Text indicating new version is hidden if DSCM is expanded, only care if it's seen at the start anyway.
        If newtestver Or newstablever Then
            lblNewVersion.Visible = Not chkExpand.Checked
            lblUrl.Visible = lblNewVersion.Visible
            If newtestver Then lblNewVersion.Text = "New testing version available"
            If newstablever Then lblNewVersion.Text = "New stable version available"
        End If


        If dsProcess IsNot Nothing
            'Node display
            'Changes the comparison instruction to display it if value is 0, rather than changing the value itself
            chkDebugDrawing.Checked = dsProcess.DrawNodes

            Dim maxNodes = dsProcess.MaxNodes
            If maxNodes <> 0
                If maxnodes >= nmbMaxNodes.Minimum And maxnodes <= nmbMaxNodes.Maximum Then
                    nmbMaxNodes.Value = maxnodes
                End If
            End If
            
            'Don't update the text box if it's clicked in, so people can copy/paste without losing cursor.
            'Probably don't need to update this more than once anyway, but why not?
            If Not txtSelfSteamID.Focused Then
                txtSelfSteamID.Text = dsProcess.SelfSteamId
            End If

            txtCurrNodes.Text = dsProcess.NodeCount
        End If



        'IRC related
        Dim ircIn As String
        ircIn = ircinRead()
        If Not ircIn Is Nothing Then
            If ircIn.Contains("REPORT|") Then
                Try
                    Dim tmpName As String
                    Dim tmpSteamID As String
                    Dim tmpSL As Integer
                    Dim tmpPhantom As String
                    Dim tmpMPZone As Integer
                    Dim tmpWorld As String
                    Dim tmpUpdMinute As Integer

                    Dim tmpFields()

                    Dim tmpReport As String
                    tmpReport = ircIn.Split("|")(1)

                    tmpFields = tmpReport.Split(",")

                    tmpName = tmpFields(0)
                    tmpSteamID = tmpFields(1)
                    tmpSL = tmpFields(2)
                    tmpPhantom = tmpFields(3)
                    tmpMPZone = tmpFields(4)
                    tmpWorld = tmpFields(5)
                    tmpUpdMinute = TimeOfDay.TimeOfDay.Minutes
                    
                    'Calculations for the IRC node-connect feature.
                    Dim LowerNodesThreshold As Integer = 4
                    Dim ReservedSteamNodeCount As Integer = 4 'N + 1 nodes reserved for Steam matchmaking

                    If Not IsNothing(dsProcess) And (tmpSteamID.Length = 16) Then
                        If (Val(txtCurrNodes.Text) < nmbMaxNodes.Value) And Not (tmpWorld = "-1--1") And (attemptConnQueue.Count + Val(txtCurrNodes.Text) < 4) Then
                            'Autoconnect to any nodes not idling at the menu when Currnodes are under LowerNodesThreshold
                            If Val(txtCurrNodes.Text) < LowerNodesThreshold Then
                                attemptConnQueue.Add(tmpSteamID)
                            ElseIf (nmbMaxNodes.Value - Val(txtCurrNodes.Text) > ReservedSteamNodeCount) Then
                                If tmpWorld = dsProcess.SelfNode.World Then
                                    If Math.Abs(tmpSL - dsProcess.SelfNode.SoulLevel) < 11 + dsProcess.SelfNode.SoulLevel * 0.1 Then
                                        attemptConnQueue.Add(tmpSteamID)
                                        ircDebugWrite("Attemping connection to " & tmpName & ", SL " & tmpSL & " in " & tmpWorld)
                                    End If
                                End If
                            End If
                        End If

                        Try
                            tmpPhantom = hshtPhantomType(tmpPhantom)
                        Catch ex As Exception
                            Console.WriteLine("Phantom type lookup failed on " & tmpPhantom)
                        End Try
                        Try
                            tmpWorld = hshtWorld(tmpWorld)
                        Catch ex As Exception
                            Console.WriteLine("World name lookup failed on " & tmpWorld)
                        End Try

                        Dim newID As Boolean = True
                        For i = 0 To dgvDSCMNet.Rows.Count - 1
                            If dgvDSCMNet.Rows(i).Cells(1).Value = tmpSteamID Then
                                newID = False
                                dgvDSCMNet.Rows(i).Cells(0).Value = tmpName
                                dgvDSCMNet.Rows(i).Cells(2).Value = tmpSL
                                dgvDSCMNet.Rows(i).Cells(3).Value = tmpPhantom
                                dgvDSCMNet.Rows(i).Cells(4).Value = tmpMPZone
                                dgvDSCMNet.Rows(i).Cells(5).Value = tmpWorld
                                dgvDSCMNet.Rows(i).Cells(6).Value = tmpUpdMinute
                            End If
                        Next
                        If newID And Not tmpSteamID = dsProcess.SelfSteamId Then dgvDSCMNet.Rows.Add(tmpName, tmpSteamID, tmpSL, tmpPhantom, tmpMPZone, tmpWorld, tmpUpdMinute)
                    End If
                Catch ex As Exception
                    ircDebugWrite("Error processing player report - " & ex.Message)
                End Try
            End If

            'Console.WriteLine(ircIn)
        End If

        Dim ircDebug As String
        ircDebug = ircDebugRead()
        If Not ircDebug Is Nothing Then
            txtIRCDebug.Text = ircDebug
        End If

        If chkDSCMNet.Checked and Not tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")" Then
            tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")"
        End If
    End Sub
    Private Shared Sub hotkeyTimer_Tick() Handles hotkeyTimer.Tick
        Dim ctrlkey As Boolean
        Dim oneKey As Boolean 'Toggle Node Display
        Dim twoKey As Boolean 'Previously toggled NamedNodes, now a free hotkey.

        ctrlkey = GetAsyncKeyState(Keys.ControlKey)
        oneKey = GetAsyncKeyState(Keys.D1)
        twoKey = GetAsyncKeyState(Keys.D2)

        If (ctrlkey And oneKey) And Not (DSCM.ctrlHeld And DSCM.oneHeld) Then
            DSCM.chkDebugDrawing.Checked = Not DSCM.chkDebugDrawing.Checked
        End If


        If (ctrlkey And twoKey) And Not (DSCM.ctrlHeld And DSCM.twoheld) Then
            'Hotkey available
        End If

        DSCM.ctrlHeld = ctrlkey
        DSCM.oneHeld = oneKey
        DSCM.twoheld = twoKey
    End Sub
    Private Sub frmResize() Handles Me.Resize
        tabs.Width = Me.Width - 35
        tabs.Height = Me.Height - 190
        dgvMPNodes.Width = Me.Width - 50
        dgvMPNodes.Height = Me.Height - 225
        dgvDSCMNet.Width = Me.Width - 50
        dgvDSCMNet.Height = Me.Height - 250
        txtIRCDebug.Location = New Point(6, dgvDSCMNet.Location.Y + dgvDSCMNet.Height + 5)
        txtIRCDebug.Width = dgvDSCMNet.Width

        dgvFavoriteNodes.Height = Me.Height - 225
        dgvRecentNodes.Height = Me.Height - 225

        btnReconnect.Location = New Point(1, Me.Height - 65)
        lblVer.Location = New Point(Me.Width - 100, Me.Height - 55)

        lblNodes.Location = New Point(Me.Width - 167, 6)
        txtCurrNodes.Location = New Point(Me.Width - 112, 5)
        lblNodeDiv.Location = New Point(Me.Width - 74, 6)
        nmbMaxNodes.Location = New Point(Me.Width - 63, 3)
        lblYourId.Location = New Point(Me.Width - 267, 35)
        txtSelfSteamID.Location = New Point(Me.Width - 155, 32)
        lblTargetId.Location = New Point(Me.Width - 281, 61)
        txtTargetSteamID.Location = New Point(Me.Width - 155, 58)
        btnAttemptId.Location = New Point(Me.Width - 155, 85)

        btnAddFavorite.Location = New Point(250, Me.Height - 65)
        btnRemFavorite.Location = New Point(400, Me.Height - 65)
    End Sub

    Private Sub btnReconnect_Click(sender As Object, e As EventArgs) Handles btnReconnect.Click
        chkDSCMNet.Checked = False
        If Not IsNothing(dsProcess) Then
            dsProcess.Dispose()
            dsProcess = Nothing
        End If
        
        Try
            dsProcess = New DarkSoulsProcess()
        Catch ex As DSProcessNotFound
        End Try
    End Sub

    Private Sub chkDebugDrawing_CheckedChanged(sender As Object, e As EventArgs) Handles chkDebugDrawing.CheckedChanged
        If IsNothing(dsProcess) Then
            chkDebugDrawing.Checked = False
            Exit Sub
        End If
        dsProcess.DrawNodes = chkDebugDrawing.Checked
    End Sub

    Private Sub refMpData_Tick() Handles refMpData.Tick
        If IsNothing(dsProcess)
            Exit Sub
        End If
        dsProcess.UpdateNodes()
        Dim nodes As New Dictionary(Of String, DSNode)(dsProcess.ConnectedNodes)
        nodes.Add(dsProcess.SelfNode.SteamId, dsProcess.SelfNode)

        For Each node As DSNode In nodes.Values
            Dim row As DataGridViewRow
            Dim notexist As Boolean = True
            For j = 0 To dgvMPNodes.Rows.Count - 1
                If dgvMPNodes.Rows(j).Cells("steamId").Value = node.SteamId Then
                    row = dgvMPNodes.Rows(j)
                End If
            Next
            If row Is Nothing Then
                Dim rowIndex = dgvMPNodes.Rows.Add()
                row = dgvMPNodes.Rows(rowIndex)
            End If
            row.Cells("name").Value = node.CharacterName
            row.Cells("steamId").Value = node.SteamId
            row.Cells("soulLevel").Value = node.SoulLevel
            row.Cells("phantomType").Value = node.PhantomTypeText
            row.Cells("mpArea").Value = node.MPZone
            row.Cells("world").Value = node.WorldText
        Next
       
        'Delete old nodes.
        For i = dgvMPNodes.Rows.Count - 1 To 0 Step -1
            If Not nodes.ContainsKey(dgvMPNodes.Rows(i).Cells("steamId").Value) Then
                dgvMPNodes.Rows.Remove(dgvMPNodes.Rows(i))
            End If
        Next
        updateRecentNodes()
    End Sub
    Private Sub updateRecentNodes()
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\RecentNodes", True)

        Dim id As String
        Dim name As String

        Dim recentNodeDict As New Dictionary(Of String, DataGridViewRow)
        For Each row In dgvRecentNodes.Rows
            recentNodeDict.Add(row.Cells("steamId").Value, row)
        Next

        Dim currentTime As Long = (DateTime.UtcNow - New DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds
        For Each row In dgvMPNodes.Rows
            name = row.Cells("name").Value
            id = row.Cells("steamId").Value
            If id <> txtSelfSteamID.Text Then
                If Not recentNodeDict.ContainsKey(id)
                    dgvRecentNodes.Rows.Add(name, id, currentTime, "Y")
                Else
                    recentNodeDict(id).Cells("orderId").Value = currentTime
                End If
            End If
            key.SetValue(id, currentTime.ToString() & "|" & name)
        Next

        'Limit recent nodes to 70
        If dgvRecentNodes.Rows.Count > 70 Then
            Dim recentNodes As New List(Of DataGridViewRow)
            For Each row In dgvRecentNodes.Rows
                recentNodes.Add(row)
            Next

            'Breaking this to fix int32 errors.  Fix later.
            recentNodes = recentNodes.OrderBy(Function(row) CType(row.Cells("orderId").Value, Long)).ToList()
            For i  = 0 To dgvRecentNodes.Rows.Count - 70
                id = recentNodes(i).Cells(1).Value
                dgvRecentNodes.Rows.Remove(recentNodes(i))

                If Not key.GetValue(id) Is Nothing Then
                    key.DeleteValue(id)
                End If
            Next
        End If
    End Sub
    Private Sub chkExpand_CheckedChanged(sender As Object, e As EventArgs) Handles chkExpand.CheckedChanged
        If chkExpand.Checked Then
            Me.Width = 800
            Me.Height = 680
            tabs.Visible = True
            lblNewVersion.Visible = False
            btnAddFavorite.Visible = True
            btnRemFavorite.Visible = True
        Else
            Me.Width = 450
            Me.Height = 190
            tabs.Visible = False
            btnAddFavorite.Visible = False
            btnRemFavorite.Visible = False
        End If
    End Sub
    Private Sub nmbMaxNodes_ValueChanged(sender As Object, e As EventArgs) Handles nmbMaxNodes.ValueChanged
        If Not IsNothing(dsProcess) Then
            dsProcess.MaxNodes = nmbMaxNodes.Value
        End If
    End Sub
    Private Sub txtTargetSteamID_LostFocus(sender As Object, e As EventArgs) Handles txtTargetSteamID.LostFocus
        'Auto-convert Steam ID after clicking out of the textbox

        Dim steamIdInt As Int64
        If txtTargetSteamID.Text.Length > 1 Then
            txtTargetSteamID.Text = txtTargetSteamID.Text.Replace(" ", "")
            'Regex code contributed by Chronial
            'Allows copy/pasting entire Steam profile URL, assuming the URL ends with the SteamID
            Dim r As Regex = New Regex("https?://steamcommunity.com/profiles/(7\d+)/", RegexOptions.IgnoreCase)
            Dim m As Match = r.Match(txtTargetSteamID.Text)
            If m.Success Then
                steamIdInt = m.Groups.Item(1).Value
            ElseIf txtTargetSteamID.Text(0) = "7" Then
                'If it starts with a 7, assume it's the Steam64 ID in int64 form.
                steamIdInt = txtTargetSteamID.Text
            End If
            If steamIdInt Then
                txtTargetSteamID.Text = "0" & Hex(steamIdInt).ToLower
            End If
        End If
    End Sub
    Private Sub btnAttemptId_MouseClick(sender As Object, e As EventArgs) Handles btnAttemptId.Click
        'Added to make future automated connection attempts simpler
        dsProcess.connectToSteamId(txtTargetSteamID.Text)
    End Sub
    Private Sub dgvNodes_selected(sender As Object, e As EventArgs) Handles dgvFavoriteNodes.CellEnter,
        dgvRecentNodes.CellEnter, dgvDSCMNet.CellEnter

        If sender.SelectedCells.Count > 0 Then
            Dim rowindex As Integer = sender.SelectedCells(0).RowIndex
            Dim id As String = sender.Rows(rowindex).Cells(1).Value
            txtTargetSteamID.Text = id
        End If
    End Sub
    Private Sub dgvNodes_doubleclick(sender As Object, e As EventArgs) Handles dgvFavoriteNodes.DoubleClick,
        dgvRecentNodes.DoubleClick, dgvDSCMNet.DoubleClick
        btnAttemptId.PerformClick()
    End Sub
    Private Sub btnAddFavorite_Click(sender As Object, e As EventArgs) Handles btnAddFavorite.Click
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\FavoriteNodes", True)

        Select Case tabs.SelectedIndex
            'Active nodes selected
            Case 0
                If dgvMPNodes.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvMPNodes.SelectedCells(0).RowIndex
                    Dim name As String = dgvMPNodes.Rows(rowindex).Cells(0).Value
                    Dim id As String = dgvMPNodes.Rows(rowindex).Cells(1).Value

                    If key.GetValue(id) Is Nothing Then
                        key.SetValue(id, name)
                        dgvFavoriteNodes.Rows.Add(name, id)
                    End If
                Else
                    MsgBox("No selection detected.")
                End If

            'Recent nodes selected
            Case 2
                If dgvRecentNodes.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvRecentNodes.SelectedCells(0).RowIndex
                    Dim name As String = dgvRecentNodes.Rows(rowindex).Cells(0).Value
                    Dim id As String = dgvRecentNodes.Rows(rowindex).Cells(1).Value

                    If key.GetValue(id) Is Nothing Then
                        key.SetValue(id, name)
                        dgvFavoriteNodes.Rows.Add(name, id)
                    End If
                Else
                    MsgBox("No selection detected.")
                End If
            Case 3
                If dgvDSCMNet.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvDSCMNet.SelectedCells(0).RowIndex
                    Dim name As String = dgvDSCMNet.Rows(rowindex).Cells(0).Value
                    Dim id As String = dgvDSCMNet.Rows(rowindex).Cells(1).Value

                    If key.GetValue(id) Is Nothing Then
                        key.SetValue(id, name)
                        dgvFavoriteNodes.Rows.Add(name, id)
                    End If
                Else
                    MsgBox("No selection detected.")
                End If
        End Select
    End Sub
    Private Sub btnRemFavorite_Click(sender As Object, e As EventArgs) Handles btnRemFavorite.Click
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\FavoriteNodes", True)

        Select Case tabs.SelectedIndex

            'Active nodes selected
            Case 0
                If dgvMPNodes.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvMPNodes.SelectedCells(0).RowIndex
                    Dim id As String = dgvMPNodes.Rows(rowindex).Cells(1).Value

                    If Not key.GetValue(id) Is Nothing Then
                        key.DeleteValue(id)
                    End If

                    For i = dgvFavoriteNodes.Rows.Count - 1 To 0 Step -1
                        If dgvFavoriteNodes.Rows(i).Cells(1).Value = id Then
                            dgvFavoriteNodes.Rows.Remove(dgvFavoriteNodes.Rows(i))
                        End If
                    Next
                Else
                    MsgBox("No selection detected.")
                End If

            'Favorite nodes selected
            Case 1
                If dgvFavoriteNodes.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvFavoriteNodes.SelectedCells(0).RowIndex
                    Dim id As String = dgvFavoriteNodes.Rows(rowindex).Cells(1).Value

                    If Not key.GetValue(id) Is Nothing Then
                        key.DeleteValue(id)
                    End If

                    For i = dgvFavoriteNodes.Rows.Count - 1 To 0 Step -1
                        If dgvFavoriteNodes.Rows(i).Cells(1).Value = id Then
                            dgvFavoriteNodes.Rows.Remove(dgvFavoriteNodes.Rows(i))
                        End If
                    Next
                Else
                    MsgBox("No selection detected.")
                End If

            'Recent nodes selected
            Case 2
                If dgvRecentNodes.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvRecentNodes.SelectedCells(0).RowIndex
                    Dim id As String = dgvRecentNodes.Rows(rowindex).Cells(1).Value

                    If Not key.GetValue(id) Is Nothing Then
                        key.DeleteValue(id)
                    End If

                    For i = dgvFavoriteNodes.Rows.Count - 1 To 0 Step -1
                        If dgvFavoriteNodes.Rows(i).Cells(1).Value = id Then
                            dgvFavoriteNodes.Rows.Remove(dgvFavoriteNodes.Rows(i))
                        End If
                    Next
                Else
                    MsgBox("No selection detected.")
                End If

            'DSCMNet selected
            Case 3
                If dgvDSCMNet.SelectedCells.Count > 0 Then
                    Dim rowindex As Integer = dgvDSCMNet.SelectedCells(0).RowIndex
                    Dim id As String = dgvDSCMNet.Rows(rowindex).Cells(1).Value

                    If Not key.GetValue(id) Is Nothing Then
                        key.DeleteValue(id)
                    End If

                    For i = dgvFavoriteNodes.Rows.Count - 1 To 0 Step -1
                        If dgvFavoriteNodes.Rows(i).Cells(1).Value = id Then
                            dgvFavoriteNodes.Rows.Remove(dgvFavoriteNodes.Rows(i))
                        End If
                    Next
                Else
                    MsgBox("No selection detected.")
                End If
        End Select
    End Sub

    Private Shared Sub ircDebugWrite(ByRef str As String)
        SyncLock DSCM.trdLock
            ircDbgQueue.Add(TimeOfDay & ": " & str)
        End SyncLock
    End Sub
    Private Shared Function ircDebugRead()
        Dim str As String
        SyncLock DSCM.trdLock
            If ircDbgQueue.Count > 0 Then
                str = ircDbgQueue.Item(0)
                ircDbgQueue.Remove(str)
            Else
                str = Nothing
            End If
        End SyncLock
        Return str
    End Function
    Private Shared Sub ircOutWrite(ByRef str As String)
        SyncLock DSCM.trdLock
            ircOutQueue.Add(str)
        End SyncLock
    End Sub
    Private Shared Function ircOutRead()
        Dim str As String
        SyncLock DSCM.trdLock
            If ircOutQueue.Count > 0 Then
                str = ircOutQueue.Item(0)
                ircOutQueue.Remove(str)
            Else
                str = Nothing
            End If
        End SyncLock
        Return str
    End Function
    Private Shared Sub ircInWrite(ByRef str As String)
        SyncLock DSCM.trdLock
            ircInQueue.Add(str)
        End SyncLock
    End Sub
    Private Shared Function ircInRead()
        Dim str As String
        SyncLock DSCM.trdLock
            If ircInQueue.Count > 0 Then
                str = ircInQueue.Item(0)
                ircInQueue.Remove(str)
            Else
                str = Nothing
            End If
        End SyncLock
        Return str
    End Function

    Private Sub chkDSCMNet_CheckedChanged(sender As Object, e As EventArgs) Handles chkDSCMNet.CheckedChanged
        If chkDSCMNet.Checked Then
            ircDebugWrite("Spawning new thread.")
            ircTrd = New Thread(AddressOf ircMain)
            ircTrd.IsBackground = True
            ircTrd.Start()

            ircTimer = New System.Windows.Forms.Timer
            ircTimer.Interval = 60 * 1000
            ircTimer.Enabled = True
            ircTimer.Start()

            ircTimer2 = New System.Windows.Forms.Timer
            ircTimer2.Interval = 2 * 60 * 1000
            ircTimer2.Enabled = True
            ircTimer2.Start()
        Else
            ircDebugWrite("Attempting to quit.")
            ircOutWrite("QUIT")
            ircTimer.Stop()
            ircTimer2.Stop

        End If

    End Sub

    Public Sub ircMain(args As String())
        Dim port As Integer
        Dim buf As String, nick As String, owner As String, server As String, chan As String
        Dim sock As New System.Net.Sockets.TcpClient()

        Try
            ircDebugWrite("Waiting for local player info")
            While IsNothing(dsProcess) OrElse dsProcess.SelfNode.CharacterName = ""

            End While

            nick = "DSCM-" & dsProcess.SelfSteamId
            owner = "DSCMbot"
            server = "dscm.wulf2k.ca"
            port = 8123
            chan = "#DSCM-Main"


            ircDebugWrite("Initiating connection")
            'Connect to irc server and get input and output text streams from TcpClient.
            sock.Connect(server, port)
            If Not sock.Connected Then
                ircDebugWrite("Failed to connect.")
                Console.WriteLine("Failed to connect!")
                Return
            End If
            input = New System.IO.StreamReader(sock.GetStream())
            output = New System.IO.StreamWriter(sock.GetStream())

            'USER and NICK login commands 
            output.Write("USER " & nick & " 0 * :" & owner & vbCr & vbLf & "NICK " & nick & vbCr & vbLf)
            output.Flush()

            output.Write("MODE " & nick & " +B" & vbCr & vbLf)
            output.Flush()

            ircOnline = False

            'Join channel on start
            While Not ircOnline
                buf = input.ReadLine()
                If Not buf Is Nothing Then
                    ircInWrite(buf)

                    If buf.StartsWith("PING ") Then
                        output.Write(buf.Replace("PING", "PONG") & vbCr & vbLf)
                        output.Flush()
                    End If


                    If buf.Contains(":MOTD") Then
                        output.write("JOIN " & chan & vbCr & vbLf)
                        output.flush()
                        ircOnline = True
                    End If
                End If
            End While

            ircDebugWrite("Connected.")
            'Process each line received from irc server
            While ircOnline
                'Will actually stop here until it receives input from server.
                'Queued outbound commands will not process until this receives input.
                buf = input.ReadLine()

                If Not buf Is Nothing Then
                    ircInWrite(buf)

                    'Send pong reply to any ping messages
                    If buf.StartsWith("PING ") Then
                        output.Write(buf.Replace("PING", "PONG") & vbCr & vbLf)
                        output.Flush()
                    End If

                    'Parse report commands
                    If buf.Contains("REPORT|") Then
                        'Report passed to main form.
                    End If
                End If

                'Parse commands from main form

                Dim cmd = ircOutRead()
                If Not cmd Is Nothing Then
                    If cmd = "QUIT" Then
                        ircDebugWrite("Quit attempt processed.")
                        output.Write("QUIT" & vbCr & vbLf)
                        output.Flush()

                        ircOnline = False
                    Else
                        output.write(cmd & vbCr & vbLf)
                        output.flush()
                    End If
                End If
            End While
            ircDebugWrite("Disconnected.")
        Catch ex As Exception
            ircDebugWrite("Unknown error in DSCMNet - " & ex.Message)
        End Try
    End Sub
End Class
