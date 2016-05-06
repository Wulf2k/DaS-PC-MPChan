Public Class DSProcessAttachException
    Inherits System.ApplicationException

    Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class DarkSoulsProcess
    Implements IDisposable
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

    Public ConnectedNodes As New Dictionary(Of String, DSNode)()
    Public SelfNode As New DSNode()

    Protected disposed As Boolean = False

    Sub New()
        attachToProcess()
        Dim beta = (ReadUInt32(dsBase + &H80) = &HE91B11E2&)
        If beta Then
            detachFromProcess()
            Throw New DSProcessAttachException("Dark Souls beta is not supported")
        End If
        'TODO (chronial): Is this safe here, or should we wait a second because Dark Souls might be booting up?
        findDllAddresses()
        disableLowFPSDisonnect()
        SetupNodeDumpHook()
    End Sub

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        If Not Me.disposed Then
            If watchdog Then
                'WriteBytes(dsPWBase + &H6E41, {&HE8, &H8E, &HD5, &HFF, &HFF})
            End If

            TearDownNodeDumpHook()
            detachFromProcess()
            disposed = True
        End If
    End Sub

    Protected Overrides Sub Finalize()
        Dispose()
    End Sub

    Private Sub attachToProcess()
        Dim windowCaption As String = "darksouls"
        Dim _allProcesses() As Process = Process.GetProcesses
        For Each pp As Process In _allProcesses
            If pp.ProcessName.ToLower.Equals(windowCaption.ToLower) AndAlso Not pp.HasExited Then
                attachToProcess(pp)
                Return
            End If
        Next
        Throw New DSProcessAttachException("Dark Souls not running")
    End Sub

    Private Sub attachToProcess(ByVal proc As Process)
        If _targetProcessHandle = IntPtr.Zero Then 'not already attached
            _targetProcess = proc
            _targetProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, False, _targetProcess.Id)
            If _targetProcessHandle = 0 Then
                Throw New DSProcessAttachException("OpenProcess() failed. Check Permissions")
            End If
        Else
            MessageBox.Show("Already attached! (Please Detach first?)")
        End If
    End Sub
    Private Sub findDllAddresses()
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
                            'this is ineffective at disabling PVP Watchdog's node write
                            'WriteBytes(dsPWBase + &H6E41, {&H90, &H90, &H90, &H90, &H90})
                        End If
                    End If

                Case "steam_api.dll" 'Find steam_api.dll for ability to directly add SteamIDs as nodes
                    steamApiBasePtr = dll.baseaddress
                    steamApiBase = steamApiBasePtr
                    steamApiDllModule = dll

            End Select
        Next
    End Sub
    Private Sub disableLowFPSDisonnect()
        If ReadUInt8(dsBase + &H978425) = &HE8& Then
            WriteBytes(dsBase + &H978425, {&H90, &H90, &H90, &H90, &H90})
        End If
    End Sub
    Private Sub detachFromProcess()
        If Not (_targetProcessHandle = IntPtr.Zero) Then
            _targetProcess = Nothing
            Try
                CloseHandle(_targetProcessHandle)
                _targetProcessHandle = IntPtr.Zero
            Catch ex As Exception
            End Try
        End If
    End Sub
    Public ReadOnly Property IsAttached As Boolean
        Get
            Return Not _targetProcess.HasExited
        End Get
    End Property
    Public Property DrawNodes As Boolean
        Get
            Return (ReadBytes(dsBase + &HBA256C, 1)(0) = 1)
        End Get
        Set(value As Boolean)
            Dim cmpLoc As Integer = dsBase + &HBA256C
            Dim TargetBufferSize = 1024

            Dim bytes() As Byte
            Dim bytes2() As Byte

            'Location in bytearray to insert jump location
            Dim bytjmp As Integer = &H6B
            Dim hookLoc As Integer = dsBase + &H15A550


            If value Then
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
                    namedNodePtr = 0
                    Throw New ApplicationException("DrawNodes code injection appears to have failed.")
                End If
            Else
                'Remove Named Node hook, restore original instruction
                bytes = {&H8B, &H44, &H24, &H10, &H56}
                WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)

                'Disable Node Drawing
                WriteBytes(cmpLoc, {&H0})
            End If
        End Set
    End Property

    Private Sub SetupNodeDumpHook()
        'Note to self, buffer is overly large.  Trim down some day.
        Dim TargetBufferSize = 4096

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H78
        Dim hookLoc As Integer = dsBase + &H7E637E

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
        Else
            nodeDumpPtr = 0
            Throw New ApplicationException("NodeDump code injection appears to have failed.")
        End If
    End Sub
    Private Sub TearDownNodeDumpHook()
        'Restore original instruction
        Dim bytes() As Byte = {&H66, &HF, &HD6, &H46, &H14}
        Dim hookLoc As Integer = dsBase + &H7E637E
        WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
    End Sub

    Public Property MaxNodes As Integer
        Get
            Dim tmpptr As Integer
            tmpptr = ReadInt32(dsBase + &HF7F834)
            tmpptr = ReadInt32(tmpptr + &H38)
            If Not tmpptr = 0 Then
                Return ReadInt32(tmpptr + &H70)
            Else
                Return 0
            End If
        End Get
        Set(value As Integer)
            Dim tmpptr As Integer
            tmpptr = ReadInt32(dsBase + &HF7F834)
            tmpptr = ReadInt32(tmpptr + &H38)
            WriteInt32(tmpptr + &H70, value)
        End Set
    End Property

    Public ReadOnly Property NodeCount As Integer
        Get
            Dim tmpptr = ReadInt32(dsBase + &HF7E204)
            If tmpptr = 0 Then
                Return 0
            End If
            Return ReadInt32(tmpptr + &HAE0)
        End Get
    End Property

    Public ReadOnly Property SelfSteamId As String
        Get
            Return ReadAsciiStr(ReadInt32(dsBase + &HF7E204) + &HA00)
        End Get
    End Property

    Public Sub ConnectToSteamId(ByVal steamId As String)
        Try
            If steamId.Length = 16 Then
                'Note to self, find a better way of confirming that this is the correct address for this function.
                'If nothing else, just compare the bytes at the address.
                Dim steamApiNetworking As Integer = steamApiBase + &H2F70

                'Extremely weak check to be sure we're at the right spot
                If Not ReadUInt8(steamApiNetworking) = &HA1& Then
                    'MsgBox("Unexpected byte at Steam_api.dll|Networking.  Game is likely to crash now." & Environment.NewLine &
                    'Environment.NewLine & "Please report this error to wulfs.throwaway.address@gmail.com.")

                    'Fail Silently rather than spam messages
                    Return

                End If

                If steamApiBase = 0 Then
                    'MsgBox("Unable to locate necessary function in memory.  Aborting connection attempt.")

                    'Fail Silently rather than spam messages
                    Return
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
            End If
        Catch ex As Exception

            'Fail Silently rather than spam messages

            'MsgBox("Well, that failed spectacularly.  Why?" & Environment.NewLine & "I dunno, I'm just an inanimate message box.  Ask a human about the following message: " &
            'Environment.NewLine & Environment.NewLine & ex.Message)
        End Try
    End Sub

    Public Sub UpdateNodes()
        Dim nodeCount As Integer = ReadInt32(dsBase + &HF62DD0)
        Dim basicNodeInfo As New Dictionary(Of String, String)
        Dim steamNodeList = ReadInt32(dsBase + &HF62DCC)
        Dim steamNodesPtr As Integer = ReadInt32(steamNodeList)
        For i = 0 To nodeCount - 1
            Dim node As New DSNode
            Dim steamData1 As Integer = ReadInt32(steamNodesPtr + &HC)
            Dim steamData2 As Integer = ReadInt32(steamData1 + &HC)
            node.SteamId = ReadUnicodeStr(steamData2 + &H30)
            node.CharacterName = ReadUnicodeStr(steamData1 + &H30)
            basicNodeInfo(node.SteamId) = node.CharacterName
            If node.SteamId = SelfSteamId
                SelfNode.SteamId = node.SteamId
                SelfNode.CharacterName = node.CharacterName
            End If
            steamNodesPtr = ReadInt32(steamNodesPtr)
        Next


        For Each key As String In New List(Of String)(ConnectedNodes.Keys)
            If Not basicNodeInfo.ContainsKey(key)
              ConnectedNodes.Remove(key)
            End If
        Next

        Dim nodeSteamId As String
        Dim nodePtr As Integer
        nodePtr = nodeDumpPtr + &H200
        While True
            nodeSteamId = ReadAsciiStr(nodePtr)
            WriteInt32(nodePtr, 0)
            If nodeSteamId = ""
                Exit While
            End If
             If basicNodeInfo.ContainsKey(nodeSteamId) Then
                Dim node As DSNode
                If ConnectedNodes.ContainsKey(nodeSteamId)
                    node = ConnectedNodes(nodeSteamId)
                Else
                    node = New DSNode()
                    node.SteamId = nodeSteamId
                    node.CharacterName = basicNodeInfo(nodeSteamId)
                    ConnectedNodes.Add(nodeSteamId, node)

                End If
                node.SoulLevel = Convert.ToInt32(ReadInt16(nodePtr + &H26))
                node.PhantomType = Convert.ToInt32(ReadInt8(nodePtr + &H24))
                node.MPZone = ReadInt32(nodePtr + &H28)
                node.World = ReadInt8(nodePtr + &H13) & "-" & ReadInt8(nodePtr + &H12)
            End If
            nodePtr += &H30
        End While

        Dim selfPtr As Integer = ReadInt32(dsBase + &HF7E204)
        SelfNode.SoulLevel = ReadInt32(selfPtr + &HA30)
        SelfNode.MPZone = ReadInt32(selfPtr + &HA14)
        SelfNode.World = ReadInt8(selfPtr + &HA13) & "-" & ReadInt8(selfPtr + &HA12)
        SelfNode.PhantomType = ReadInt32(selfPtr + &HA28)

        Dim tmpCharPtr As Integer = ReadInt32(dsBase + &HF78700)
        tmpCharPtr = ReadInt32(tmpCharPtr + &H8)
        SelfNode.Indictments = ReadInt32(tmpCharPtr + &HEC)
        selfNode.Covenant = ReadInt8(tmpCharPtr + &H10B)
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
    Public Function ReadUInt8(ByVal addr As IntPtr) As Byte
        Dim _rtnBytes(0) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 1, vbNull)
        Return _rtnBytes(0)
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
End Class
