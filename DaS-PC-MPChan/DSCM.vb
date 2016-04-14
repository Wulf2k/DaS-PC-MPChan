Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Net
Imports System.IO

Public Class DSCM
    Private WithEvents refMpData As New System.Windows.Forms.Timer()
    Private WithEvents refTimer As New System.Windows.Forms.Timer()
    Private WithEvents hotkeyTimer As New System.Windows.Forms.Timer()
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Short

    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByVal lpNumberOfBytesWritten As Integer) As Boolean
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    Private Declare Function VirtualAllocEx Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal lpAddress As IntPtr, ByVal dwSize As IntPtr, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As IntPtr
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Integer, ByVal lpThreadAttributes As Integer, ByVal dwStackSize As Integer, ByVal lpStartAddress As Integer, ByVal lpParameter As Integer, ByVal dwCreationFlags As Integer, ByRef lpThreadId As Integer) As Integer




    Public Const PROCESS_VM_READ = &H10
    Public Const TH32CS_SNAPPROCESS = &H2
    Public Const MEM_COMMIT = 4096
    Public Const PAGE_READWRITE = 4
    Public Const PROCESS_CREATE_THREAD = (&H2)
    Public Const PROCESS_VM_OPERATION = (&H8)
    Public Const PROCESS_VM_WRITE = (&H20)
    Public Const PROCESS_ALL_ACCESS = &H1F0FFF

    Private _targetProcess As Process = Nothing
    Private _targetProcessHandle As IntPtr = IntPtr.Zero

    Private updTrd As Thread

    'Hotkeys
    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean

    Dim beta As Boolean

    Dim namedNodePtr As Integer
    Dim nodeDumpPtr As Integer
    Dim forceIdPtr As Integer
    Dim attemptIdPtr As Integer

    Dim steamApiDllPtr As IntPtr = 0
    Dim steamApiDllModule As ProcessModule

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
                steamApiDllPtr = 0
                MessageBox.Show("OpenProcess() FAIL! Are you Administrator??")
            Else
                TryAttachToProcess = True

                'Find steam_api.dll for ability to directly add SteamIDs as nodes
                For Each dll In _targetProcess.Modules
                    If dll.modulename.tolower = "steam_api.dll" Then
                        steamApiDllPtr = dll.baseaddress
                        steamApiDllModule = dll
                    End If

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

    Public Function ReadInt8(ByVal addr As IntPtr) As SByte
        Dim _rtnBytes(0) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 1, vbNull)
        Return _rtnBytes(0)
    End Function
    Public Function ReadInt16(ByVal addr As IntPtr) As Int16
        Dim _rtnBytes(1) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 2, vbNull)
        Return BitConverter.ToInt16(_rtnBytes, 0)
    End Function
    Public Function ReadInt32(ByVal addr As IntPtr) As Int32
        Dim _rtnBytes(3) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 4, vbNull)

        Return BitConverter.ToInt32(_rtnBytes, 0)
    End Function
    Public Function ReadInt64(ByVal addr As IntPtr) As Int64
        Dim _rtnBytes(7) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 8, vbNull)
        Return BitConverter.ToInt64(_rtnBytes, 0)
    End Function
    Public Function ReadUInt16(ByVal addr As IntPtr) As UInt16
        Dim _rtnBytes(1) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 2, vbNull)
        Return BitConverter.ToUInt16(_rtnBytes, 0)
    End Function
    Public Function ReadUInt32(ByVal addr As IntPtr) As UInt32
        Dim _rtnBytes(3) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 4, vbNull)
        Return BitConverter.ToUInt32(_rtnBytes, 0)
    End Function
    Public Function ReadUInt64(ByVal addr As IntPtr) As UInt64
        Dim _rtnBytes(7) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 8, vbNull)
        Return BitConverter.ToUInt64(_rtnBytes, 0)
    End Function
    Public Function ReadFloat(ByVal addr As IntPtr) As Single
        Dim _rtnBytes(3) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 4, vbNull)
        Return BitConverter.ToSingle(_rtnBytes, 0)
    End Function
    Public Function ReadDouble(ByVal addr As IntPtr) As Double
        Dim _rtnBytes(7) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 8, vbNull)
        Return BitConverter.ToDouble(_rtnBytes, 0)
    End Function
    Public Function ReadIntPtr(ByVal addr As IntPtr) As IntPtr
        Dim _rtnBytes(IntPtr.Size - 1) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, IntPtr.Size, Nothing)
        If IntPtr.Size = 4 Then
            Return New IntPtr(BitConverter.ToUInt32(_rtnBytes, 0))
        Else
            Return New IntPtr(BitConverter.ToInt64(_rtnBytes, 0))
        End If
    End Function
    Public Function ReadBytes(ByVal addr As IntPtr, ByVal size As Int32) As Byte()
        Dim _rtnBytes(size - 1) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, size, vbNull)
        Return _rtnBytes
    End Function
    Private Function ReadAsciiStr(ByVal addr As UInteger) As String
        Dim Str As String = ""
        Dim cont As Boolean = True
        Dim loc As Integer = 0

        Dim bytes(&H10) As Byte

        ReadProcessMemory(_targetProcessHandle, addr, bytes, &H10, vbNull)

        While (cont And loc < &H10)
            If bytes(loc) > 0 Then

                Str = Str + Convert.ToChar(bytes(loc))

                loc += 1
            Else
                cont = False
            End If
        End While

        Return Str
    End Function
    Private Function ReadUnicodeStr(ByVal addr As UInteger) As String

        'Doesn't understand Unicode, just treat it as an ASCII string with 0's between bytes

        Dim Str As String = ""
        Dim cont As Boolean = True
        Dim loc As Integer = 0

        Dim bytes(&H20) As Byte

        ReadProcessMemory(_targetProcessHandle, addr, bytes, &H20, vbNull)

        While (cont And loc < &H20)
            If bytes(loc) > 0 Then

                Str = Str + Convert.ToChar(bytes(loc))

                loc += 2
            Else
                cont = False
            End If
        End While

        Return Str
    End Function

    Public Sub WriteInt32(ByVal addr As IntPtr, val As Int32)
        WriteProcessMemory(_targetProcessHandle, addr, BitConverter.GetBytes(val), 4, Nothing)
    End Sub
    Public Sub WriteUInt32(ByVal addr As IntPtr, val As UInt32)
        WriteProcessMemory(_targetProcessHandle, addr, BitConverter.GetBytes(val), 4, Nothing)
    End Sub
    Public Sub WriteFloat(ByVal addr As IntPtr, val As Single)
        WriteProcessMemory(_targetProcessHandle, addr, BitConverter.GetBytes(val), 4, Nothing)
    End Sub
    Public Sub WriteBytes(ByVal addr As IntPtr, val As Byte())
        WriteProcessMemory(_targetProcessHandle, addr, val, val.Length, Nothing)
    End Sub
    Public Sub WriteAsciiStr(addr As UInteger, str As String)
        WriteProcessMemory(_targetProcessHandle, addr, System.Text.Encoding.ASCII.GetBytes(str), str.Length, Nothing)
    End Sub

    Private Sub DSCM_Close(sender As Object, e As EventArgs) Handles MyBase.FormClosed
        chkDebugDrawing.Checked = False
        chkNamedNodes.Checked = False
        chkExpand.Checked = False
    End Sub

    Private Sub DSCM_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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
        beta = (ReadUInt32(&H400080) = &HE91B11E2&)
        If beta Then
            MsgBox("Beta version detected.  Disconnecting from process.")
            DetachFromProcess()
        End If



        dgvMPNodes.Columns.Add("Name", "Name")
        dgvMPNodes.Columns(0).Width = 180
        dgvMPNodes.Columns(0).ValueType = GetType(String)
        dgvMPNodes.Columns.Add("Steam ID", "Steam ID")
        dgvMPNodes.Columns(1).Width = 145
        dgvMPNodes.Columns(1).ValueType = GetType(String)
        dgvMPNodes.Columns.Add("SL", "SL")
        dgvMPNodes.Columns(2).Width = 60
        dgvMPNodes.Columns(2).ValueType = GetType(Integer)
        dgvMPNodes.Columns.Add("Phantom Type", "Phantom Type")
        dgvMPNodes.Columns(3).Width = 80
        dgvMPNodes.Columns(3).ValueType = GetType(String)
        dgvMPNodes.Columns.Add("MP Area", "MP Area")
        dgvMPNodes.Columns(4).Width = 80
        dgvMPNodes.Columns(4).ValueType = GetType(Integer)
        dgvMPNodes.Columns.Add("World", "World")
        dgvMPNodes.Columns(5).Width = 200
        dgvMPNodes.Columns(5).ValueType = GetType(String)

        dgvMPNodes.Font = New Font("Consolas", 10)

        'Check version number in new thread, so main thread isn't delayed.
        'Compares value on server to date in label on main form
        updTrd = New Thread(AddressOf updatecheck)
        updTrd.IsBackground = True
        updTrd.Start()
    End Sub
    Private Sub updatecheck()
        Try
            My.Computer.Network.DownloadFile("http://wulf2k.ca/pc/das/dscm-ver.txt", Path.GetTempPath & "\dscm-ver.txt", "", "", False, 9800, True)
            Dim ver = File.ReadAllLines(Path.GetTempPath & "\dscm-ver.txt")(0)

            newver = (ver > lblVer.Text.Replace(".", ""))
        Catch ex As Exception

        End Try
    End Sub
    Private Sub refTimer_Tick() Handles refTimer.Tick
        Dim dbgboost As Integer = 0
        Dim tmpptr As Integer = 0

        'Text indicating new version is hidden if DSCM is expanded, only care if it's seen at the start anyway.
        lblNewVersion.Visible = (newver And Not chkExpand.Checked)

        chkDebugDrawing.Checked = (ReadBytes(&HFA256C, 1)(0) = 1)

        tmpptr = ReadUInt32(&H137E204)
        nmbMPChannel.Value = ReadBytes(tmpptr + &HB69, 1)(0)


        'If original code has been replaced with a JMP, then Named Node functionality is enabled
        chkNamedNodes.Checked = (ReadBytes(&H55A550, 1)(0) = &HE9)

        tmpptr = ReadInt32(&H137F834)
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
            txtSelfSteamID.Text = ReadAsciiStr(ReadInt32(&H137E204) + &HA00)
        End If

        tmpptr = ReadInt32(&H137E204)
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
        Dim oneKey As Boolean
        Dim twoKey As Boolean

        ctrlkey = GetAsyncKeyState(Keys.ControlKey)
        oneKey = GetAsyncKeyState(Keys.D1)
        twoKey = GetAsyncKeyState(Keys.D2)

        If (ctrlkey And oneKey) And Not (DSCM.ctrlHeld And DSCM.oneHeld) Then
            DSCM.chkDebugDrawing.Checked = Not DSCM.chkDebugDrawing.Checked
        End If
        If (ctrlkey And twoKey) And Not (DSCM.ctrlHeld And DSCM.twoheld) Then
            DSCM.chkNamedNodes.Checked = Not DSCM.chkNamedNodes.Checked
        End If

        DSCM.ctrlHeld = ctrlkey
        DSCM.oneHeld = oneKey
        DSCM.twoheld = twoKey
    End Sub
    Private Sub frmResize() Handles Me.Resize
        dgvMPNodes.Width = Me.Width - 50
        dgvMPNodes.Height = Me.Height - 200

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
    End Sub

    Private Sub btnReconnect_Click(sender As Object, e As EventArgs) Handles btnReconnect.Click
        namedNodePtr = 0
        forceIdPtr = 0
        nodeDumpPtr = 0
        attemptIdPtr = 0

        DetachFromProcess()
        TryAttachToProcess("darksouls")

        'Note to self, push beta & debug check out to its own sub.
        beta = (ReadUInt32(&H400080) = &HE91B11E2&)
        If beta Then
            MsgBox("Beta version detected.  Disconnecting from process.")
            DetachFromProcess()
        End If
        chkExpand.Checked = False
    End Sub

    Private Sub chkNamedNodes_CheckedChanged(sender As Object, e As EventArgs) Handles chkNamedNodes.CheckedChanged
        Dim TargetBufferSize = 1024

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H6B

        If chkNamedNodes.Checked Then

            'If memory has not previously been allocated then allocate, otherwise use previous allocation
            'Memory leaks still exists if somebody were to repeatedly reattach to the process, so...  don't do that.
            If namedNodePtr = 0 Then
                namedNodePtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
            End If

            'Note to self, comment in the actual ASM code that's being injected here before it gets lost

            bytes = {&H8B, &H44, &H24, &H10, &H50, &H8B, &HC3, &H8B, &HD9, &H81, &HE3, &H0, &HFB, &H0, &H0, &H81,
                    &HFB, &H0, &HFB, &H0, &H0, &H8B, &HD8, &HF, &H84, &H5, &H0, &H0, &H0, &HE9, &H46, &H0,
                    &H0, &H0, &H8B, &H5B, &HD0, &H83, &HFB, &H0, &HF, &H84, &H14, &H0, &H0, &H0, &H8B, &H5B,
                    &H14, &H83, &HFB, &H0, &HF, &H84, &H8, &H0, &H0, &H0, &H83, &HC3, &H30, &HE9, &H7, &H0,
                    &H0, &H0, &H8B, &HD8, &HE9, &H1F, &H0, &H0, &H0, &H50, &HB8, &H0, &H0, &H0, &H0, &H83,
                    &HF8, &H20, &HF, &H84, &H9, &H0, &H0, &H0, &H8A, &H13, &H88, &H17, &H40, &H43, &H47, &HEB,
                    &HEE, &H83, &HEB, &H20, &H83, &HEF, &H20, &H58, &H58, &H56, &HE9, &H0, &H0, &H0, &H0}

            'Modify final JMP above to return to instruction after original hook
            bytes2 = BitConverter.GetBytes((&H55A550 - &H6A) - namedNodePtr)
            Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, namedNodePtr, bytes, TargetBufferSize, 0)

            'Insert hook to jump to allocated memory above
            bytes = {&HE9, 0, 0, 0, 0}
            bytes2 = BitConverter.GetBytes((namedNodePtr - (&H55A550) - 5))
            Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, (&H55A550), bytes, bytes.Length, 0)
        Else
            'Remove hook, restore original isntruction
            bytes = {&H8B, &H44, &H24, &H10, &H56}
            WriteProcessMemory(_targetProcessHandle, (&H55A550), bytes, bytes.Length, 0)
        End If
    End Sub
    Private Sub chkDebugDrawing_CheckedChanged(sender As Object, e As EventArgs) Handles chkDebugDrawing.CheckedChanged
        If chkDebugDrawing.Checked Then
            WriteBytes(&HFA256C, {&H1})
        Else
            WriteBytes(&HFA256C, {&H0})
        End If
    End Sub
    Private Sub nmbMPChannel_ValueChanged(sender As Object, e As EventArgs) Handles nmbMPChannel.ValueChanged
        Dim tmpptr As Integer
        tmpptr = ReadUInt32(&H137E204)
        WriteBytes(tmpptr + &HB69, {nmbMPChannel.Value})
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


        nodeCount = ReadInt32(&H1362DD0)
        SteamNodeList = ReadInt32(&H1362DCC)
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

        Dim tmpptr As Integer = ReadInt32(&H137E204)
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
    End Sub

    Private Sub chkExpand_CheckedChanged(sender As Object, e As EventArgs) Handles chkExpand.CheckedChanged

        'Note to self, buffer is overly large.  Trim down some day.
        Dim TargetBufferSize = 4096

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H78

        If chkExpand.Checked Then

            If nodeDumpPtr = 0 Then
                nodeDumpPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
            End If

            'Note to self, comment in actual ASM code here before it gets lost
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
            bytes2 = BitConverter.GetBytes((&HBE637E - &H77) - nodeDumpPtr)
            Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, nodeDumpPtr, bytes, TargetBufferSize, 0)

            'Insert the hook
            bytes = {&HE9, 0, 0, 0, 0}
            bytes2 = BitConverter.GetBytes((nodeDumpPtr - (&HBE637E) - 5))
            Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, (&HBE637E), bytes, bytes.Length, 0)
            refMpData.Start()

            Me.Width = 800
            Me.Height = 680
            dgvMPNodes.Visible = True
            dgvMPNodes.Location = New Point(25, 125)
            lblNewVersion.Visible = False

        Else
            'Restore original instruction
            bytes = {&H66, &HF, &HD6, &H46, &H14}
            WriteProcessMemory(_targetProcessHandle, (&HBE637E), bytes, bytes.Length, 0)
            refMpData.Stop()

            Me.Width = 450
            Me.Height = 190
            dgvMPNodes.Visible = False
        End If
    End Sub

    Private Sub nmbMaxNodes_ValueChanged(sender As Object, e As EventArgs) Handles nmbMaxNodes.ValueChanged
        Dim tmpptr As Integer

        tmpptr = ReadInt32(&H137F834)
        tmpptr = ReadInt32(tmpptr + &H38)
        WriteInt32(tmpptr + &H70, nmbMaxNodes.Value)
    End Sub

    Private Sub txtTargetSteamID_LostFocus(sender As Object, e As EventArgs) Handles txtTargetSteamID.LostFocus
        'Auto-convert Steam ID after clicking out of the textbox
        'If it starts with a 7, assume it's the Steam64 ID in int64 form.

        Dim steamIdInt As Int64
        If txtTargetSteamID.Text.Length > 1 Then
            If txtTargetSteamID.Text(0) = "7" Then
                steamIdInt = txtTargetSteamID.Text
                txtTargetSteamID.Text = "0" & Hex(steamIdInt).ToLower
            End If
        End If
    End Sub

    Private Sub chkAttemptConn_CheckedChanged(sender As Object, e As EventArgs) Handles btnAttemptId.Click
        Try
            Dim steamApiBase As Integer = CType(steamApiDllModule.BaseAddress, Int32)

            'Note to self, find a better way of confirming that this is the correct address for this function.
            'If nothing else, just compare the bytes at the address.
            'Some people have reported crashes.
            Dim steamApiNetworking As Integer = steamApiBase + &H2F70

            If steamApiBase = 0 Then
                MsgBox("Unable to locate necessary function in memory.  Aborting connection attempt.")
            Else
                Dim DataPacket1 As Integer

                Dim TargetBufferSize = 1024
                Dim bytes() As Byte
                Dim bytes2() As Byte

                If attemptIdPtr = 0 Then
                    attemptIdPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
                End If
                DataPacket1 = attemptIdPtr + &H100

                'Note to self, as usual, comment in the actual ASM before it gets lost.
                bytes = {&HE8, &H0, &H0, &H0, &H0, &H8B, &H10, &H8B, &H12, &H6A, &H1, &H6A, &H2, &HBF, &H43, &H0, _
                         &H0, &H0, &H57, &HB9, &H0, &H0, &H0, &H0, &H51, &H68, &H0, &H0, &H0, &H0, &H68, &H0, _
                         &H0, &H0, &H0, &H8B, &HC8, &HFF, &HD2, &HC3}

                'Set steam_api.SteamNetworking call
                bytes2 = BitConverter.GetBytes(steamApiNetworking - attemptIdPtr - 5)
                Array.Copy(bytes2, 0, bytes, &H1, bytes2.Length)

                bytes2 = BitConverter.GetBytes(attemptIdPtr + &H100)
                Array.Copy(bytes2, 0, bytes, &H14, bytes2.Length)

                bytes2 = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Left(txtTargetSteamID.Text, 8), 16))
                Array.Copy(bytes2, 0, bytes, &H1A, bytes2.Length)

                bytes2 = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Right(txtTargetSteamID.Text, 8), 16))
                Array.Copy(bytes2, 0, bytes, &H1F, bytes2.Length)

                WriteProcessMemory(_targetProcessHandle, attemptIdPtr, bytes, bytes.Length, 0)


                'Set up data packet
                bytes = {&H1}
                WriteProcessMemory(_targetProcessHandle, DataPacket1, bytes, bytes.Length, 0)

                Dim selfSteamName As String
                selfSteamName = ReadUnicodeStr(ReadInt32(&H1362DD4) + &H30)

                bytes = System.Text.Encoding.Unicode.GetBytes(selfSteamName)
                WriteProcessMemory(_targetProcessHandle, DataPacket1 + 1, bytes, bytes.Length, 0)

                CreateRemoteThread(_targetProcessHandle, 0, 0, attemptIdPtr, 0, 0, 0)
            End If
        Catch ex As Exception
            MsgBox("Well, that failed spectacularly.  Why?" & Environment.NewLine & "I dunno, I'm just an inanimate message box.  Ask a human about the following message: " & _
                   Environment.NewLine & Environment.NewLine & ex.Message)
        End Try
    End Sub
End Class
