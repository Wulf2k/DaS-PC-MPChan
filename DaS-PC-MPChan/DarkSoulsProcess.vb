Imports System.Text
Imports System.Threading

Public Class DSProcessAttachException
    Inherits System.ApplicationException

    Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class DSConnectException
    Inherits System.ApplicationException

    Sub New(message As String)
        MyBase.New(message)
    End Sub
End Class

Public Class AllocatedMemory
    Implements IDisposable
    Private Declare Function VirtualAllocEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, dwSize As IntPtr, flAllocationType As Integer, flProtect As Integer) As IntPtr
    Private Declare Function VirtualProtectEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, ByVal lpSize As IntPtr, ByVal dwNewProtect As UInt32, ByRef dwOldProtect As UInt32) As Boolean
    Private Declare Function VirtualFreeEx Lib "kernel32.dll" (hProcess As IntPtr, lpAddress As IntPtr, ByVal dwSize As Integer, ByVal dwFreeType As Integer) As Boolean
    Public Const MEM_COMMIT = 4096
    Public Const MEM_RELEASE = &H8000
    Public Const PAGE_READWRITE = 4
    Public Const PAGE_EXECUTE_READWRITE = &H40

    Private _process As IntPtr
    Private _address As IntPtr
    Sub New(targetProcessHandle As IntPtr, size As Integer)
        _process = targetProcessHandle
        _address = VirtualAllocEx(_process, 0, size, MEM_COMMIT, PAGE_READWRITE)
        If _address = 0 Then
            Throw New ApplicationException("Failed to allocate Memory")
        End If
        Dim oldProtectionOut As UInteger
        VirtualProtectEx(_process, _address, size, PAGE_EXECUTE_READWRITE, oldProtectionOut)
    End Sub
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        If _address <> 0 Then
            VirtualFreeEx(_process, _address, 0, MEM_RELEASE)
            _address = 0
        End If
    End Sub
    Protected Overrides Sub Finalize()
        Dispose()
    End Sub
    Public Shared Widening Operator CType(ByVal m As AllocatedMemory) As IntPtr
        Return m._address
    End Operator
    Public ReadOnly Property address As IntPtr
        Get
            Return _address
        End Get
    End Property
End Class

