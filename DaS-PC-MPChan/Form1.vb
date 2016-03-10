Imports System.Runtime.InteropServices
Imports System.Threading

Public Class Form1

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

    Public Function TryAttachToProcess(ByVal windowCaption As String) As Boolean
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            If pp.MainWindowTitle.ToLower.Equals(windowCaption.ToLower) Then
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


        TryAttachToProcess("DARK SOULS")
        beta = (ReadUInt32(&H400080) = &HE91B11E2&)
        If beta Then
            MsgBox("Beta version detected.  Disconnecting from process.")
            DetachFromProcess()
        End If
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

    Private Sub btnReconnect_Click(sender As Object, e As EventArgs) Handles btnReconnect.Click
        DetachFromProcess()
        TryAttachToProcess("DARK SOULS")
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

        Dim bytjmp As Integer = &H32

        If chkNamedNodes.Checked Then
            If debug Then
                MsgBox("This function is not currently available in debug.")
                chkNamedNodes.Checked = False
            Else
                insertPtr = VirtualAllocEx(_targetProcessHandle, 0, TargetBufferSize, MEM_COMMIT, PAGE_READWRITE)

                bytes = {&H81, &HFC, 0, &HFC, &H18, 0, &H8B, &H44, &H24, &H10, &H77, &H24, &H8B, &H5B,
                         &HD0, &H8B, &H5B, &H14, &H83, &HC3, &H30, &H50, &HB8, 0, 0, 0, 0, &H83, &HF8,
                         &H1E, &H74, &H9, &H8A, &H13, &H88, &H17, &H40, &H43, &H47, &HEB, &HF2, &H83,
                         &HEB, &H1E, &H83, &HEF, &H1E, &H58, &H56, &HE9, 0, 0, 0, 0}
                bytes2 = BitConverter.GetBytes((&H55A550 - &H31 + dbgboost) - insertPtr)
                Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, insertPtr, bytes, TargetBufferSize, 0)

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
End Class
