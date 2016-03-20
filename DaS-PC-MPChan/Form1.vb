Imports System.Runtime.InteropServices
Imports System.Threading

Public Class Form1
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

    Dim ctrlHeld As Boolean
    Dim oneHeld As Boolean
    Dim twoheld As Boolean

    Dim beta As Boolean
    Dim debug As Boolean

    Dim nodeDumpPtr As Integer


    Public Function TryAttachToProcess(ByVal windowCaption As String) As Boolean
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            'If pp.MainWindowTitle.ToLower.Equals(windowCaption.ToLower) Then
            If pp.ProcessName.ToLower.Equals(windowCaption.ToLower) Then
                'found it! proceed.
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
                MessageBox.Show("OpenProcess() FAIL! Are you Administrator??")
            Else
                'if we get here, all connected and ready to use ReadProcessMemory()
                TryAttachToProcess = True
                'MessageBox.Show("OpenProcess() OK")
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
                'MessageBox.Show("MemReader::Detach() OK")
            Catch ex As Exception
                'MessageBox.Show("MemoryManager::DetachFromProcess::CloseHandle error " & Environment.NewLine & ex.Message)
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

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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
        dgvMPNodes.Columns(0).Width = 140
        dgvMPNodes.Columns.Add("Steam ID", "Steam ID")
        dgvMPNodes.Columns(1).Width = 140
        dgvMPNodes.Columns(1).Visible = False
        dgvMPNodes.Columns.Add("SL", "SL")
        dgvMPNodes.Columns(2).Width = 60
        dgvMPNodes.Columns.Add("Phantom Type", "Phantom Type")
        dgvMPNodes.Columns(3).Width = 60
        dgvMPNodes.Columns.Add("MP Area", "MP Area")
        dgvMPNodes.Columns(4).Width = 60
        dgvMPNodes.Columns.Add("World", "World")
        dgvMPNodes.Columns(5).Width = 150

    End Sub
    Private Sub refTimer_Tick() Handles refTimer.Tick
        Dim dbgboost As Integer = 0
        Dim tmpptr As Integer = 0
        debug = (ReadUInt32(&H400080) = &HCE9634B4&)

        If debug Then dbgboost = &H3C20
        chkDebugDrawing.Checked = (ReadBytes(&HFA256C + dbgboost, 1)(0) = 1)

        If debug Then dbgboost = &H41C0
        tmpptr = ReadUInt32(&H137E204 + dbgboost)
        nmbMPChannel.Value = ReadBytes(tmpptr + &HB69, 1)(0)

        chkNamedNodes.Checked = (ReadBytes(&H55A550, 1)(0) = &HE9)



        tmpptr = ReadInt32(&H137E204)
        If Not tmpptr = 0 Then
            dgvMPNodes.Rows(0).Cells(4).Value = ReadInt32(tmpptr + &HA14)
        End If
    End Sub
    Private Shared Sub hotkeyTimer_Tick() Handles hotkeyTimer.Tick
        Dim ctrlkey As Boolean
        Dim oneKey As Boolean
        Dim twoKey As Boolean

        ctrlkey = GetAsyncKeyState(Keys.ControlKey)
        oneKey = GetAsyncKeyState(Keys.D1)
        twoKey = GetAsyncKeyState(Keys.D2)

        If (ctrlkey And oneKey) And Not (Form1.ctrlHeld And Form1.oneHeld) Then
            Form1.chkDebugDrawing.Checked = Not Form1.chkDebugDrawing.Checked
        End If
        If (ctrlkey And twoKey) And Not (Form1.ctrlHeld And Form1.twoheld) Then
            Form1.chkNamedNodes.Checked = Not Form1.chkNamedNodes.Checked
        End If

        Form1.ctrlHeld = ctrlkey
        Form1.oneHeld = oneKey
        Form1.twoheld = twoKey
    End Sub
    Private Sub frmResize() Handles Me.Resize
        dgvMPNodes.Width = Me.Width - 50
        dgvMPNodes.Height = Me.Height - 200

        btnReconnect.Location = New Point(1, Me.Height - 50)
        lblVer.Location = New Point(Me.Width - 150, Me.Height - 40)
    End Sub

    Private Sub btnReconnect_Click(sender As Object, e As EventArgs) Handles btnReconnect.Click
        DetachFromProcess()
        TryAttachToProcess("darksouls")
        beta = (ReadUInt32(&H400080) = &HE91B11E2&)
        If beta Then
            MsgBox("Beta version detected.  Disconnecting from process.")
            DetachFromProcess()
        End If
    End Sub

    Private Sub chkNamedNodes_CheckedChanged(sender As Object, e As EventArgs) Handles chkNamedNodes.CheckedChanged
        Dim TargetBufferSize = 1024
        Dim insertPtr As Integer
        Dim dbgboost As Integer = 0

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H6B

        If chkNamedNodes.Checked Then
            If debug Then
                MsgBox("This function is not currently available in debug.")
                chkNamedNodes.Checked = False
            Else
                insertPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)

                bytes = {&H8B, &H44, &H24, &H10, &H50, &H8B, &HC3, &H8B, &HD9, &H81, &HE3, &H00, &HFB, &H00, &H00, &H81,
                        &HFB, &H00, &HFB, &H00, &H00, &H8B, &HD8, &H0F, &H84, &H05, &H00, &H00, &H00, &HE9, &H46, &H00,
                        &H00, &H00, &H8B, &H5B, &HD0, &H83, &HFB, &H00, &H0F, &H84, &H14, &H00, &H00, &H00, &H8B, &H5B,
                        &H14, &H83, &HFB, &H00, &H0F, &H84, &H08, &H00, &H00, &H00, &H83, &HC3, &H30, &HE9, &H07, &H00,
                        &H00, &H00, &H8B, &HD8, &HE9, &H1F, &H00, &H00, &H00, &H50, &HB8, &H00, &H00, &H00, &H00, &H83,
                        &HF8, &H20, &H0F, &H84, &H09, &H00, &H00, &H00, &H8A, &H13, &H88, &H17, &H40, &H43, &H47, &HEB,
                        &HEE, &H83, &HEB, &H20, &H83, &HEF, &H20, &H58, &H58, &H56, &HE9, &H00, &H00, &H00, &H00}
                bytes2 = BitConverter.GetBytes((&H55A550 - &H6A + dbgboost) - insertPtr)
                Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, insertPtr, bytes, TargetBufferSize, 0)

                'MsgBox(Hex(insertPtr))
                bytes = {&HE9, 0, 0, 0, 0}
                bytes2 = BitConverter.GetBytes((insertPtr - (&H55A550 + dbgboost) - 5))
                Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, (&H55A550 + dbgboost), bytes, bytes.Length, 0)
            End If
        Else
            bytes = {&H8B, &H44, &H24, &H10, &H56}
            WriteProcessMemory(_targetProcessHandle, (&H55A550 + dbgboost), bytes, bytes.Length, 0)
        End If
    End Sub
    Private Sub chkDebugDrawing_CheckedChanged(sender As Object, e As EventArgs) Handles chkDebugDrawing.CheckedChanged
        Dim dbgboost As Integer = 0

        If debug Then dbgboost = &H3C20

        If chkDebugDrawing.Checked Then
            WriteBytes(&HFA256C + dbgboost, {&H1})
        Else
            WriteBytes(&HFA256C + dbgboost, {&H0})
        End If
    End Sub
    Private Sub nmbMPChannel_ValueChanged(sender As Object, e As EventArgs) Handles nmbMPChannel.ValueChanged
        Dim dbgboost As Integer = 0
        Dim tmpptr As Integer
        If debug Then dbgboost = &H41C0
        tmpptr = ReadUInt32(&H137E204 + dbgboost)
        WriteBytes(tmpptr + &HB69, {nmbMPChannel.Value})
    End Sub

    Private Sub refMpData_Tick() Handles refMpData.Tick


        Dim nodeCount As Integer
        Dim row(9) As String

        Dim SteamNodesPtr As Integer
        Dim SteamNodeList As Integer
        Dim SteamData1 As Integer
        Dim SteamData2 As Integer


        nodeCount = ReadInt32(&H1362DD0)
        SteamNodeList = ReadInt32(&H1362DCC)
        SteamNodesPtr = ReadInt32(SteamNodeList)


        dgvMPNodes.Rows.Clear()



        For i = 0 To nodeCount - 1
            SteamData1 = ReadInt32(SteamNodesPtr + &HC)
            SteamData2 = ReadInt32(SteamData1 + &HC)
            row(0) = ReadUnicodeStr(SteamData1 + &H30)
            row(1) = ReadUnicodeStr(SteamData2 + &H30)

            SteamNodesPtr = ReadInt32(SteamNodesPtr)

            dgvMPNodes.Rows.Add(row)
        Next

        Dim tmpptr As Integer = ReadInt32(&H137E204)
        dgvMPNodes.Rows(0).Cells(2).Value = ReadInt32(tmpptr + &HA30)
        dgvMPNodes.Rows(0).Cells(3).Value = ReadInt32(tmpptr + &HA28)
        dgvMPNodes.Rows(0).Cells(4).Value = ReadInt32(tmpptr + &HA14)
        dgvMPNodes.Rows(0).Cells(5).Value = ReadInt8(tmpptr + &HA13) & "-" & ReadInt8(tmpptr + &HA12)


        Dim cont = True
        Dim tmpid As String

        tmpptr = nodeDumpPtr + &H200

        While cont
            tmpid = ReadAsciiStr(tmpptr)
            cont = Not (tmpid = Nothing)

            For i = 1 To dgvMPNodes.Rows.Count - 1
                If (dgvMPNodes.Rows(i).Cells(1).Value = tmpid) And cont Then

                    'SL
                    dgvMPNodes.Rows(i).Cells(2).Value = ReadInt16(tmpptr + &H26)

                    'Phantom
                    dgvMPNodes.Rows(i).Cells(3).Value = ReadInt8(tmpptr + &H24)



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
    End Sub

    Private Sub chkExpand_CheckedChanged(sender As Object, e As EventArgs) Handles chkExpand.CheckedChanged

        Dim TargetBufferSize = 4096
        Dim insertPtr As Integer
        Dim dbgboost As Integer = 0

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H78

        If chkExpand.Checked Then
            If debug Then
                MsgBox("This function is not currently available in debug.")
                chkExpand.Checked = False
            Else
                insertPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)
                nodeDumpPtr = insertPtr

                bytes = {&H50, &H53, &H51, &H52, &H56, &H57, &HBF, &H00, &H00, &H00, &H00, &HB8, &H00, &H00, &H00, &H00,
                            &HBB, &H00, &H00, &H00, &H00, &HB9, &H00, &H00, &H00, &H00, &HBA, &H00, &H00, &H00, &H00, &H8A,
                            &H1F, &H80, &HFB, &H00, &H0F, &H84, &H30, &H00, &H00, &H00, &H8A, &H06, &H8A, &H1F, &H46, &H47,
                            &H41, &H38, &HD8, &H0F, &H85, &H13, &H00, &H00, &H00, &H83, &HF9, &H11, &H75, &HEC, &H29, &HCE,
                            &H29, &HCF, &HB9, &H00, &H00, &H00, &H00, &HE9, &H0E, &H00, &H00, &H00, &H29, &HCE, &H29, &HCF,
                            &HB9, &H00, &H00, &H00, &H00, &H83, &HC7, &H30, &HEB, &HC5, &H8A, &H1E, &H88, &H1F, &H46, &H47,
                            &H41, &H83, &HF9, &H30, &H0F, &H84, &H02, &H00, &H00, &H00, &HEB, &HEE, &H5F, &H5E, &H5A, &H59,
                            &H5B, &H58, &H66, &H0F, &HD6, &H46, &H14, &HE9, &H00, &H00, &H00, &H00}

                'Adjust EDI
                bytes2 = BitConverter.GetBytes(nodeDumpPtr + &H200)
                Array.Copy(bytes2, 0, bytes, &H7, bytes2.Length)

                'Adjust the jump home
                bytes2 = BitConverter.GetBytes((&HBE637E - &H77 + dbgboost) - insertPtr)
                Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, insertPtr, bytes, TargetBufferSize, 0)

                bytes = {&HE9, 0, 0, 0, 0}
                bytes2 = BitConverter.GetBytes((insertPtr - (&HBE637E + dbgboost) - 5))
                Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, (&HBE637E + dbgboost), bytes, bytes.Length, 0)
                refMpData.Start()

                Me.Width = 575
                Me.Height = 530
                dgvMPNodes.Visible = True
                dgvMPNodes.Location = New Point(25, 125)
            End If
        Else
            bytes = {&H66, &H0F, &HD6, &H46, &H14}
            WriteProcessMemory(_targetProcessHandle, (&HBE637E + dbgboost), bytes, bytes.Length, 0)
            refMpData.Stop()

            Me.Width = 341
            Me.Height = 177
            dgvMPNodes.Visible = False

        End If
    End Sub
End Class
