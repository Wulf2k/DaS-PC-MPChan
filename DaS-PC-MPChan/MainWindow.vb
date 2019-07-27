Imports System.Collections.Specialized
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
    Private WithEvents updateNetNodesTimer As New System.Windows.Forms.Timer()
    Private WithEvents updateOnlineStateTimer As New System.Windows.Forms.Timer()
    Private WithEvents netNodeConnectTimer As New System.Windows.Forms.Timer()
    Private WithEvents publishNodesTimer As New System.Windows.Forms.Timer()
    Private WithEvents checkWatchNodeTimer As New System.Windows.Forms.Timer()
    Private WithEvents dsAttachmentTimer As New System.Windows.Forms.Timer()
    Private WithEvents hotkeyTimer As New System.Windows.Forms.Timer()

    'For hotkey support
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Short

    'Hotkeys
    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean

    Public Version As String

    Private random As New Random()

    Private optionsLoaded As Boolean = False

    Private dsProcess As DarkSoulsProcess = Nothing
    Private _netClient As NetClient = Nothing
    Private netNodeDisplayList As New DSNodeBindingList()
    Private activeNodesDisplayList As New DSNodeBindingList()
    Private connectedNodes As New Dictionary(Of String, ConnectedNode)
    Private watchSteamId As String = Nothing
    Private watchExchangedLastTime As Date = DateTime.UTCNow

    Private manualConnections As New HashSet(Of String)

    Private recentConnections As New Queue(Of Tuple(Of Date, String))

    Private Sub DSCM_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        lbxDebugLog.Load(Me)
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
        updateOnlineStateTimer.Interval = Config.OnlineCheckInterval
        updateOnlineStateTimer.Start()
        updateNetNodesTimer.Interval = Config.UpdateNetNodesInterval
        netNodeConnectTimer.Interval = Config.NetNodeConnectInterval
        publishNodesTimer.Interval = Config.PublishNodesInterval
        checkWatchNodeTimer.Interval = Config.CheckWatchNodeInterval

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

        'updatecheck()    'Neutered due to lack of planned features
        updateOnlineState()
    End Sub
    Private Sub setupGridViews()
        Dim AlternateRowColor = Color.FromArgb(&HFFE3E3E3)

        With dgvMPNodes
            .AutoGenerateColumns = False
            .DataSource = activeNodesDisplayList
            .Columns.Add("name", "Name")
            .Columns("name").MinimumWidth = 80
            .Columns("name").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
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
            .Columns("world").Width = 180
            .Columns("world").DataPropertyName = "WorldText"
            .Columns.Add("ping", "Ping")
            .Columns("ping").Width = 50
            .Columns("ping").DataPropertyName = "PingColumn"
            .Columns("ping").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            .Font = New Font("Consolas", 10)
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            .Sort(.Columns("soulLevel"), ListSortDirection.Ascending)
            .Sort(.Columns("mpArea"), ListSortDirection.Ascending)
            .Sort(.Columns("world"), ListSortDirection.Descending)
        End With

        With dgvFavoriteNodes
            .Columns.Add("name", "Name")
            .Columns("name").Width = 180
            .Columns("name").ValueType = GetType(String)
            .Columns.Add("steamId", "Steam ID")
            .Columns("steamId").Width = 145
            .Columns("steamId").ValueType = GetType(String)
            .Columns.Add("isOnline", "O")
            .Columns("isOnline").Width = 20
            .Columns("isOnline").ValueType = GetType(String)
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With

        With dgvRecentNodes
            .AutoGenerateColumns = False
            .Columns.Add("name", "Name")
            .Columns("name").Width = 180
            .Columns("name").ValueType = GetType(String)
            .Columns.Add("steamId", "Steam ID")
            .Columns("steamId").Width = 145
            .Columns("steamId").ValueType = GetType(String)
            .Columns.Add("orderId", "Order ID")
            .Columns("orderId").Visible = False
            .Columns("orderId").ValueType = GetType(Long)
            .Columns.Add("isOnline", "O")
            .Columns("isOnline").Width = 20
            .Columns("isOnline").ValueType = GetType(String)
            .Font = New Font("Consolas", 10)
            .AlternatingRowsDefaultCellStyle.BackColor = AlternateRowColor
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect
        End With

        With dgvDSCMNet
            .AutoGenerateColumns = False
            .DataSource = netNodeDisplayList
            .Columns.Add("name", "Name")
            .Columns("name").MinimumWidth = 80
            .Columns("name").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
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

        regval = key.GetValue("MaxNodes")
        If regval Is Nothing Then key.SetValue("MaxNodes", "20")


        chkExpand.Checked = (key.GetValue("ExpandDSCM") = "True")
        chkDSCMNet.Checked = (key.GetValue("JoinDSCM-Net") = "True")
        nmbMaxNodes.Value = key.GetValue("MaxNodes")

        optionsLoaded = True
    End Sub
    Private Sub updateOnlineState_Tick() Handles updateOnlineStateTimer.Tick
        updateOnlineState()
    End Sub
    Private Async Sub updateOnlineState()
        Try
            Dim steamIds = New HashSet(Of String)
            For Each Row In dgvRecentNodes.Rows
                steamIds.Add(Row.Cells("steamId").Value)
            Next
            For Each Row In dgvFavoriteNodes.Rows
                If steamIds.Count < 100 Then steamIds.Add(Row.Cells("steamId").Value)
            Next
            Dim converter As New Converter(Of String, String)(Function(num) Convert.ToInt64(num, 16).ToString())
            Dim idQuery = String.Join(",", Array.ConvertAll(steamIds.ToArray(), converter))
            Dim uri = Config.OnlineCheckUrl & "?ids=" & idQuery
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
        Catch ex As Exception
            'Fail silently since nobody wants to be bothered for the online check.
        End Try
    End Sub
    Private Async Sub updatecheck()
        Try
            Dim client As New Net.WebClient()
            Dim content As String = Await client.DownloadStringTaskAsync(Config.VersionCheckUrl)

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
    Private Sub connectToNetNode() Handles netNodeConnectTimer.Tick
        If _netClient Is Nothing OrElse dsProcess Is Nothing Then Return

        Dim inMenu = (dsProcess.SelfSteamId = "" OrElse
                dsProcess.SelfNode.CharacterName = "" OrElse
                dsProcess.SelfNode.PhantomType = -1)

        If inMenu AndAlso dsProcess.NodeCount >= Config.BadNodesThreshold
            Return
        End If

        If dsProcess.NodeCount < dsProcess.MaxNodes - Config.NodesReservedForSteam - 1 Then
            Dim candidate As DSNode = selectNetNodeForConnecting()
            If candidate IsNot Nothing Then
                connectToSteamId(candidate.SteamId)
            End If
        End If
    End Sub
    Private Function selectNetNodeForConnecting() As DSNode
        Dim blackSet As New HashSet(Of String)()
        blackSet.Add(dsProcess.SelfNode.SteamId)
        For Each c In recentConnections
            blackSet.Add(c.Item2)
        Next
        For Each n In dsProcess.ConnectedNodes.Values
            blackSet.Add(n.SteamId)
        Next

        Dim candidates As New List(Of DSNode)
        For Each node In _netClient.netNodes.Values
            If blackSet.Contains(node.SteamId) Then Continue For
            candidates.Add(node)
        Next

        If candidates.Count = 0 Then Return Nothing

        'Pick the first few nodes at random to improve the network structure
        If dsProcess.NodeCount < Config.BadNodesThreshold Then
            Dim idx = random.Next(candidates.Count)
            Return candidates.Item(idx)
        End If

        Dim self = dsProcess.SelfNode

        'These read out dsProcess memory, so don't calculate them for every node
        Dim anorLondoInvading = self.Covenant = Covenant.DarkmoonBlade AndAlso dsProcess.HasDarkmoonRingEquipped
        Dim forestInvading = self.Covenant = Covenant.ForestHunter AndAlso dsProcess.HasCatCovenantRingEquipped
        Dim sorted As IOrderedEnumerable(Of DSNode) = candidates _
            .OrderByDescending(Function(other) (other.World <> "-1--1")) _
            .ThenByDescending(Function(other) As Boolean
                                   'Special cross-world invasions
                                   If anorLondoInvading Then
                                       'TODO: Use others dark anor londo info once we have it
                                       If other.World = AnorLondoWorld Then
                                           Return self.canDarkmoonInvade(other)
                                       End If
                                   ElseIf forestInvading Then
                                       If DarkrootGardenZones.Contains(other.MPZone) Then
                                           If other.HasExtendedInfo And other.Covenant = Covenant.ForestHunter Then
                                               Return False
                                           Else
                                               Return self.canForestInvade(other)
                                           End If
                                       End If
                                   End If
                                   Return False
                               End Function) _
            .ThenByDescending(Function(other) (other.World = self.World))

        Dim pvpSorting = Function(s As IOrderedEnumerable(Of DSNode))
                             Return s.ThenByDescending(Function(other) self.canBeRedSignSummoned(other)) _
                                     .ThenByDescending(Function(other) other.canBeRedSignSummoned(self)) _
                                     .ThenByDescending(Function(other) other.canRedEyeInvade(self)) _
                                     .ThenByDescending(Function(other) self.canRedEyeInvade(other)) _
                                     .ThenByDescending(Function(other) other.canBeSummoned(self)) _
                                     .ThenByDescending(Function(other) self.canBeSummoned(other))
                         End Function

        Dim sameMpZone = Function(other) other.MPZone = self.MPZone

        If self.Covenant = Covenant.Darkwraith Then
            sorted = sorted.ThenByDescending(Function(other) self.canRedEyeInvade(other)) _
                .ThenByDescending(sameMpZone) _
                .ThenByDescending(Function(other) other.canBeRedSignSummoned(self))
        ElseIf self.Covenant = Covenant.DarkmoonBlade Then
            sorted = sorted.ThenByDescending(Function(other) self.canDarkmoonInvade(other)) _
                .ThenByDescending(Function(other)
                                      If other.HasExtendedInfo Then
                                          Return If(other.Indictments > 0, 1, -1)
                                      Else
                                          Return 0
                                      End If
                                  End Function) _
                .ThenByDescending(sameMpZone)
            sorted = pvpSorting(sorted)
        ElseIf self.Covenant = Covenant.ForestHunter Or self.Covenant = Covenant.ChaosServant Then
            'Nothing specific to be done here, assume general interest in PVP
            sorted = pvpSorting(sorted)
        ElseIf self.Covenant = Covenant.GravelordServant Or self.Covenant = Covenant.PathOfTheDragon Then
            sorted = sorted.ThenByDescending(Function(other) self.canBeSummoned(other)) _
                .ThenByDescending(sameMpZone)
            sorted = pvpSorting(sorted)
        ElseIf self.Covenant = Covenant.WarriorOfSunlight Then
            sorted = sorted.ThenByDescending(Function(other) self.canBeSummoned(other)) _
                .ThenByDescending(Function(other) other.canBeSummoned(self))
        Else
            'No Covenant, Way of White, Princess Guard
            sorted = sorted.ThenByDescending(Function(other) self.canBeSummoned(other)) _
                .ThenByDescending(Function(other) other.canBeSummoned(self))
        End If

        sorted = sorted.ThenByDescending(sameMpZone) _
            .ThenBy(Function(other) Math.Abs(other.SoulLevel - self.SoulLevel))
        Return sorted(0)
    End Function
    Private Function nodeRanking(other As DSNode) As Integer
        '0 = good, 1 = half-bad, 2 = bad
        'Half-Bad = I can't interact with them, but they can invade me
        Dim self = dsProcess.SelfNode
        If (self.Covenant = Covenant.DarkmoonBlade AndAlso other.World = AnorLondoWorld AndAlso
            self.canDarkmoonInvade(other) AndAlso dsProcess.HasDarkmoonRingEquipped) Then Return 0
        If (self.Covenant = Covenant.ForestHunter AndAlso DarkrootGardenZones.Contains(other.MPZone) AndAlso
            self.canForestInvade(other) AndAlso dsProcess.HasCatCovenantRingEquipped) Then Return 0

        If self.World = other.World Then
            Dim coopPossible = (self.canBeSummoned(other) OrElse other.canBeSummoned(self))
            If coopPossible Then Return 0
            If self.Covenant = Covenant.Darkwraith And self.canRedEyeInvade(other) Then Return 0
            If self.Covenant = Covenant.DarkmoonBlade And self.canDarkmoonInvade(other) Then Return 0

            If self.Indictments > 0 And other.canDarkmoonInvade(self) Then Return 1
            If other.canRedEyeInvade(self) Then Return 1
        End If

        'TODO: check whether Sif is alive
        'If we knew that the other player is a Forest Hunter, we could mark this as a good node
        If (self.Covenant <> Covenant.ForestHunter AndAlso DarkrootGardenZones.Contains(self.MPZone) AndAlso
            other.canForestInvade(self)) Then Return 1
        'TODO: Add Dark Anor Londo check once we read out anor londo darkness
        Return 2
    End Function
    Private Sub handleDisconnects()
        If _netClient Is Nothing Or dsProcess Is Nothing Then Return
        If dsProcess.SelfNode.PhantomType = -1 Then Return

        Dim now As Date = Date.UtcNow
        Dim disconnectCandidates As New List(Of Tuple(Of ConnectedNode, Integer))()
        Dim badNodeCount = 0
        For Each connectedNode In connectedNodes.Values
            If Not IsNothing(watchSteamId) AndAlso connectedNode.node.SteamId = watchSteamId Then
                Continue For
            End If
            Dim ranking = nodeRanking(connectedNode.node)
            If ranking = 0 Then
                connectedNode.lastGoodTime = now
            Else
                badNodeCount += 1
                Dim badSeconds = (now - connectedNode.lastGoodTime).TotalSeconds
                If (manualConnections.Contains(connectedNode.node.SteamId) And badSeconds < Config.ManualNodeGracePeriod) Then
                    Continue For
                ElseIf ranking = 1 And badSeconds < Config.HalfBadNodeGracePeriod Then
                    Continue For
                ElseIf ranking = 2 and badSeconds < Config.BadNodeGracePeriod Then
                    Continue For
                End If
                'We might currently have an online interaction
                If (dsProcess.SelfNode.PhantomType = PhantomType.Coop Or dsProcess.SelfNode.PhantomType = PhantomType.Invader Or
                    connectedNode.node.PhantomType = PhantomType.Coop Or connectedNode.node.PhantomType = PhantomType.Invader) Then
                    Continue For
                End If
                disconnectCandidates.Add(Tuple.Create(connectedNode, ranking))
            End If
        Next

        Dim disconnectCount = Math.Min(badNodeCount - Config.BadNodesThreshold, disconnectCandidates.Count)
        disconnectCount = Math.Min(DisconnectTargetFreeNodes - (nmbMaxNodes.Value - dsProcess.NodeCount), disconnectCount)

        If disconnectCount < 1 Then
            Return
        End If
        Dim disconnectNodes = disconnectCandidates _
                .OrderByDescending(Function(x) x.Item2) _
                .Take(disconnectCount)
        For Each disconnectNode In disconnectNodes
            dsProcess.DisconnectSteamId(disconnectNode.Item1.node.SteamId)
        Next
    End Sub
    Private Sub updateUI() Handles updateUITimer.Tick
        If dsProcess Is Nothing Then
            btnLaunchDS.Visible = true
        Else
            'Node display
            'Changes the comparison instruction to display it if value is 0, rather than changing the value itself
            chkDebugDrawing.Checked = dsProcess.DrawNodes

            btnLaunchDS.Visible = False

            Dim maxNodes = dsProcess.MaxNodes
            ' Reading the value messes with input
            If Not nmbMaxNodes.Focused Then
                If maxNodes <> nmbMaxNodes.Value And maxNodes >= nmbMaxNodes.Minimum And maxNodes <= nmbMaxNodes.Maximum Then
                    dsProcess.MaxNodes = nmbMaxNodes.Value
                    'Read again, in case something else is force-setting it
                    maxNodes = dsProcess.MaxNodes
                End If
            End If
            If maxNodes >= nmbMaxNodes.Minimum And maxNodes <= nmbMaxNodes.Maximum Then
                If Not nmbMaxNodes.Focused Then
                    nmbMaxNodes.Value = maxNodes
                End If
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

            'errorCheckSteamName()
            txtLocalSteamName.Text = dsProcess.SelfSteamName
            If txtLocalSteamName.Text.length > 15 Then 
                txtLocalSteamName.BackColor = Color.OrangeRed
            Else
                txtLocalSteamName.BackColor = DefaultBackColor
            End If



            txtWatchdogActive.Text = dsProcess.HasWatchdog
            txtSin.Text = dsProcess.Sin
            txtDeaths.Text = dsProcess.Deaths
            txtPhantomType.Text = dsProcess.PhantomType
            txtTeamType.Text = dsProcess.TeamType
            txtClearCount.Text = dsProcess.ClearCount
            txtTimePlayed.Text = TimeSpan.FromMilliseconds(dsProcess.TimePlayed).ToString("ddd\.hh\:mm\:ss")

            txtRedCooldown.Text = math.Round(dsProcess.redCooldown, 0)
            txtBlueCooldown.Text = math.Round(dsProcess.blueCooldown, 0)


            txtXPos.Text = Math.Round(dsProcess.xPos, 1)
            txtYPos.Text = Math.Round(dsProcess.yPos, 1)
            txtZPos.Text = Math.Round(dsProcess.zPos, 1)

            Dim flaglist As New NameValueCollection

            flaglist.Add("Gaping Dragon", dsProcess.FlagsGapingDragonDead)
            flaglist.Add("Bell Gargoyles", dsProcess.FlagsBellGargoylesDead)
            flaglist.Add("Priscilla", dsProcess.FlagsPriscillaDead)
            flaglist.Add("Sif", dsProcess.FlagsSifDead)
            flaglist.Add("Pinwheel", dsProcess.FlagsPinwheelDead)
            flaglist.Add("Nito", dsProcess.FlagsNitoDead)
            flaglist.Add("Chaos Witch Quelaag", dsProcess.FlagsQuelaagDead)
            flaglist.Add("Bed of Chaos", dsProcess.FlagsBedOfChaosDead)
            flaglist.Add("Iron Golem", dsProcess.FlagsIronGolemDead)
            flaglist.Add("Ornstein & Smough", dsProcess.FlagsOnSDead)
            flaglist.Add("Four Kings", dsProcess.FlagsFourKingsDead)
            flaglist.Add("Seath", dsProcess.FlagsSeathDead)
            flaglist.Add("Gwyn", dsProcess.FlagsGwynDead)
            flaglist.Add("Taurus Demon", dsProcess.FlagsTaurusDead)
            flaglist.Add("Capra Demon", dsProcess.FlagsCapraDead)
            flaglist.Add("Moonlight Butterfly", dsProcess.FlagsMoonlightButterflyDead)
            flaglist.Add("Sanctuary Guardian", dsProcess.FlagsSanctuaryGuardianDead)
            flaglist.Add("Artorias", dsProcess.FlagsArtoriasDead)
            flaglist.Add("Manus", dsProcess.FlagsManusDead)
            flaglist.Add("Kalameet", dsProcess.FlagsKalameetDead)
            flaglist.Add("Demon Firesage", dsProcess.FlagsDemonFiresageDead)
            flaglist.Add("Ceaseless Discharge", dsProcess.FlagsCeaselessDischargeDead)
            flaglist.Add("Centipede Demon", dsProcess.FlagsCentipedeDemonDead)
            flaglist.Add("Gwyndolin", dsProcess.FlagsGwyndolinDead)
            flaglist.Add("Dark Anor Londo", dsProcess.FlagsDarkAnorLondo)
            flaglist.Add("New Londo Drained", dsProcess.FlagsNewLondoDrained)

            For Each item in flaglist.keys
                Try
                    clbEventFlags.SetItemChecked(clbEventFlags.Items.IndexOf(item), flaglist.GetValues(item)(0))
                Catch ex As Exception
                    MsgBox("Failed flag lookup - " & ex.Message)
                End Try
            Next

            UpdateDebugLog()
        End If


        If Not tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")" Then
            tabDSCMNet.Text = "DSCM-Net (" & dgvDSCMNet.Rows.Count & ")"
        End If
    End Sub
    Private Sub UpdateDebugLog()
        If Not IsNothing(dsProcess) Then
            lbxDebugLog.UpdateFromDS(dsProcess.debugLog)
        End If
    End Sub
    Private Sub chkLoggerEnabled_CheckedChanged( sender As Object,  e As EventArgs) Handles chkLoggerEnabled.CheckedChanged
        If Not IsNothing(dsProcess) Then dsProcess.enableDebugLog = chkLoggerEnabled.Checked
    End Sub
    Private Async Sub checkWatchNode() Handles checkWatchNodeTimer.Tick
        If _netClient Is Nothing Or dsProcess Is Nothing Then Return

        Dim connectedToOldNode = Not IsNothing(watchSteamId) AndAlso connectedNodes.ContainsKey(watchSteamId)

        If connectedToOldNode And (DateTime.UtcNow - watchExchangedLastTime).TotalMilliseconds <= Config.ExchangeWatchNodeInterval
            Return
        End If

        If dsProcess.NodeCount - connectedToOldNode < dsProcess.MaxNodes - Config.NodesReservedForSteam Then
            If Not IsNothing(watchSteamId) Then
                dsProcess.DisconnectSteamId(watchSteamId)
            End If

            Try
                Dim newWatchId = Await _netClient.getWatchId()
                watchSteamId = newWatchId
                watchExchangedLastTime = DateTime.UtcNow
                dsProcess.ConnectToSteamId(newWatchId)
            Catch ex As Exception
                txtIRCDebug.Text = "Failed to get new watch: " & ex.Message
            End Try
        End If
    End Sub
    Private Async Sub updateNetNodes() Handles updateNetNodesTimer.Tick
        If _netClient IsNot Nothing Then
            Await _netClient.loadNodes()
            netNodeDisplayList.SyncWithDict(_netClient.netNodes, dgvDSCMNet)
        End If
    End Sub
    Private Async Sub publishNodes() Handles publishNodesTimer.Tick
        If _netClient IsNot Nothing AndAlso dsProcess IsNot Nothing AndAlso dsProcess.SelfNode.SteamId IsNot Nothing Then
            Await _netClient.publishLocalNodes(dsProcess.SelfNode, dsProcess.ConnectedNodes.Values(), dsProcess.ReadLobbyList())
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
                dsProcess.enableDebugLog = chkLoggerEnabled.Checked
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

    Private Sub errorCheckSteamName()
        'Disabled temporarily due to being non-functional
        Dim byt() As Byte
        byt = Encoding.Unicode.GetBytes(dsProcess.SelfSteamName)
        
        If byt.Length > &H1d Then ReDim Preserve byt(&H1d)
        
        Dim tmpStr As String
        tmpStr = Encoding.Unicode.GetString(byt)
        tmpStr = tmpStr.Replace("#", "")
        
        If byt(0) = 0 Then tmpStr = "Invalid Name"

        dsProcess.SelfSteamName = tmpStr
    End Sub

    Private Sub updateActiveNodes() Handles updateActiveNodesTimer.Tick
        Dim selfNode As DSNode = Nothing
        If dsProcess IsNot Nothing Then
            dsProcess.UpdateNodes()
            If dsProcess.SelfNode.SteamId Is Nothing Then Return
            For Each kv In dsProcess.ConnectedNodes
                If connectedNodes.ContainsKey(kv.Key) Then
                    connectedNodes(kv.Key).node = kv.Value.Clone()
                Else
                    connectedNodes(kv.Key) = New ConnectedNode(kv.Value.Clone())
                End If
            Next
            For Each steamId In connectedNodes.Keys.ToList()
                If Not dsProcess.ConnectedNodes.ContainsKey(steamId) Then
                    connectedNodes.Remove(steamId)
                End If
            Next
            selfNode = dsProcess.SelfNode.Clone()
        Else
            connectedNodes.Clear()

        End If

        Dim activeNodes = connectedNodes.ToDictionary(Function(kv) kv.Key, Function(kv) kv.Value.node)
        If selfNode IsNot Nothing Then
            activeNodes.Add(selfNode.SteamId, selfNode)
        End If
        activeNodesDisplayList.SyncWithDict(activeNodes)

        'Color Rows according to ranking
        For Each row As DataGridViewRow In dgvMPNodes.Rows
            Dim steamId = row.Cells("steamId").Value
            row.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(0, 0, 0)
            If steamId = selfNode.SteamId Then
                row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(198, 239, 206)
            ElseIf steamId = watchSteamId Then
                row.DefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100)
                row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(200, 200, 200)
            Else
                Dim ranking = nodeRanking(activeNodes(steamId))
                If ranking = 2 Then
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 199, 206)
                ElseIf ranking = 1 Then
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 235, 156)
                Else
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 255, 255)
                End If
            End If
            If steamId = selfNode.SteamId Or manualConnections.Contains(steamId) Then
                row.DefaultCellStyle.Font = New Font(dgvMPNodes.DefaultCellStyle.Font, FontStyle.Bold)
            Else
                row.DefaultCellStyle.Font = Nothing
            End If
        Next

        updateRecentNodes()
        'Do this now as our node info as recent as possible
        handleDisconnects()
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
        If optionsLoaded Then
            Dim key = My.Computer.Registry.CurrentUser.OpenSubKey("Software\DSCM\Options", True)
            key.SetValue("MaxNodes", nmbMaxNodes.Value)
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
            While (now - recentConnections.Peek().Item1).TotalSeconds > Config.ConnectionRetryTimeout
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
        manualConnections.Add(idString)
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
        Dim steamId = sender.CurrentRow.Cells("steamId").Value
        manualConnections.Add(steamId)
        connectToSteamId(steamId)
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
            _netClient = New NetClient(Me)
            netNodeConnectTimer.Start()
            updateNetNodesTimer.Start()
            publishNodesTimer.Start()
            checkWatchNodeTimer.Start()
            updateNetNodes()
        Else
            If _netClient IsNot Nothing Then
                updateNetNodesTimer.Stop()
                netNodeConnectTimer.Stop()
                publishNodesTimer.Stop()
                checkWatchNodeTimer.Stop()
                _netClient = Nothing
            End If
        End If
    End Sub

    Private Sub dgvNodes_doubleclick(sender As Object, e As EventArgs) Handles dgvRecentNodes.DoubleClick, dgvFavoriteNodes.DoubleClick, dgvDSCMNet.DoubleClick

    End Sub

    Private Sub btnLaunchDS_Click(sender As Object, e As EventArgs) Handles btnLaunchDS.Click
        Try
            Dim proc As New System.Diagnostics.Process()
            proc = Process.Start("steam://rungameid/211420", "")
        Catch ex As Exception
            MsgBox("Error launching." & Environment.NewLine & ex.Message)
        End Try
    End Sub
