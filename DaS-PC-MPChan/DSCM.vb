Imports System.Threading
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Net.Sockets


Public Class DSCM
    'Timers
    Private WithEvents refMpData As New System.Windows.Forms.Timer()
    Private WithEvents refTimer As New System.Windows.Forms.Timer()
    Private WithEvents onlineTimer As New System.Windows.Forms.Timer()
    Private WithEvents ircConnectTimer As New System.Windows.Forms.Timer()
    Private WithEvents dsProcessTimer As New System.Windows.Forms.Timer()
    Private WithEvents hotkeyTimer As New System.Windows.Forms.Timer()

    'For hotkey support
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Short

    'Thread to check for updates
    Private updTrd As Thread


    'Hotkeys
    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean

    Public Version As String
    'New version of DSCM available?
    Dim newstablever As Boolean = False
    Dim newtestver As Boolean = False

    Private dsProcess As DarkSoulsProcess = Nothing
    Private _ircClient As IRCClient = Nothing
    Private ircDisplayList As New SortableBindingList(Of DSNode)(New List(Of DSNode))
    
    Private recentConnections As New Queue(Of Tuple(Of Date, String))

    Private Sub DSCM_Close(sender As Object, e As EventArgs) Handles MyBase.FormClosed
        chkDebugDrawing.Checked = False
    End Sub
    Private Sub DSCM_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Version = lblVer.Text
        'Start Refresh timer
        refTimer.Interval = 200
        refTimer.Start()

        hotkeyTimer.Interval = 10
        hotkeyTimer.Start()

        refMpData.Interval = 5000
        refMpData.Start()

        ircConnectTimer.Interval = 20000

        dsProcessTimer.Interval = 1000
        dsProcessTimer.Start()

        attachDSProcess()

        'Set initial form size to non-expanded
        Me.Width = 450
        Me.Height = 190

        Dim AlternateRowColor = Color.FromArgb(&HFFE3E3E3)

        With dgvMPNodes
            .Columns.Add("name", "Name")
            .Columns(0).Width = 180
            .Columns(0).ValueType = GetType(String)
            .Columns.Add("steamId", "Steam ID")
            .Columns(1).Width = 145
            .Columns(1).ValueType = GetType(String)
            .Columns.Add("soulLevel", "SL")
            .Columns(2).Width = 60
            .Columns(2).ValueType = GetType(Integer)
            .Columns.Add("phantomType", "Phantom Type")
            .Columns(3).Width = 80
            .Columns(3).ValueType = GetType(String)
            .Columns.Add("mpArea", "MP Area")
            .Columns(4).Width = 80
            .Columns(4).ValueType = GetType(Integer)
            .Columns.Add("world", "World")
            .Columns(5).Width = 200
            .Columns(5).ValueType = GetType(String)
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
        End With

        With dgvFavoriteNodes
            .Columns.Add("name", "Name")
            .Columns(0).Width = 180
            .Columns(0).ValueType = GetType(String)
            .Columns.Add("steamId", "Steam ID")
            .Columns(1).Width = 145
            .Columns(1).ValueType = GetType(String)
            .Columns.Add("isOnline", "O")
            .Columns(2).Width = 20
            .Columns(2).ValueType = GetType(String)
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
        End With

        With dgvRecentNodes
            .Columns.Add("name", "Name")
            .Columns(0).Width = 180
            .Columns(0).ValueType = GetType(String)
            .Columns.Add("steamId", "Steam ID")
            .Columns(1).Width = 145
            .Columns(1).ValueType = GetType(String)
            .Columns.Add("orderId", "Order ID")
            .Columns(2).Visible = False
            .Columns(2).ValueType = GetType(Long)
            .Columns.Add("isOnline", "O")
            .Columns(3).Width = 20
            .Columns(3).ValueType = GetType(String)
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
        End With

        With dgvDSCMNet
            .AutoGenerateColumns = False
            .DataSource = ircDisplayList
            .Columns.Add("name", "Name")
            .Columns("name").Width = 135
            .Columns("name").DataPropertyName = "CharacterNameColumn"
            .Columns.Add("steamId", "Steam ID")
            .Columns("steamId").Width = 145
            .Columns("steamId").DataPropertyName = "SteamIdColumn"
            .Columns("steamId").Visible = False
            .Columns.Add("soulLevel", "SL")
            .Columns("soulLevel").Width = 40
            .Columns("soulLevel").DataPropertyName = "SoulLevelColumn"
            .Columns.Add("phantomType", "Phantom Type")
            .Columns("phantomType").Width = 70
            .Columns("phantomType").DataPropertyName = "PhantomTypeText"
            .Columns.Add("mpArea", "MP Area")
            .Columns("mpArea").Width = 60
            .Columns("mpArea").DataPropertyName = "MPZoneColumn"
            .Columns.Add("world", "World")
            .Columns("world").Width = 195
            .Columns("world").DataPropertyName = "WorldText"
            .Columns.Add("covenant", "Covenant")
            .Columns("covenant").Width = 165
            .Columns("covenant").DataPropertyName = "CovenantColumn"
            .Columns.Add("indictments", "Sin")
            .Columns("indictments").Width = 60
            .Columns("indictments").DataPropertyName = "IndictmentsColumn"
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
        End With

        InitDarkmoonTab()

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

        loadReadme()

        onlineTimer.Enabled = True
        onlineTimer.Interval = 10 * 60 * 1000
        onlineTimer.Start()

        chkExpand.Checked = True
        chkDSCMNet.Checked = True
    End Sub
    Private Sub InitDarkmoonTab()
        'Darkmoon tab purpose: enhanced matchmaking experience for members of the "Blade of the Darkmoon" covenant.
        'Intended features:
        '-Scrape players with sin from DSCM-Net
        '-Scrape sinners by a preferred order
        '-Scrape sinners starting closest to player
        '-Flush nodes of non-sinners to provide space for sinners
        With tabDarkmoon
            Dim dkmPreferencesList As New ArrayList()
            With dkmPreferencesList
                .Add(New DkmPref("Disabled", 0, "Searching that prefers nodes of sinners is disabled."))
                .Add(New DkmPref("Indiscriminate", -1, "Justice indiscriminate. Sinners are selected from DSCM-Net without any additional sorting beyond the normal."))
                '.Add(New DkmPref("Random", 1, "Retribution strikes at random, striking fear into all. Sinners are explicitly selected at random from DSCM-Net."))
                '.Add(New DkmPref("Weak Sinners", -2, "The power of the Blades of the Darkmoon is overpowering. Sinners are selected from DSCM-Net starting with the ones with the lowest permissible Soul Level."))
                '.Add(New DkmPref("Strong Sinners", 2, "The Blades of the Darkmoon pursue the most dangerous sinners. Sinners are selected from DSCM-Net starting with the ones with the highest permissible Soul Level."))
                '.Add(New DkmPref("Petty Sinners", -3, "Even the smallest of crimes do not go unpunished. Sinners are selected from DSCM-Net starting with the ones who have sinned the least."))
                '.Add(New DkmPref("Dire Sinners", 3, "The most guilty are the ones most deserving of punishment. Sinners are selected from DSCM-Net starting with the ones who have sinned the most."))
            End With
            With DkmPrefBox
                .DataSource = dkmPreferencesList
                .DisplayMember = "Name"
                .ValueMember = "Value"
                .AutoCompleteMode = AutoCompleteMode.Suggest
                .DropDownStyle = ComboBoxStyle.DropDownList
                .SelectedIndex = 1
            End With
            With DkmPrefHelpTextBox
                Dim Pref As DkmPref = DkmPrefBox.SelectedItem
                .Text = Pref.HelpText
            End With
        End With
    End Sub
    Private Sub loadReadme()
        Dim body = My.Resources.Readme
        txtHelpView.Text = body
    End Sub
    Private Sub helpView_Navigating(sender As System.Object, e As System.Windows.Forms.WebBrowserNavigatingEventArgs)
        If e.Url.ToString <> "about:blank" Then
            e.Cancel = True 'Cancel the event to avoid default behavior
            System.Diagnostics.Process.Start(e.Url.ToString()) 'Open the link in the default browser
        End If
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

            newstablever = (stablever > Version.Replace(".", ""))
            newtestver = (testver > Version.Replace(".", ""))
        Catch ex As Exception
            'Fail silently since nobody wants to be bothered for an update check.
        End Try
    End Sub
    Private Sub connectToIRCNode() Handles ircConnectTimer.Tick
        If (_ircClient Is Nothing OrElse
                dsProcess Is Nothing OrElse
                dsProcess.SelfSteamId = "" OrElse
                dsProcess.SelfNode.CharacterName = "" OrElse
                dsProcess.SelfNode.PhantomType = -1) Then
            'We either can't connect to other players yet or are lacking the
            'neccessary information to make a good choice (our character is not loaded)
            Return
        End If
        Dim ReservedSteamNodeCount As Integer = 4
        If dsProcess.NodeCount < dsProcess.MaxNodes - ReservedSteamNodeCount Then
            Dim blacklist As New List(Of String)
            For Each c In recentConnections
                blacklist.Add(c.Item2)
            Next
            For Each n In dsProcess.ConnectedNodes.Values
                blacklist.Add(n.SteamId)
            Next
            Dim candidate As DSNode = _ircClient.GetNodeForConnecting(dsProcess.SelfNode, blacklist)
            If candidate IsNot Nothing Then
                connectToSteamId(candidate.SteamId)
            End If
        End If
    End Sub
    
    Private Sub refTimer_Tick() Handles refTimer.Tick
        Dim dbgboost As Integer = 0
        Dim tmpptr As Integer = 0

        'Text indicating new version is hidden if DSCM is expanded, only care if it's seen at the start anyway.
        If newtestver Or newstablever Then
            lblNewVersion.Visible = true
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

        ' Sync IRC Nodes from client to display list
        If _ircClient IsNot Nothing Then
            Dim seenNodes As New HashSet(Of String)
            For i = ircDisplayList.Count - 1 To 0 Step -1
                Dim node As DSNode = ircDisplayList(i)
                If Not _ircClient.ircNodes.ContainsKey(node.SteamId) Then
                    ircDisplayList.RemoveAt(i)
                Else
                    seenNodes.Add(node.SteamId)
                    Dim ircNode As DSNode = _ircClient.ircNodes(node.SteamId).Item1
                    If Not node.MemberwiseEquals(ircNode) Then
                        ircDisplayList(i) = ircNode
                    End If
                End If
            Next
            For Each t In _ircClient.ircNodes.Values
                If Not seenNodes.Contains(t.Item1.SteamId) Then
                    ircDisplayList.Add(t.Item1)
                End If
            Next
        End If

        If chkDSCMNet.Checked and Not tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")" Then
            tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")"
        End If
    End Sub
    Private Shared Sub hotkeyTimer_Tick() Handles hotkeytimer.Tick
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

        txtHelpView.Width = dgvMPNodes.Width
        txtHelpView.Height = dgvMPNodes.Height

        dgvFavoriteNodes.Height = Me.Height - 225
        dgvRecentNodes.Height = Me.Height - 225

        dsProcessStatus.Location = New Point(10, Me.Height - 63)
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

    Private Sub attachDSProcess() Handles dsProcessTimer.Tick
        If dsProcess isNot Nothing Then
            If Not dsProcess.IsAttached
                dsProcess.Dispose()
                dsProcess = Nothing
            End If
        End If
        If dsProcess is Nothing Then
            Try
                dsProcess = New DarkSoulsProcess()
                dsProcessStatus.Text = " Attached to Dark Souls process"
                dsProcessStatus.BackColor = System.Drawing.Color.FromArgb(200, 255, 200)
            Catch ex As DSProcessAttachException
                dsProcessStatus.Text = " " & ex.Message
                dsProcessStatus.BackColor = System.Drawing.Color.FromArgb(255, 200, 200)
            End Try
        End If
    End Sub

    Private Sub chkDebugDrawing_CheckedChanged(sender As Object, e As EventArgs) Handles chkDebugDrawing.CheckedChanged
        If IsNothing(dsProcess) Then
            chkDebugDrawing.Checked = False
            Exit Sub
        End If
        dsProcess.DrawNodes = chkDebugDrawing.Checked
    End Sub

    Private Sub refMpData_Tick() Handles refMpData.Tick
        Dim nodes As Dictionary(Of String, DSNode)
        Dim selfNode As DSNode = Nothing
        If dsProcess Is Nothing Then
            nodes = New Dictionary(Of String, DSNode)()
        Else
            dsProcess.UpdateNodes()
            If dsProcess.SelfNode.SteamId Is Nothing Then Return
            nodes = New Dictionary(Of String, DSNode)(dsProcess.ConnectedNodes)
            selfNode = dsProcess.SelfNode
        End If

        If _ircClient IsNot Nothing
            _ircClient.setLocalNodes(selfNode, nodes.Values)
        End If

        If selfNode IsNot Nothing Then
            nodes.Add(selfNode.SteamId, selfNode)
        End If
        For Each node As DSNode In nodes.Values
            Dim row As DataGridViewRow = Nothing
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
    Private Sub connectToSteamId(steamId As String)
        If dsProcess IsNot Nothing Then
            Try
                dsProcess.ConnectToSteamId(steamId)
            Catch ex As DSConnectException
                dsProcessStatus.Text = " Connect failed: " & ex.Message
                dsProcessStatus.BackColor = System.Drawing.Color.FromArgb(255, 153, 51)
                Return
            End Try

            Dim now As Date = DateTime.UtcNow
            recentConnections.Enqueue(Tuple.Create(now, steamId))
            While (now - recentConnections.Peek().Item1).TotalMinutes > 5
                recentConnections.Dequeue()
            End While
        End If
    End Sub
    Private Sub btnAttemptId_MouseClick(sender As Object, e As EventArgs) Handles btnAttemptId.Click
        'Added to make future automated connection attempts simpler
        connectToSteamId(txtTargetSteamID.Text)
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

    Private Sub chkDSCMNet_CheckedChanged(sender As Object, e As EventArgs) Handles chkDSCMNet.CheckedChanged
        If chkDSCMNet.Checked Then
            _ircClient = New IRCClient(Me)
            ircConnectTimer.Start()
        Else
            If _ircClient IsNot Nothing
                ircConnectTimer.Stop()
                _ircClient.Shutdown()
                _ircClient = Nothing
                ircDisplayList.Clear()
            End If
        End If
    End Sub

    Private Sub dgvNodes_selected(sender As Object, e As DataGridViewCellEventArgs) Handles dgvRecentNodes.CellEnter, dgvFavoriteNodes.CellEnter, dgvDSCMNet.CellEnter

    End Sub

    Private Sub DkmPrefBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles DkmPrefBox.SelectedIndexChanged
        Dim PrefBox As ComboBox = sender
        Dim Pref As DkmPref = DkmPrefBox.SelectedItem
        DkmPrefHelpTextBox.Text = Pref.HelpText
    End Sub
End Class