Imports System.Threading
Imports System.IO
Imports System.Text.RegularExpressions

Public Class DSCM
    'Timers
    Private WithEvents refMpData As New System.Windows.Forms.Timer()
    Private WithEvents refTimer As New System.Windows.Forms.Timer()
    Private WithEvents onlineTimer As New System.Windows.Forms.Timer()
    Private WithEvents hotkeyTimer As New System.Windows.Forms.Timer()

    'For hotkey support
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Short

    'Process/Memory Manipulation
    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByVal lpNumberOfBytesWritten As Integer) As Boolean
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    Private Declare Function VirtualAllocEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As IntPtr, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As IntPtr
    Private Declare Function VirtualProtectEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal lpSize As IntPtr, ByVal dwNewProtect As UInt32, ByRef dwOldProtect As UInt32) As Boolean
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Integer, ByVal lpThreadAttributes As Integer, ByVal dwStackSize As Integer, ByVal lpStartAddress As Integer, ByVal lpParameter As Integer, ByVal dwCreationFlags As Integer, ByRef lpThreadId As Integer) As Integer

    Public Const PROCESS_VM_READ = &H10
    Public Const TH32CS_SNAPPROCESS = &H2
    Public Const MEM_COMMIT = 4096
    Public Const PAGE_READWRITE = 4
    Public Const PAGE_EXECUTE_READWRITE = &H40
    Public Const PROCESS_CREATE_THREAD = &H2
    Public Const PROCESS_VM_OPERATION = &H8
    Public Const PROCESS_VM_WRITE = &H20
    Public Const PROCESS_ALL_ACCESS = &H1F0FFF

    Private _oldPageProtection As Integer = 0
    Private _targetProcess As Process = Nothing
    Private _targetProcessHandle As IntPtr = IntPtr.Zero

    'Thread to check for updates
    Private updTrd As Thread

    'Hotkeys
    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean

    'Check for Dark Souls beta EXE, fail if true
    Dim beta As Boolean

    'Check for PVP Watchdog
    Dim watchdog As Boolean

    'Addresses of the various inserted functions
    Dim namedNodePtr As Integer = 0
    Dim nodeDumpPtr As Integer = 0
    Dim forceIdPtr As Integer = 0
    Dim attemptIdPtr As Integer = 0

    Dim dsPWPtr As IntPtr
    Dim dsPWBase As Integer

    Dim dsBasePtr As IntPtr
    Dim dsBase As Integer

    'For locating the Steam matchmaking functions
    Dim steamApiBasePtr As IntPtr
    Dim steamApiBase As Integer = 0

    Dim steamApiDllModule As ProcessModule

    'New version of DSCM available?
    Dim newver As Boolean = False

    Public Function TryAttachToProcess(ByVal windowCaption As String) As Boolean
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            If pp.ProcessName.ToLower.Equals(windowCaption.ToLower) Then
                Return TryAttachToProcess(pp)
            End If
        Next
        MessageBox.Show("Unable to find process '" & windowCaption & "'." & vbCrLf & " Is it running?")
        Return False
    End Function
    Public Function TryAttachToProcess(ByVal proc As Process) As Boolean
        If _targetProcessHandle = IntPtr.Zero Then 'not already attached
            _targetProcess = proc
            _targetProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, False, _targetProcess.Id)
            If _targetProcessHandle = 0 Then
                TryAttachToProcess = False
                steamApiBase = 0
                MessageBox.Show("OpenProcess() FAIL! Are you Administrator??")
            Else
                TryAttachToProcess = True

                'Map various base addresses
                For Each dll In _targetProcess.Modules

                    Select Case dll.modulename.tolower

                        Case "darksouls.exe"
                            'Note to self, extra variables due to issues with conversion.  Fix "some day".
                            dsBasePtr = dll.BaseAddress
                            dsBase = dsBasePtr

                        Case "d3d9.dll"
                            If watchdog = False Then
                                dsPWPtr = dll.baseaddress
                                dsPWBase = dsPWPtr

                                If ReadUInt8(dsPWBase + &H6E41) = &HE8& Then
                                    watchdog = True
                                    WriteBytes(dsPWBase + &H6E41, {&H90, &H90, &H90, &H90, &H90})
                                End If
                            End If


                        'Find steam_api.dll for ability to directly add SteamIDs as nodes
                        Case "steam_api.dll"
                            steamApiBasePtr = dll.baseaddress
                            steamApiBase = steamApiBasePtr
                            steamApiDllModule = dll

                    End Select
                Next
            End If
        Else
            MessageBox.Show("Already attached! (Please Detach first?)")
            TryAttachToProcess = False
        End If
    End Function
    Public Sub DetachFromProcess()
        If Not (_targetProcessHandle = IntPtr.Zero) Then
            _targetProcess = Nothing
            Try
                CloseHandle(_targetProcessHandle)
                _targetProcessHandle = IntPtr.Zero
            Catch ex As Exception
            End Try
        End If
    End Sub

    Private Sub DSCM_Close(sender As Object, e As EventArgs) Handles MyBase.FormClosed
        chkDebugDrawing.Checked = False
        chkExpand.Checked = False
        If watchdog Then
            WriteBytes(dsPWBase + &H6E41, {&HE8, &H8E, &HD5, &HFF, &HFF})
        End If
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
        refTimer.Enabled = True

        TryAttachToProcess("darksouls")
        beta = (ReadUInt32(dsBase + &H80) = &HE91B11E2&)
        If beta Then
            MsgBox("Beta version detected.  Disconnecting from process.")
            DetachFromProcess()
        End If

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
            Dim ver = File.ReadAllLines(Path.GetTempPath & "\dscm-ver.txt")(0)

            newver = (ver > lblVer.Text.Replace(".", ""))
        Catch ex As Exception
            'Fail silently since nobody wants to be bothered for an update check.
        End Try
    End Sub
    Private Sub refTimer_Tick() Handles refTimer.Tick
        Dim dbgboost As Integer = 0
        Dim tmpptr As Integer = 0

        'Text indicating new version is hidden if DSCM is expanded, only care if it's seen at the start anyway.
        lblNewVersion.Visible = (newver And Not chkExpand.Checked)

        'Node display
        'Changes the comparison instruction to display it if value is 0, rather than changing the value itself
        chkDebugDrawing.Checked = (ReadBytes(dsBase + &HBA256C, 1)(0) = 1)

        tmpptr = ReadInt32(dsBase + &HF7F834)
        tmpptr = ReadInt32(tmpptr + &H38)
        If Not tmpptr = 0 And Not beta Then
            Dim maxnodes = ReadInt32(tmpptr + &H70)
            If maxnodes >= nmbMaxNodes.Minimum And maxnodes <= nmbMaxNodes.Maximum Then
                nmbMaxNodes.Value = maxnodes
            End If
        End If

        'Don't update the text box if it's clicked in, so people can copy/paste without losing cursor.
        'Probably don't need to update this more than once anyway, but why not?
        If Not txtSelfSteamID.Focused Then
            txtSelfSteamID.Text = ReadAsciiStr(ReadInt32(dsBase + &HF7E204) + &HA00)
        End If

        tmpptr = ReadInt32(dsBase + &HF7E204)
        If Not tmpptr = 0 And Not beta Then
            txtCurrNodes.Text = ReadInt32(tmpptr + &HAE0)

            'Find self in DGV by SteamID, then update MP Zone.
            For i = 0 To dgvMPNodes.Rows.Count - 1
                If dgvMPNodes.Rows(i).Cells(1).Value = txtSelfSteamID.Text Then
                    dgvMPNodes.Rows(i).Cells(4).Value = ReadInt32(tmpptr + &HA14)
                End If
            Next
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
        'Forget about all previously allocated memory for functions on reconnect, allocate fresh if called again.
        'Yeah, it's a memory leak, so don't do this hundreds of thousands of times.
        namedNodePtr = 0
        forceIdPtr = 0
        nodeDumpPtr = 0
        attemptIdPtr = 0

        If watchdog Then
            WriteBytes(dsPWBase + &H6E41, {&HE8, &H8E, &HD5, &HFF, &HFF})
        End If

        watchdog = False
        beta = False

        DetachFromProcess()
        TryAttachToProcess("darksouls")

        'Note to self, push beta & debug check out to its own sub.
        beta = (ReadUInt32(dsBase + &H80) = &HE91B11E2&)
        If beta Then
            MsgBox("Beta version detected.  Disconnecting from process.")
            DetachFromProcess()
        End If
        chkExpand.Checked = False
    End Sub

    Private Sub chkDebugDrawing_CheckedChanged(sender As Object, e As EventArgs) Handles chkDebugDrawing.CheckedChanged
        Dim cmpLoc As Integer = dsBase + &HBA256C
        Dim TargetBufferSize = 1024

        Dim bytes() As Byte
        Dim bytes2() As Byte

        'Location in bytearray to insert jump location
        Dim bytjmp As Integer = &H6B
        Dim hookLoc As Integer = dsBase + &H15A550


        If chkDebugDrawing.Checked Then
            'Changes instruction doing the compare rather than changing the value it compares against
            WriteBytes(cmpLoc, {&H1})


            'If memory has not previously been allocated then allocate, otherwise use previous allocation
            'Memory leaks still exists if somebody were to repeatedly reattach to the process, so...  don't do that.
            If namedNodePtr = 0 Then
                namedNodePtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
                VirtualProtectEx(_targetProcessHandle, namedNodePtr, TargetBufferSize, PAGE_EXECUTE_READWRITE, _oldPageProtection)
            End If

            'ASM in Resources\ASM-NamedNodes.txt
            bytes = {&H8B, &H44, &H24, &H10, &H50, &H8B, &HC3, &H8B, &HD9, &H81, &HE3, &H0, &HFB, &H0, &H0, &H81,
                        &HFB, &H0, &HFB, &H0, &H0, &H8B, &HD8, &HF, &H84, &H5, &H0, &H0, &H0, &HE9, &H46, &H0,
                        &H0, &H0, &H8B, &H5B, &HD0, &H83, &HFB, &H0, &HF, &H84, &H14, &H0, &H0, &H0, &H8B, &H5B,
                        &H14, &H83, &HFB, &H0, &HF, &H84, &H8, &H0, &H0, &H0, &H83, &HC3, &H30, &HE9, &H7, &H0,
                        &H0, &H0, &H8B, &HD8, &HE9, &H1F, &H0, &H0, &H0, &H50, &HB8, &H0, &H0, &H0, &H0, &H83,
                        &HF8, &H20, &HF, &H84, &H9, &H0, &H0, &H0, &H8A, &H13, &H88, &H17, &H40, &H43, &H47, &HEB,
                        &HEE, &H83, &HEB, &H20, &H83, &HEF, &H20, &H58, &H58, &H56, &HE9, &H0, &H0, &H0, &H0}

            'Modify final JMP above to return to instruction after original hook
            bytes2 = BitConverter.GetBytes((hookLoc - &H6A) - namedNodePtr)
            Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, namedNodePtr, bytes, TargetBufferSize, 0)

            If ReadUInt8(namedNodePtr) = &H8B& Then
                'Insert hook to jump to allocated memory above
                bytes = {&HE9, 0, 0, 0, 0}
                bytes2 = BitConverter.GetBytes((namedNodePtr - hookLoc - 5))
                Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
            Else
                MsgBox("Code insertion appears to have failed.  Try again.")
                namedNodePtr = 0
                chkDebugDrawing.Checked = False
            End If
        Else
            'Remove Named Node hook, restore original instruction
            bytes = {&H8B, &H44, &H24, &H10, &H56}
            WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)

            'Disable Node Drawing
            WriteBytes(cmpLoc, {&H0})
        End If
    End Sub

    Private Sub refMpData_Tick() Handles refMpData.Tick
        Dim nodeCount As Integer

        Dim rowName As String = ""
        Dim rowSteamID As String = ""
        Dim rowSL As Integer = 0
        Dim rowPhantomType As String = ""
        Dim rowMPArea As Integer = 0
        Dim rowWorld As String = ""

        Dim SteamNodesPtr As Integer
        Dim SteamNodeList As Integer
        Dim SteamData1 As Integer
        Dim SteamData2 As Integer

        'Note to self, update to relative addresses
        nodeCount = ReadInt32(dsBase + &HF62DD0)
        SteamNodeList = ReadInt32(dsBase + &HF62DCC)
        SteamNodesPtr = ReadInt32(SteamNodeList)

        'Erase world value for all entries, if world value not updated later entry will be deleted.
        For i = 0 To dgvMPNodes.Rows.Count - 1
            dgvMPNodes.Rows(i).Cells(5).Value = ""
        Next

        For i = 0 To nodeCount - 1
            SteamData1 = ReadInt32(SteamNodesPtr + &HC)
            SteamData2 = ReadInt32(SteamData1 + &HC)
            rowName = ReadUnicodeStr(SteamData1 + &H30)
            rowSteamID = ReadUnicodeStr(SteamData2 + &H30)

            SteamNodesPtr = ReadInt32(SteamNodesPtr)

            Dim notexist As Boolean = True
            For j = 0 To dgvMPNodes.Rows.Count - 1
                If rowSteamID = dgvMPNodes.Rows(j).Cells(1).Value Then notexist = False
            Next
            If notexist Then dgvMPNodes.Rows.Add({rowName, rowSteamID, rowSL, rowPhantomType, rowMPArea, rowWorld})
        Next

        Dim tmpptr As Integer = ReadInt32(dsBase + &HF7E204)
        For i = 0 To dgvMPNodes.Rows.Count - 1
            If dgvMPNodes.Rows(i).Cells(1).Value = txtSelfSteamID.Text Then
                dgvMPNodes.Rows(i).Cells(2).Value = ReadInt32(tmpptr + &HA30)
                dgvMPNodes.Rows(i).Cells(3).Value = ReadInt32(tmpptr + &HA28).ToString
                dgvMPNodes.Rows(i).Cells(4).Value = ReadInt32(tmpptr + &HA14)
                dgvMPNodes.Rows(i).Cells(5).Value = (ReadInt8(tmpptr + &HA13) & "-" & ReadInt8(tmpptr + &HA12)).ToString
            End If
        Next

        Dim cont = True
        Dim tmpid As String

        tmpptr = nodeDumpPtr + &H200

        While cont
            tmpid = ReadAsciiStr(tmpptr)
            cont = Not (tmpid = Nothing)

            For i = 0 To dgvMPNodes.Rows.Count - 1
                If (dgvMPNodes.Rows(i).Cells(1).Value = tmpid) And cont Then

                    'SL
                    dgvMPNodes.Rows(i).Cells(2).Value = Convert.ToInt32(ReadInt16(tmpptr + &H26))

                    'Phantom
                    dgvMPNodes.Rows(i).Cells(3).Value = ReadInt8(tmpptr + &H24).ToString

                    'Mp Area ID
                    dgvMPNodes.Rows(i).Cells(4).Value = ReadInt32(tmpptr + &H28)

                    'World
                    tmpid = ReadInt8(tmpptr + &H13) & "-" & ReadInt8(tmpptr + &H12)
                    dgvMPNodes.Rows(i).Cells(5).Value = tmpid
                End If
                WriteInt32(tmpptr, 0)
            Next
            tmpptr += &H30
        End While

        For i = 0 To dgvMPNodes.Rows.Count - 1
            tmpid = dgvMPNodes.Rows(i).Cells(3).Value
            Select Case tmpid
                Case "0"
                    tmpid = "Human"
                Case "1"
                    tmpid = "Co-op"
                Case "2"
                    tmpid = "Invader"
                Case "8"
                    tmpid = "Hollow"
            End Select
            dgvMPNodes.Rows(i).Cells(3).Value = tmpid

            'Note to self, should probably convert this to a pretty form of lookup some day.
            tmpid = dgvMPNodes.Rows(i).Cells(5).Value
            Select Case tmpid
                Case "-1--1"
                    tmpid = "None"
                Case "10-0"
                    tmpid = "Depths"
                Case "10-1"
                    tmpid = "Undead Burg/Parish"
                Case "10-2"
                    tmpid = "Firelink Shrine"
                Case "11-0"
                    tmpid = "Painted World"
                Case "12-0"
                    tmpid = "Darkroot Garden"
                Case "12-1"
                    tmpid = "Oolacile"
                Case "13-0"
                    tmpid = "Catacombs"
                Case "13-1"
                    tmpid = "Tomb of the Giants"
                Case "13-2"
                    tmpid = "Ash Lake"
                Case "14-0"
                    tmpid = "Blighttown"
                Case "14-1"
                    tmpid = "Demon Ruins"
                Case "15-0"
                    tmpid = "Sen's Fortress"
                Case "15-1"
                    tmpid = "Anor Londo"
                Case "16-0"
                    tmpid = "New Londo Ruins"
                Case "17-0"
                    tmpid = "Duke's Archives / Caves"
                Case "18-0"
                    tmpid = "Kiln"
                Case "18-1"
                    tmpid = "Undead Asylum"
            End Select
            dgvMPNodes.Rows(i).Cells(5).Value = tmpid
        Next

        'Delete old nodes.
        For i = dgvMPNodes.Rows.Count - 1 To 0 Step -1
            If dgvMPNodes.Rows(i).Cells(5).Value = "" Then dgvMPNodes.Rows.Remove(dgvMPNodes.Rows(i))
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
            recentNodes = recentNodes.OrderBy(Function(row) row.Cells("orderId").Value).ToList()
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

        'Note to self, buffer is overly large.  Trim down some day.
        Dim TargetBufferSize = 4096

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H78
        Dim hookLoc As Integer = dsBase + &H7E637E

        If chkExpand.Checked Then

            If nodeDumpPtr = 0 Then
                nodeDumpPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
                VirtualProtectEx(_targetProcessHandle, nodeDumpPtr, TargetBufferSize, PAGE_EXECUTE_READWRITE, _oldPageProtection)
            End If

            'ASM in Resources\ASM-NodeDump.txt
            bytes = {&H50, &H53, &H51, &H52, &H56, &H57, &HBF, &H0, &H0, &H0, &H0, &HB8, &H0, &H0, &H0, &H0,
                        &HBB, &H0, &H0, &H0, &H0, &HB9, &H0, &H0, &H0, &H0, &HBA, &H0, &H0, &H0, &H0, &H8A,
                        &H1F, &H80, &HFB, &H0, &HF, &H84, &H30, &H0, &H0, &H0, &H8A, &H6, &H8A, &H1F, &H46, &H47,
                        &H41, &H38, &HD8, &HF, &H85, &H13, &H0, &H0, &H0, &H83, &HF9, &H11, &H75, &HEC, &H29, &HCE,
                        &H29, &HCF, &HB9, &H0, &H0, &H0, &H0, &HE9, &HE, &H0, &H0, &H0, &H29, &HCE, &H29, &HCF,
                        &HB9, &H0, &H0, &H0, &H0, &H83, &HC7, &H30, &HEB, &HC5, &H8A, &H1E, &H88, &H1F, &H46, &H47,
                        &H41, &H83, &HF9, &H30, &HF, &H84, &H2, &H0, &H0, &H0, &HEB, &HEE, &H5F, &H5E, &H5A, &H59,
                        &H5B, &H58, &H66, &HF, &HD6, &H46, &H14, &HE9, &H0, &H0, &H0, &H0}

            'Adjust EDI
            bytes2 = BitConverter.GetBytes(nodeDumpPtr + &H200)
            Array.Copy(bytes2, 0, bytes, &H7, bytes2.Length)

            'Adjust the jump home
            bytes2 = BitConverter.GetBytes((hookLoc - &H77) - nodeDumpPtr)
            Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, nodeDumpPtr, bytes, TargetBufferSize, 0)

            If ReadUInt8(nodeDumpPtr) = &H50& Then
                'Insert the hook
                bytes = {&HE9, 0, 0, 0, 0}
                bytes2 = BitConverter.GetBytes((nodeDumpPtr - hookLoc - 5))
                Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
                refMpData.Start()

                Me.Width = 800
                Me.Height = 680
                tabs.Visible = True
                lblNewVersion.Visible = False
                btnAddFavorite.Visible = True
                btnRemFavorite.Visible = True
            Else
                MsgBox("Code insertion appears to have failed.  Try again.")
                nodeDumpPtr = 0
                chkExpand.Checked = False
            End If
        Else
            'Restore original instruction
            bytes = {&H66, &HF, &HD6, &H46, &H14}
            WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
            refMpData.Stop()

            Me.Width = 450
            Me.Height = 190
            tabs.Visible = False
            btnAddFavorite.Visible = False
            btnRemFavorite.Visible = False
        End If
    End Sub
    Private Sub nmbMaxNodes_ValueChanged(sender As Object, e As EventArgs) Handles nmbMaxNodes.ValueChanged
        Dim tmpptr As Integer

        tmpptr = ReadInt32(dsBase + &HF7F834)
        tmpptr = ReadInt32(tmpptr + &H38)
        WriteInt32(tmpptr + &H70, nmbMaxNodes.Value)
    End Sub
    Private Sub txtTargetSteamID_LostFocus(sender As Object, e As EventArgs) Handles txtTargetSteamID.LostFocus
        'Auto-convert Steam ID after clicking out of the textbox

        Dim steamIdInt As Int64
        If txtTargetSteamID.Text.Length > 1 Then
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
        attemptConnSteamID(txtTargetSteamID.Text)
    End Sub
    Private Sub attemptConnSteamID(ByVal steamID As String)
        Try
            'Note to self, find a better way of confirming that this is the correct address for this function.
            'If nothing else, just compare the bytes at the address.
            Dim steamApiNetworking As Integer = steamApiBase + &H2F70

            'Extremely weak check to be sure we're at the right spot
            If Not ReadUInt8(steamApiNetworking) = &HA1& Then
                MsgBox("Unexpected byte at Steam_api.dll|Networking.  Game is likely to crash now." & Environment.NewLine &
                       Environment.NewLine & "Please report this error to wulfs.throwaway.address@gmail.com.")
            End If

            If steamApiBase = 0 Then
                MsgBox("Unable to locate necessary function in memory.  Aborting connection attempt.")
            Else
                Dim DataPacket1 As Integer

                Dim TargetBufferSize = 1024
                Dim bytes() As Byte
                Dim bytes2() As Byte

                'If 0 then allocate memory, otherwise use previously allocated memory.
                If attemptIdPtr = 0 Then
                    attemptIdPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
                    VirtualProtectEx(_targetProcessHandle, attemptIdPtr, TargetBufferSize, PAGE_EXECUTE_READWRITE, _oldPageProtection)
                End If
                DataPacket1 = attemptIdPtr + &H100

                'Note to self, as usual, comment in the actual ASM before it gets lost.
                bytes = {&HE8, &H0, &H0, &H0, &H0, &H8B, &H10, &H8B, &H12, &H6A, &H1, &H6A, &H2, &HBF, &H43, &H0,
                         &H0, &H0, &H57, &HB9, &H0, &H0, &H0, &H0, &H51, &H68, &H0, &H0, &H0, &H0, &H68, &H0,
                         &H0, &H0, &H0, &H8B, &HC8, &HFF, &HD2, &HC3}

                'Set steam_api.SteamNetworking call
                bytes2 = BitConverter.GetBytes(steamApiNetworking - attemptIdPtr - 5)
                Array.Copy(bytes2, 0, bytes, &H1, bytes2.Length)

                bytes2 = BitConverter.GetBytes(attemptIdPtr + &H100)
                Array.Copy(bytes2, 0, bytes, &H14, bytes2.Length)

                bytes2 = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Left(steamID, 8), 16))
                Array.Copy(bytes2, 0, bytes, &H1A, bytes2.Length)

                bytes2 = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Right(steamID, 8), 16))
                Array.Copy(bytes2, 0, bytes, &H1F, bytes2.Length)

                WriteProcessMemory(_targetProcessHandle, attemptIdPtr, bytes, bytes.Length, 0)


                'Set up data packet
                bytes = {&H1}
                WriteProcessMemory(_targetProcessHandle, DataPacket1, bytes, bytes.Length, 0)

                Dim selfSteamName As String
                selfSteamName = ReadUnicodeStr(ReadInt32(dsBase + &HF62DD4) + &H30)

                bytes = System.Text.Encoding.Unicode.GetBytes(selfSteamName)
                WriteProcessMemory(_targetProcessHandle, DataPacket1 + 1, bytes, bytes.Length, 0)

                CreateRemoteThread(_targetProcessHandle, 0, 0, attemptIdPtr, 0, 0, 0)
            End If
        Catch ex As Exception
            MsgBox("Well, that failed spectacularly.  Why?" & Environment.NewLine & "I dunno, I'm just an inanimate message box.  Ask a human about the following message: " &
                   Environment.NewLine & Environment.NewLine & ex.Message)
        End Try
    End Sub
    Private Sub dgvNodes_selected(sender As Object, e As EventArgs) Handles dgvFavoriteNodes.CellEnter, dgvRecentNodes.CellEnter
        If sender.SelectedCells.Count > 0 Then
            Dim rowindex As Integer = sender.SelectedCells(0).RowIndex
            Dim id As String = sender.Rows(rowindex).Cells(1).Value
            txtTargetSteamID.Text = id
        End If
    End Sub
    Private Sub dgvNodes_doubleclick(sender As Object, e As EventArgs) Handles dgvFavoriteNodes.DoubleClick, dgvRecentNodes.DoubleClick
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
        End Select
    End Sub
End Class