Public Class DarkSoulsProcess
    Implements IDisposable
    'Process/Memory Manipulation
    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByVal lpNumberOfBytesWritten As Integer) As Boolean
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpThreadAttributes As IntPtr, ByVal dwStackSize As Integer, ByVal lpStartAddress As IntPtr, ByVal lpParameter As IntPtr, ByVal dwCreationFlags As Integer, ByRef lpThreadId As IntPtr) As Integer

    Public Const PROCESS_VM_READ = &H10
    Public Const TH32CS_SNAPPROCESS = &H2
    Public Const PROCESS_CREATE_THREAD = &H2
    Public Const PROCESS_VM_OPERATION = &H8
    Public Const PROCESS_VM_WRITE = &H20
    Public Const PROCESS_ALL_ACCESS = &H1F0FFF

    Private _targetProcess As Process = Nothing
    Private _targetProcessHandle As IntPtr = IntPtr.Zero

    'Addresses of the various inserted functions
    Private namedNodeMemory As AllocatedMemory
    Private nodeDumpMemory As AllocatedMemory
    Private connectMemory As AllocatedMemory

    'Dark Souls
    Private dsBase As IntPtr = 0
    'Steam API
    Private steamApiBase As IntPtr = 0
    'PVP Watchdog
    Private watchdogBase As IntPtr = 0


    Private steamApiDllModule As ProcessModule

    Public ConnectedNodes As New Dictionary(Of String, DSNode)()
    Public SelfNode As New DSNode()

    Protected disposed As Boolean = False

    Sub New()
        attachToProcess()
        'Give the process a little time to start up
        Thread.Sleep(200)
        findDllAddresses()
        Dim beta = (ReadUInt32(dsBase + &H80) = &HE91B11E2&)
        If beta Then
            detachFromProcess()
            Throw New DSProcessAttachException("Dark Souls beta is not supported")
        End If
        disableLowFPSDisonnect()
        SetupNodeDumpHook()
    End Sub

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        If Not Me.disposed Then
            If watchdogBase Then
                'WriteBytes(watchdogBase + &H6E41, {&HE8, &H8E, &HD5, &HFF, &HFF})
            End If

            If connectMemory IsNot Nothing Then
                connectMemory.Dispose()
                connectMemory = Nothing
            End If
            DrawNodes = False
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
        For Each dll As ProcessModule In _targetProcess.Modules
            Select Case dll.ModuleName.ToLower
                Case "darksouls.exe"
                    dsBase = dll.BaseAddress

                Case "d3d9.dll"
                    If (dll.FileVersionInfo.ProductName IsNot Nothing AndAlso
                            dll.FileVersionInfo.ProductName.Contains("Watchdog")) Then
                        watchdogBase = dll.BaseAddress
                    End If

                Case "steam_api.dll" 'Find steam_api.dll for ability to directly add SteamIDs as nodes
                    steamApiBase = dll.BaseAddress
            End Select
        Next
        If dsBase = 0 Then Throw New DSProcessAttachException("Couldn't find Dark Souls base address")
        If steamApiBase = 0 Then Throw New DSProcessAttachException("Couldn't find Steam API base address")
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
    Public ReadOnly Property HasWatchdog As Boolean
        Get
            Return watchdogBase <> 0
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
            Dim hookLoc As IntPtr = dsBase + &H15A550

            If value Then
                'Changes instruction doing the compare rather than changing the value it compares against
                WriteBytes(cmpLoc, {&H1})

                'If memory has not previously been allocated then allocate, otherwise use previous allocation
                If namedNodeMemory Is Nothing Then
                    namedNodeMemory = New AllocatedMemory(_targetProcessHandle, TargetBufferSize)
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
                bytes2 = BitConverter.GetBytes(CType((hookLoc - &H6A) - namedNodeMemory.address, Int32))
                Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
                WriteProcessMemory(_targetProcessHandle, namedNodeMemory, bytes, TargetBufferSize, 0)

                If ReadUInt8(namedNodeMemory) = &H8B& Then
                    'Insert hook to jump to allocated memory above
                    bytes = {&HE9, 0, 0, 0, 0}
                    bytes2 = BitConverter.GetBytes(CType(namedNodeMemory - hookLoc - 5, Int32))
                    Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
                    WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
                Else
                    namedNodeMemory.Dispose()
                    namedNodeMemory = Nothing
                    Throw New ApplicationException("DrawNodes code injection appears to have failed.")
                End If
            Else
                'Remove Named Node hook, restore original instruction
                bytes = {&H8B, &H44, &H24, &H10, &H56}
                WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)

                'Disable Node Drawing
                WriteBytes(cmpLoc, {&H0})

                ' Free memory
                If namedNodeMemory IsNot Nothing Then
                    namedNodeMemory.Dispose()
                    namedNodeMemory = Nothing
                End If
            End If
        End Set
    End Property

    Private Sub SetupNodeDumpHook()
        'Note to self, buffer is overly large.  Trim down some day.
        Dim TargetBufferSize = 4096

        Dim bytes() As Byte
        Dim bytes2() As Byte

        Dim bytjmp As Integer = &H78
        Dim hookLoc As IntPtr = dsBase + &H7E637E

        If nodeDumpMemory Is Nothing Then
            nodeDumpMemory = New AllocatedMemory(_targetProcessHandle, TargetBufferSize)
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
        bytes2 = BitConverter.GetBytes(CType(nodeDumpMemory.address + &H200, UInt32))
        Array.Copy(bytes2, 0, bytes, &H7, bytes2.Length)

        'Adjust the jump home
        bytes2 = BitConverter.GetBytes(CType((hookLoc - &H77) - nodeDumpMemory.address, Int32))
        Array.Copy(bytes2, 0, bytes, bytjmp, bytes2.Length)
        WriteProcessMemory(_targetProcessHandle, nodeDumpMemory, bytes, TargetBufferSize, 0)

        If ReadUInt8(nodeDumpMemory) = &H50& Then
            'Insert the hook
            bytes = {&HE9, 0, 0, 0, 0}
            bytes2 = BitConverter.GetBytes(CType(nodeDumpMemory - hookLoc - 5, Int32))
            Array.Copy(bytes2, 0, bytes, 1, bytes2.Length)
            WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
        Else
            nodeDumpMemory.Dispose()
            nodeDumpMemory = Nothing
            Throw New DSProcessAttachException("NodeDump code injection appears to have failed.")
        End If
    End Sub
    Private Sub TearDownNodeDumpHook()
        'Restore original instruction
        Dim bytes() As Byte = {&H66, &HF, &HD6, &H46, &H14}
        Dim hookLoc As Integer = dsBase + &H7E637E
        WriteProcessMemory(_targetProcessHandle, hookLoc, bytes, bytes.Length, 0)
        If nodeDumpMemory IsNot Nothing Then
            nodeDumpMemory.Dispose()
            nodeDumpMemory = Nothing
        End If
    End Sub

    Public Property MaxNodes As Integer
        Get
            Dim tmpptr As Integer
            tmpptr = ReadInt32(dsBase + &HF7F838)
            tmpptr = ReadInt32(tmpptr + &H38)
            If Not tmpptr = 0 Then
                Dim value = ReadInt32(tmpptr + &H70)
                If value > 64 Then Return -1
                Return value
            Else
                Return -1
            End If
        End Get
        Set(value As Integer)
            Dim tmpptr As Integer
            tmpptr = ReadInt32(dsBase + &HF7F838)
            tmpptr = ReadInt32(tmpptr + &H38)
            WriteInt32(tmpptr + &H70, value)
        End Set
    End Property

    Public ReadOnly Property NodeCount As Integer
        Get
            Return ReadInt32(dsBase + &HF62DD0) - 1
        End Get
    End Property

    Public ReadOnly Property SelfSteamId As String
        Get
            Return ReadSteamIdAscii(ReadInt32(dsBase + &HF7E204) + &HA00)
        End Get
    End Property

    Public Sub ConnectToSteamId(ByVal steamId As String)
        Dim data(70) As Byte
        data(0) = &H1
        Dim selfSteamName As String = ReadSteamName(ReadInt32(dsBase + &HF62DD4) + &H30)
        Dim selfSteamNameBytes() As Byte = Encoding.Unicode.GetBytes(selfSteamName)
        Array.Copy(selfSteamNameBytes, 0, data, 1, selfSteamNameBytes.Length)

        Try
            sendP2PPacket(steamId, data)
        Catch ex As Exception
            Throw New DSConnectException(ex.Message)
        End Try
    End Sub

    Public Sub DisconnectSteamId(ByVal steamId As String)
        Dim data(70) As Byte
        data(0) = &H2
        Dim selfSteamName As String = ReadSteamName(ReadInt32(dsBase + &HF62DD4) + &H30)
        Dim selfSteamNameBytes() As Byte = Encoding.Unicode.GetBytes(selfSteamName)
        Array.Copy(selfSteamNameBytes, 0, data, 1, selfSteamNameBytes.Length)

        Try
            sendP2PPacket(steamId, data)
        Catch ex As Exception
            Throw New DSConnectException(ex.Message)
        End Try
    End Sub

    Private Sub sendP2PPacket(targetSteamId As String, data As Byte())
        If targetSteamId.Length <> 16 Then
            Throw New ApplicationException("Invalid Steam ID: " & targetSteamId)
        End If
        'Note to self, find a better way of confirming that this is the correct address for this function.
        'If nothing else, just compare the bytes at the address.
        Dim steamApiNetworking As IntPtr = steamApiBase + &H2F70

        'Extremely weak check to be sure we're at the right spot
        If Not ReadUInt8(steamApiNetworking) = &HA1& Then
            Throw New ApplicationException("Unexpected byte at steam_api.dll|Networking")
        End If

        'If 0 then allocate memory, otherwise use previously allocated memory.
        If connectMemory Is Nothing Then
            connectMemory = New AllocatedMemory(_targetProcessHandle, 1024)
        End If

        'Set up data packet
        If data.Length > 512 Then Throw New ApplicationException("Data packet to big")
        Dim dataPacketPtr As IntPtr = connectMemory.address + &H100
        WriteProcessMemory(_targetProcessHandle, dataPacketPtr, data, data.Length, 0)

        'Set up code
        'Note to self, as usual, comment in the actual ASM before it gets lost.
        Dim code() As Byte = {
            &HE8, &H0, &H0, &H0, &H0, &H8B, &H10, &H8B, &H12, &H6A, &H1, &H6A, &H2, &HBF, &H0, &H0,
            &H0, &H0, &H57, &HB9, &H0, &H0, &H0, &H0, &H51, &H68, &H0, &H0, &H0, &H0, &H68, &H0,
            &H0, &H0, &H0, &H8B, &HC8, &HFF, &HD2, &HC3}

        'Set steam_api.SteamNetworking call
        Dim steamApiNetworkingRelative() As Byte = BitConverter.GetBytes(CType(steamApiNetworking - connectMemory.address - 5, Int32))
        Array.Copy(steamApiNetworkingRelative, 0, code, &H1, steamApiNetworkingRelative.Length)

        Dim dataPacketLen() As Byte = BitConverter.GetBytes(CType(data.Length, UInt32))
        Array.Copy(dataPacketLen, 0, code, &HE, dataPacketLen.Length)

        Dim dataPacketPtrBytes() As Byte = BitConverter.GetBytes(CType(dataPacketPtr, UInt32))
        Array.Copy(dataPacketPtrBytes, 0, code, &H14, dataPacketPtrBytes.Length)

        Dim steamIdLeft() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Left(targetSteamId, 8), 16))
        Array.Copy(steamIdLeft, 0, code, &H1A, steamIdLeft.Length)

        Dim steamIdRight() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Right(targetSteamId, 8), 16))
        Array.Copy(steamIdRight, 0, code, &H1F, steamIdRight.Length)

        WriteProcessMemory(_targetProcessHandle, connectMemory, code, code.Length, 0)

        CreateRemoteThread(_targetProcessHandle, 0, 0, connectMemory, 0, 0, 0)
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
            node.SteamId = ReadSteamIdUnicode(steamData2 + &H30)
            node.CharacterName = ReadSteamName(steamData1 + &H30)
            If node.SteamId = SelfSteamId Then
                SelfNode.SteamId = node.SteamId
                SelfNode.CharacterName = node.CharacterName
            Else
                basicNodeInfo(node.SteamId) = node.CharacterName
            End If
            steamNodesPtr = ReadInt32(steamNodesPtr)
        Next


        For Each key As String In New List(Of String)(ConnectedNodes.Keys)
            If Not basicNodeInfo.ContainsKey(key) Then
                ConnectedNodes.Remove(key)
            End If
        Next

        Dim nodeSteamId As String
        Dim nodePtr As Integer
        nodePtr = nodeDumpMemory.address + &H200
        While True
            nodeSteamId = ReadSteamIdAscii(nodePtr)
            WriteInt32(nodePtr, 0)
            If nodeSteamId Is Nothing Then
                Exit While
            End If
            If basicNodeInfo.ContainsKey(nodeSteamId) Then
                Dim node As DSNode
                If ConnectedNodes.ContainsKey(nodeSteamId) Then
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

        Dim heroPtr As Integer = ReadInt32(dsBase + &HF78700)
        heroPtr = ReadInt32(heroPtr + &H8)
        SelfNode.Indictments = ReadInt32(heroPtr + &HEC)
        SelfNode.Covenant = ReadInt8(heroPtr + &H10B)
    End Sub
    Public ReadOnly Property HasDarkmoonRingEquiped As Boolean
        Get
            Dim heroPtr As Integer = ReadInt32(dsBase + &HF78700)
            heroPtr = ReadInt32(heroPtr + &H8)
            Return ReadInt32(heroPtr + &H280) = 102 Or ReadInt32(heroPtr + &H284) = 102
        End Get
    End Property
    Public ReadOnly Property HasCatCovenantRingEquiped As Boolean
        Get
            Dim heroPtr As Integer = ReadInt32(dsBase + &HF78700)
            heroPtr = ReadInt32(heroPtr + &H8)
            Return ReadInt32(heroPtr + &H280) = 103 Or ReadInt32(heroPtr + &H284) = 103
        End Get
    End Property

    Public Function ReadInt8(ByVal addr As IntPtr) As SByte
        Dim _rtnBytes(0) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, _rtnBytes, 1, vbNull)
        If _rtnBytes(0) < 128 Then Return _rtnBytes(0)
        Return _rtnBytes(0) - 256
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
    Private Function ReadSteamIdAscii(addr As IntPtr) As String
        Dim bytes(15) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, bytes, 16, vbNull)
        If bytes(0) = 0 Then Return Nothing
        Return Encoding.ASCII.GetChars(bytes)
    End Function
    Private Function ReadSteamIdUnicode(addr As IntPtr) As String
        Dim bytes(31) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, bytes, 32, vbNull)
        Return Encoding.Unicode.GetChars(bytes)
    End Function
    Private Function ReadSteamName(ByVal addr As UInteger) As String
        Dim str As New StringBuilder()
        Dim bytes(63) As Byte
        ReadProcessMemory(_targetProcessHandle, addr, bytes, 64, vbNull)

        For i = 0 To 63 Step 2
            Dim chr As UInt16 = bytes(i) + 256 * bytes(i + 1)
            If chr = 0 Then Exit For
            str.Append(Convert.ToChar(chr))
        Next
        Return str.ToString()
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