End Class


Class DebugLogForm
Inherits ListBox
    Private mw As MainWindow
    Private LobbyRegex As New Regex("^SteamMatchmaking|^Property added:|^LobbyData")
    Public Sub Load(mainWindow As MainWindow)
        mw = mainWindow
        AddHandler mw.chkLogDBG.CheckedChanged, AddressOf FilterEntries
        AddHandler mw.chkLogLobby.CheckedChanged, AddressOf FilterEntries
    End Sub
    Private ReadOnly Property ShowDbgEntries As Boolean
        Get
            Return mw.chkLogDBG.Checked
        End Get
    End Property
    Private ReadOnly Property ShowLobbyEntries As Boolean
        Get
            Return mw.chkLogLobby.Checked
        End Get
    End Property
    Private Function EntryAllowed(entry As DebugLogEntry) As Boolean
        If Not ShowDbgEntries AndAlso entry.severity = "DBG" Then Return False
        If Not ShowLobbyEntries AndAlso entry.severity = "DBG" AndAlso LobbyRegex.IsMatch(entry.msg) Then Return False
        Return True
    End Function
    Public Sub FilterEntries()
        Me.BeginUpdate()
        For i = Items.Count - 1 To 0 Step -1
            If Not EntryAllowed(Items(i)) Then
                Items.RemoveAt(i)
            End If
        Next
        Me.EndUpdate()
    End Sub
    Public Sub UpdateFromDS(dsDebugLog As List(Of DebugLogEntry))
        SyncLock dsDebugLog
            If dsDebugLog.Count
                Me.BeginUpdate()
                Dim itemsVisible As Integer = (Me.Height \ Me.ItemHeight)
                Dim scrollToBottom = Me.TopIndex > Items.Count - itemsVisible - 1

                For Each entry In dsDebugLog
                    entry.msg = LogTranslations.TranslateMessage(entry.msg)
                    If EntryAllowed(entry) Then Items.Add(entry)
                Next
                dsDebugLog.Clear()

                While Items.Count > Config.DebugLogLength
                    Items.RemoveAt(0)
                End While
                
                Me.EndUpdate()
                If scrollToBottom Then Me.TopIndex = Me.Items.Count - 1
            End If
        End SyncLock
    End Sub
    Public Sub InitContextMenu() Handles Me.ContextMenuStripChanged
        If IsNothing(ContextMenuStrip) Then Return
        AddHandler ContextMenuStrip.ItemClicked, AddressOf ContextMenuItemClicked
    End Sub
    Private Sub ContextMenuItemClicked(sender As Object, e As ToolStripItemClickedEventArgs)
        If e.ClickedItem.Name = "copy" Then
            Dim text = String.Join(vbCrLf, SelectedItems.Cast(Of DebugLogEntry).Select(Function(x) x.ToString()).ToArray())
            Clipboard.SetData(DataFormats.UnicodeText, text)
        ElseIf e.ClickedItem.Name = "selectAll" Then
            BeginUpdate()
            For i = 0 To Items.Count - 1
                Me.SetSelected(i, True)
            Next
            EndUpdate()
        End If
    End Sub
End Class

Public Class DebugLogEntry
    Public severity As String
    Public msg As String
    Public ts As Date

    Sub New(ts, severity, msg)
        Me.ts = ts
        Me.severity = severity
        Me.msg = msg
    End Sub

    Public Overrides Function ToString() As String
        Return ts.ToString("hh:mm:ss.fff") & " " & severity & ": " & msg
    End Function
End Class

Class ConnectedNode
    Public node As DSNode
    Public lastGoodTime As Date

    Sub New(node As DSNode)
        Me.node = node
        Me.lastGoodTime = DateTime.UtcNow
    End Sub
End Class