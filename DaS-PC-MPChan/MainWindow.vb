Imports System.Threading
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Net.Sockets
Imports System.ComponentModel
Imports System.Text


Public Class MainWindow
    'Timers
    Private WithEvents updateActiveNodesTimer As New System.Windows.Forms.Timer()
    Private WithEvents updateUITimer As New System.Windows.Forms.Timer()
    Private WithEvents updateOnlineStateTimer As New System.Windows.Forms.Timer()
    Private WithEvents ircNodeConnectTimer As New System.Windows.Forms.Timer()
    Private WithEvents dsAttachmentTimer As New System.Windows.Forms.Timer()
    Private WithEvents hotkeyTimer As New System.Windows.Forms.Timer()

    'For hotkey support
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Short

    'Hotkeys
    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean

    Public Version As String

    Private dsProcess As DarkSoulsProcess = Nothing
    Private _ircClient As IRCClient = Nothing
    Private ircDisplayList As New DSNodeBindingList()
    Private activeNodesDisplayList As New DSNodeBindingList()

    Private recentConnections As New Queue(Of Tuple(Of Date, String))

    Private Sub DSCM_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        If dsProcess IsNot Nothing Then
            dsProcess.Dispose()
            dsProcess = Nothing
        End If
        If _ircClient IsNot Nothing Then
            'Give the IRC Client a chance to quit gracefully
            _ircClient.Shutdown()
            _ircClient.Join(100)
        End If
    End Sub
    Private Sub DSCM_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Version = lblVer.Text

        Dim oldFileArg As String = Nothing
        For Each arg In Environment.GetCommandLineArgs().Skip(1)
            If arg.StartsWith("--old-file=") Then
                oldFileArg = arg.Substring("--old-file=".Length)
            Else
                MsgBox("Unknown command line arguments")
                oldFileArg = Nothing
                Exit For
            End If
        Next
        If oldFileArg IsNot Nothing Then
            If oldFileArg.EndsWith(".old") Then
                Dim t = New Thread(
                    Sub()
                    Try
                        'Give the old version time to shut down
                        Thread.Sleep(1000)
                        File.Delete(oldFileArg)
                    Catch ex As Exception
                        Me.Invoke(Function() MsgBox("Deleting old version failed: " & vbCrLf & ex.Message, MsgBoxStyle.Exclamation))
                    End Try
                End Sub)
                t.Start()
            Else
                MsgBox("Deleting old version failed: Invalid filename ", MsgBoxStyle.Exclamation)
            End If
        End If


        txtTargetSteamID.SetPlaceholder(txtTargetSteamID.Text)
        txtTargetSteamID.Text = ""

        updateUITimer.Interval = 200
        updateUITimer.Start()
        hotkeyTimer.Interval = 10
        hotkeyTimer.Start()
        updateActiveNodesTimer.Interval = 5000
        updateActiveNodesTimer.Start()
        dsAttachmentTimer.Interval = 1000
        dsAttachmentTimer.Start()
        updateOnlineStateTimer.Interval = 10 * 60 * 1000
        updateOnlineStateTimer.Start()
        ircNodeConnectTimer.Interval = 20 * 1000

        attachDSProcess()

        setupGridViews()

        'Create regkeys if they don't exist
        My.Computer.Registry.CurrentUser.CreateSubKey("Software\DSCM\FavoriteNodes")
        My.Computer.Registry.CurrentUser.CreateSubKey("Software\DSCM\RecentNodes")
        My.Computer.Registry.CurrentUser.CreateSubKey("Software\DSCM\Options")

        loadFavoriteNodes()
        loadRecentNodes()
        loadOptions()
        loadReadme()

        'Resize window
        chkExpand_CheckedChanged()

        updatecheck()
        updateOnlineState()
    End Sub
    Private Sub setupGridViews()
        Dim AlternateRowColor = Color.FromArgb(&HFFE3E3E3)

        With dgvMPNodes
            .AutoGenerateColumns = False
            .DataSource = activeNodesDisplayList
            .Columns.Add("name", "Name")
            .Columns("name").Width = 180
            .Columns("name").DataPropertyName = "CharacterNameColumn"
            .Columns.Add("steamId", "Steam ID")
            .Columns("steamId").Width = 145
            .Columns("steamId").DataPropertyName = "SteamIdColumn"
            .Columns.Add("soulLevel", "SL")
            .Columns("soulLevel").Width = 60
            .Columns("soulLevel").DataPropertyName = "SoulLevelColumn"
            .Columns("soulLevel").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Columns.Add("phantomType", "Phantom Type")
            .Columns("phantomType").Width = 80
            .Columns("phantomType").DataPropertyName = "PhantomTypeText"
            .Columns.Add("mpArea", "MP Area")
            .Columns("mpArea").Width = 60
            .Columns("mpArea").DataPropertyName = "MPZoneColumn"
            .Columns.Add("world", "World")
            .Columns("world").Width = 200
            .Columns("world").DataPropertyName = "WorldText"
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .Sort(.Columns("soulLevel"), ListSortDirection.Ascending)
            .Sort(.Columns("mpArea"), ListSortDirection.Ascending)
            .Sort(.Columns("world"), ListSortDirection.Descending)
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
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With

        With dgvRecentNodes
            .AutoGenerateColumns = False
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
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
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
            .Columns("soulLevel").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
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
            .Columns("indictments").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Font = New Font("Consolas", 10)
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .Sort(.Columns("steamId"), ListSortDirection.Ascending)
            .Sort(.Columns("soulLevel"), ListSortDirection.Descending)
        End With
    End Sub
    Private Sub loadReadme()
        Dim html As XElement =
            <html>
                <head>
                    <style>
                        body {font-family: Calibri}
                        ol, ul {margin-bottom: 1em}
                        h1 {border-bottom: 1px solid black}
                    </style>
                </head>
                <body>###</body>
            </html>

        Dim htmlString = html.ToString()
        helpView.DocumentText = htmlString.Replace("###", My.Resources.Readme)
        helpView.IsWebBrowserContextMenuEnabled = False
        helpView.AllowWebBrowserDrop = False
    End Sub
    Private Sub helpView_Navigating(sender As System.Object, e As System.Windows.Forms.WebBrowserNavigatingEventArgs) Handles helpView.Navigating
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
    Private Sub loadOptions()
        Dim key As Microsoft.Win32.RegistryKey
        Dim regval As String

        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\Options", True)

        regval = key.GetValue("ExpandDSCM")
        If regval Is Nothing Then key.SetValue("ExpandDSCM", "True")

        regval = key.GetValue("JoinDSCM-Net")
        If regval Is Nothing Then key.SetValue("JoinDSCM-Net", "True")


        chkExpand.Checked = (key.GetValue("ExpandDSCM") = "True")
        chkDSCMNet.Checked = (key.GetValue("JoinDSCM-Net") = "True")
    End Sub
    Private Sub updateOnlineState_Tick() Handles updateOnlineStateTimer.Tick
        updateOnlineState()
    End Sub
    Private Async Sub updateOnlineState()
        Dim steamIds = New HashSet(Of String)
        For Each Row In dgvRecentNodes.Rows
            steamIds.Add(Row.Cells("steamId").Value)
        Next
        For Each Row In dgvFavoriteNodes.Rows
            If steamIds.Count < 100 Then steamIds.Add(Row.Cells("steamId").Value)
        Next
        Dim converter As New Converter(Of String, String)(Function(num) Convert.ToInt64(num, 16).ToString())
        Dim idQuery = String.Join(",", Array.ConvertAll(steamIds.ToArray(), converter))
        Dim uri = "http://chronial.de/scripts/dscm/is_online.php?ids=" & idQuery
        Dim client As New Net.WebClient()
        Dim contents() As Byte = Await client.DownloadDataTaskAsync(uri)

        Dim onlineInfo = New Dictionary(Of Int64, Boolean)
        Try
            Dim parser As New FileIO.TextFieldParser(New MemoryStream(contents))
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
    Private Async Sub updatecheck()
        Try
            Dim client As New Net.WebClient()
            Dim uri = "http://wulf2k.ca/pc/das/dscm-ver.txt"
            Dim content As String = Await client.DownloadStringTaskAsync(uri)

            Dim lines() As String = content.Split({vbCrLf, vbLf}, StringSplitOptions.None)
            Dim stableVersion = lines(0)
            Dim stableUrl = lines(2)
            Dim testVersion = lines(1)
            Dim testUrl = lines(3)

            If stableVersion > Version.Replace(".", "") Then
                lblNewVersion.Visible = True
                btnUpdate.Visible = True
                btnUpdate.Tag = stableUrl
                lblNewVersion.Text = "New stable version available"
            ElseIf testVersion > Version.Replace(".", "") Then
                lblNewVersion.Visible = True
                btnUpdate.Visible = True
                btnUpdate.Tag = testUrl
                lblNewVersion.Text = "New testing version available"
            End If
        Catch ex As Exception
            'Fail silently since nobody wants to be bothered for an update check.
        End Try
    End Sub
    Private Sub btnUpdate_Click(sender As Button, e As EventArgs) Handles btnUpdate.Click
        Dim updateWindow As New UpdateWindow(sender.Tag)
        updateWindow.ShowDialog()
        If updateWindow.WasSuccessful Then
            If dsProcess IsNot Nothing Then
                dsProcess.Dispose()
                dsProcess = Nothing
            End If
            Process.Start(updateWindow.NewAssembly, """--old-file=" & updateWindow.OldAssembly & """")
            Me.Close()
        End If
    End Sub
    Private Sub connectToIRCNode() Handles ircNodeConnectTimer.Tick
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

    Private Sub updateUI() Handles updateUITimer.Tick
        If dsProcess Is Nothing Then
            nmbMaxNodes.Enabled = False
            nmbMaxNodes.BackColor = New Color()
        Else
            'Node display
            'Changes the comparison instruction to display it if value is 0, rather than changing the value itself
            chkDebugDrawing.Checked = dsProcess.DrawNodes

            Dim maxNodes = dsProcess.MaxNodes
            If maxNodes >= nmbMaxNodes.Minimum And maxNodes <= nmbMaxNodes.Maximum Then
                nmbMaxNodes.Value = maxNodes
                nmbMaxNodes.Enabled = True
                nmbMaxNodes.BackColor = New Color()
            Else
                nmbMaxNodes.Enabled = False
                nmbMaxNodes.BackColor = System.Drawing.Color.FromArgb(255, 200, 200)
            End If

            'Don't update the text box if it's clicked in, so people can copy/paste without losing cursor.
            'Probably don't need to update this more than once anyway, but why not?
            If Not txtSelfSteamID.Focused Then
                txtSelfSteamID.Text = dsProcess.SelfSteamId
            End If

            txtCurrNodes.Text = dsProcess.NodeCount
        End If

        If _ircClient IsNot Nothing Then
            ircDisplayList.SyncWithDict(_ircClient.ircNodes, Function(x) x.Item1, dgvDSCMNet)
        End If

        If Not tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")" Then
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

        If (ctrlkey And oneKey) And Not (MainWindow.ctrlHeld And MainWindow.oneHeld) Then
            MainWindow.chkDebugDrawing.Checked = Not MainWindow.chkDebugDrawing.Checked
        End If


        If (ctrlkey And twoKey) And Not (MainWindow.ctrlHeld And MainWindow.twoheld) Then
            'Hotkey available
        End If

        MainWindow.ctrlHeld = ctrlkey
        MainWindow.oneHeld = oneKey
        MainWindow.twoheld = twoKey
    End Sub
    Private Sub attachDSProcess() Handles dsAttachmentTimer.Tick
        If dsProcess IsNot Nothing Then
            If Not dsProcess.IsAttached Then
                dsProcess.Dispose()
                dsProcess = Nothing
            End If
        End If
        If dsProcess Is Nothing Then
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

    Private Sub updateActiveNodes() Handles updateActiveNodesTimer.Tick
        Dim nodes As New Dictionary(Of String, DSNode)
        Dim selfNode As DSNode = Nothing
        If dsProcess IsNot Nothing Then
            dsProcess.UpdateNodes()
            If dsProcess.SelfNode.SteamId Is Nothing Then Return
            For Each kv In dsProcess.ConnectedNodes
                nodes(kv.Key) = kv.Value.Clone()
            Next
            selfNode = dsProcess.SelfNode.Clone()
        End If

        If _ircClient IsNot Nothing Then
            _ircClient.setLocalNodes(selfNode, nodes.Values)
        End If

        If selfNode IsNot Nothing Then
            nodes.Add(selfNode.SteamId, selfNode)
        End If
        activeNodesDisplayList.SyncWithDict(nodes)
        updateRecentNodes()
    End Sub
    Private Sub updateRecentNodes()
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\RecentNodes", True)

        Dim recentNodeDict As New Dictionary(Of String, DataGridViewRow)
        For Each row In dgvRecentNodes.Rows
            recentNodeDict.Add(row.Cells("steamId").Value, row)
        Next

        Dim currentTime As Long = (DateTime.UtcNow - New DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds
        For Each node In activeNodesDisplayList
            If node.SteamId <> txtSelfSteamID.Text Then
                If Not recentNodeDict.ContainsKey(node.SteamId) Then
                    dgvRecentNodes.Rows.Add(node.CharacterName, node.SteamId, currentTime, "Y")
                Else
                    recentNodeDict(node.SteamId).Cells("orderId").Value = currentTime
                End If
            End If
            key.SetValue(node.SteamId, currentTime.ToString() & "|" & node.CharacterName)
        Next

        'Limit recent nodes to 70
        If dgvRecentNodes.Rows.Count > 70 Then
            Dim recentNodes As New List(Of DataGridViewRow)
            For Each row In dgvRecentNodes.Rows
                recentNodes.Add(row)
            Next

            recentNodes = recentNodes.OrderBy(Function(row) CType(row.Cells("orderId").Value, Long)).ToList()
            For i = 0 To dgvRecentNodes.Rows.Count - 70
                Dim id As String = recentNodes(i).Cells(1).Value
                dgvRecentNodes.Rows.Remove(recentNodes(i))

                If Not key.GetValue(id) Is Nothing Then
                    key.DeleteValue(id)
                End If
            Next
        End If
    End Sub
    Private Sub chkExpand_CheckedChanged() Handles chkExpand.CheckedChanged
        Dim key As Microsoft.Win32.RegistryKey

        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\Options", True)
        key.SetValue("ExpandDSCM", chkExpand.Checked)

        If chkExpand.Checked Then
            Me.Width = 800
            Me.Height = 680
            tabs.Visible = True
            btnAddFavorite.Visible = True
            btnRemFavorite.Visible = True
        Else
            Me.Width = 500
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
        If String.IsNullOrWhiteSpace(txtTargetSteamID.Text) Then
            MsgBox("No target for connection given", MsgBoxStyle.Critical)
            Return
        End If
        Dim idString As String = txtTargetSteamID.Text.Replace(" ", "")

        If Not Regex.IsMatch(idString, "^\d+$") Then
            Dim m As Match = Regex.Match(idString, "https?://steamcommunity.com/profiles/(7\d+)")
            If m.Success Then
                'The url contains the steamid, no need for a network request
                idString = m.Groups.Item(1).Value
            ElseIf Regex.IsMatch(idString, "^https?://steamcommunity.com/") Then
                'Get the steamid via api request
                Try
                    Dim url As String = idString.Split("?")(0) & "?xml=1"
                    Dim document As New Xml.XmlDocument()
                    document.Load(url)
                    Dim idNode = document.SelectSingleNode("/profile/steamID64")
                    idString = idNode.InnerText
                Catch ex As Exception
                    'We display an error message later on
                End Try
            End If
        End If

        If idString(0) = "7" Then
            'If it starts with a 7, assume it's the Steam64 ID in int64 form.
            Try
                Dim steamIdInt As Int64 = idString
                idString = "0" & Hex(steamIdInt).ToLower
            Catch ex As InvalidCastException
                'We display an error message later on
            End Try
        End If
        Dim validTarget As Boolean = False
        If idString.Length = 16 Then
            Try
                Convert.ToInt64(idString, 16)
                validTarget = True
            Catch ex As Exception
            End Try
        End If
        If Not validTarget Then
            MsgBox("The given target could not be converted to a Steam64 ID:" & vbCrLf & txtTargetSteamID.Text, MsgBoxStyle.Critical)
            Return
        End If
        If dsProcess Is Nothing Then
            MsgBox("You can only connect to other players while Dark Souls is running.", MsgBoxStyle.Critical)
            Return
        End If
        connectToSteamId(idString)
    End Sub
    Private Function getSelectedNode() As Tuple(Of String, String)
        Dim currentGrid As DataGridView = Nothing
        If tabs.SelectedTab Is tabActive Then
            currentGrid = dgvMPNodes
        ElseIf tabs.SelectedTab Is tabRecent Then
            currentGrid = dgvRecentNodes
        ElseIf tabs.SelectedTab Is tabFavorites Then
            currentGrid = dgvFavoriteNodes
        ElseIf tabs.SelectedTab Is tabDSCMNet Then
            currentGrid = dgvDSCMNet
        Else
            Return Nothing
        End If

        Dim name As String = currentGrid.CurrentRow.Cells("name").Value
        Dim steamId As String = currentGrid.CurrentRow.Cells("steamId").Value
        Return Tuple.Create(steamId, name)
    End Function
    Private Sub dgvNodes_doubleclick(sender As DataGridView, e As EventArgs) Handles dgvFavoriteNodes.DoubleClick,
        dgvRecentNodes.DoubleClick, dgvDSCMNet.DoubleClick
        connectToSteamId(sender.CurrentRow.Cells("steamId").Value)
    End Sub
    Private Sub btnAddFavorite_Click(sender As Object, e As EventArgs) Handles btnAddFavorite.Click
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\FavoriteNodes", True)

        Dim selectedNode = getSelectedNode()
        If selectedNode Is Nothing Then
            MsgBox("No selection detected.")
            Return
        End If

        If key.GetValue(selectedNode.Item1) Is Nothing Then
            key.SetValue(selectedNode.Item1, selectedNode.Item2)
            dgvFavoriteNodes.Rows.Add(selectedNode.Item2, selectedNode.Item1)
        End If
    End Sub
    Private Sub btnRemFavorite_Click(sender As Object, e As EventArgs) Handles btnRemFavorite.Click
        Dim key As Microsoft.Win32.RegistryKey
        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\FavoriteNodes", True)

        Dim selectedNode = getSelectedNode()
        If selectedNode Is Nothing Then
            MsgBox("No selection detected.")
            Return
        End If

        Dim steamId As String = selectedNode.Item1

        If Not key.GetValue(steamId) Is Nothing Then
            key.DeleteValue(steamId)
        End If

        For i = dgvFavoriteNodes.Rows.Count - 1 To 0 Step -1
            If dgvFavoriteNodes.Rows(i).Cells("steamId").Value = steamId Then
                dgvFavoriteNodes.Rows.Remove(dgvFavoriteNodes.Rows(i))
            End If
        Next
    End Sub

    Private Sub chkDSCMNet_CheckedChanged(sender As Object, e As EventArgs) Handles chkDSCMNet.CheckedChanged
        Dim key As Microsoft.Win32.RegistryKey

        key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\Options", True)
        key.SetValue("JoinDSCM-Net", chkDSCMNet.Checked)

        If chkDSCMNet.Checked Then
            _ircClient = New IRCClient(Me)
            ircNodeConnectTimer.Start()
        Else
            If _ircClient IsNot Nothing Then
                ircNodeConnectTimer.Stop()
                _ircClient.Shutdown()
                _ircClient = Nothing
                ircDisplayList.Clear()
            End If
        End If
    End Sub
End Class