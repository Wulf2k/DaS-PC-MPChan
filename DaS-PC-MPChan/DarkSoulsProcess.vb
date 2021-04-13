Imports System.Text
Imports System.Threading
Imports System.Runtime.InteropServices

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
    Private _disposed As Boolean = False
    Sub New(targetProcessHandle As IntPtr, size As Integer)
        _process = targetProcessHandle
        _address = VirtualAllocEx(_process, 0, size, MEM_COMMIT, PAGE_READWRITE)
        If _address = 0 Then
            Throw New ApplicationException("Failed to allocate Memory. Error Code: " & Err.LastDllError.ToString())
        End If
        Dim oldProtectionOut As UInteger
        VirtualProtectEx(_process, _address, size, PAGE_EXECUTE_READWRITE, oldProtectionOut)
    End Sub
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        If Not _disposed Then
            If _address <> 0 Then
                VirtualFreeEx(_process, _address, 0, MEM_RELEASE)
                _address = 0
            End If
            _disposed = True
        End If
    End Sub
    Protected Overrides Sub Finalize()
        Dispose()
    End Sub
    Public Sub Leak()
        _disposed = True
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


Public Class JmpHook
    Implements IDisposable
    Private _process As IntPtr
    Private _hookLocation As IntPtr
    Private _jmpTarget As Intptr
    Private _oldInstructions As Byte()
    Private _isActive As Boolean

    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesWritten As Integer) As Boolean

    Sub New(targetProcessHandle As IntPtr, hookLocation As IntPtr, jmpTarget As IntPtr, Optional hookInstructionSize As UInt32 = 5)
        Debug.Assert(hookInstructionSize >= 5)
        _process = targetProcessHandle
        _hookLocation = hookLocation
        _jmpTarget = jmpTarget
        _oldInstructions = New Byte(hookInstructionSize-1) {}
        ReadProcessMemory(_process, _hookLocation, _oldInstructions, hookInstructionSize, vbNull)
        _isActive = False
    End Sub
    Public Sub Activate()
        If Not _isActive Then
            Dim jmpOffset As Int32 = _jmpTarget - _hookLocation - 5
            Dim jmpInstruction() As Byte = {&HE9}
            jmpInstruction = jmpInstruction.Concat(BitConverter.GetBytes(jmpOffset)).ToArray()

            Dim nopInstruction As Byte = &H90
            Dim replaceWith() As Byte = jmpInstruction.Concat(Enumerable.Repeat(nopInstruction, _oldInstructions.Length - 5)).ToArray()
            WriteProcessMemory(_process, _hookLocation, replaceWith, replaceWith.Length, vbNull)
            _isActive = True
        End If
    End Sub
    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        If _isActive And _hookLocation <> 0 Then
             WriteProcessMemory(_process, _hookLocation, _oldInstructions, _oldInstructions.Length, vbNull)
            _hookLocation = 0
        End If
    End Sub
    Protected Overrides Sub Finalize()
        Dispose()
    End Sub
    Public Shared Sub Permanent(targetProcessHandle As IntPtr, hookLocation As IntPtr, jmpTarget As IntPtr, Optional hookInstructionSize As UInt32 = 5)
        Dim hook As New JmpHook(targetProcessHandle, hookLocation, jmpTarget, hookInstructionSize)
        hook.Activate()
        'Disable cleanup
        hook._hookLocation = 0
    End Sub
    Public Function PatchCode(code() As Byte) As Byte()
        Dim patchedCode = code.Concat(_oldInstructions)

        ' This is JmpTargetAdress - JmpSourcePositon (address after the jmp)
        Dim returnAddress = BitConverter.GetBytes(
            CType((_hookLocation + _oldInstructions.Length) -
                  (_jmpTarget + code.Length + _oldInstructions.Length + 5),
                Int32))
        patchedCode = patchedCode.Concat({&HE9}) 'JMP
        patchedCode = patchedCode.Concat(returnAddress)
        Return patchedCode.ToArray()
    End Function
End Class



Public Class DarkSoulsProcess
    Implements IDisposable
    'Process/Memory Manipulation
    Private Declare Function OpenProcess Lib "kernel32.dll" (ByVal dwDesiredAcess As UInt32, ByVal bInheritHandle As Boolean, ByVal dwProcessId As Int32) As IntPtr
    Private Declare Function ReadProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByRef lpNumberOfBytesRead As Integer) As Boolean
    Private Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpBaseAddress As IntPtr, ByVal lpBuffer() As Byte, ByVal iSize As Integer, ByVal lpNumberOfBytesWritten As Integer) As Boolean
    Private Declare Function CloseHandle Lib "kernel32.dll" (ByVal hObject As IntPtr) As Boolean
    Private Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As IntPtr, ByVal lpThreadAttributes As IntPtr, ByVal dwStackSize As Integer, ByVal lpStartAddress As IntPtr, ByVal lpParameter As IntPtr, ByVal dwCreationFlags As Integer, ByRef lpThreadId As IntPtr) As Integer
    Private Declare Function CreatePipe Lib "kernel32" (ByRef phReadPipe As IntPtr, ByRef phWritePipe As IntPtr, ByVal lpPipeAttributes As IntPtr, ByVal nSize As Int32) As Boolean
    Private Declare Function DuplicateHandle Lib "kernel32" (ByVal sourceProcessHandle As IntPtr, ByVal sourceHandle As IntPtr,
                                                             ByVal targetProcessHandle As IntPtr, ByRef targetHandle As IntPtr,
                                                             ByVal desiredAccess As Int32, ByVal inheritHandle As Boolean, options As Int32) As Boolean
    Private Declare Function ReadFile Lib "kernel32" (ByVal fileHandle As IntPtr, ByVal buffer() As Byte, ByVal bytesToRead As Int32,  ByRef lpNumberOfBytesRead As Integer, ByVal overlapped As IntPtr) As Boolean
    Private Declare Function GetCurrentThread Lib "kernel32" () As IntPtr
    Private Declare Function CancelSynchronousIo Lib "kernel32" (ByVal hThread As IntPtr) As Boolean

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
    Private namedNodeHook As JmpHook
    Private nodeDumpMemory As AllocatedMemory
    Private nodeDumpHook As JmpHook
    Private lobbyDumpMemory As AllocatedMemory
    Private lobbyDumpHook As JmpHook
    Private connectMemory As AllocatedMemory

    Private blocklistSendHook As JmpHook
    Private blocklistRecvHook As JmpHook
    Private blocklistSendDetour As AllocatedMemory
    Private blocklistRecvDetour As AllocatedMemory
    Private blocklistInMemory As AllocatedMemory
    Private blocklistInMemorySize As Int32
    Private whitelistInMemory As AllocatedMemory
    Private whitelistInMemorySize As Int32
    Public listTypeInMemory As AllocatedMemory

    Public type18TmpStorageSteamId As AllocatedMemory
    Public const onHitListTmpStorageSteamIdSize As UInt32 = 200
    Public onHitListTmpStorageSteamId As AllocatedMemory
    Private badSpeffectList As AllocatedMemory
    Private badSpeffectListSize As Int32
    Private ReceiveOnHitPacketDetour As AllocatedMemory
    Private ReceiveOnHitPacketHook As JmpHook

    Private debugLogMemory As AllocatedMemory
    Private debugLogThread As Thread
    Private debugLogThreadHandle As IntPtr
    Private debugLogPipeHandle As IntPtr
    Public debugLog As New List(Of DebugLogEntry)

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
        If Not HasWatchdog Then
            InstallNamecrashFix()
        End If
        While True
            Try
                Dim success = Inject_P2PPacket()
                If success Then
                    Exit While
                End If
            Catch ex As Exception
                'hard failure. no recovering the blocking feature now
                'don't wait for a return here otherwise the auto-attach timer will call this function again
                Dim thread as New Thread(
                  Sub() 
                    MsgBox(ex.Message, MsgBoxStyle.Critical)
                  End Sub
                )
                thread.Start()
                Throw New DSProcessAttachException("Unable to monitor packets")
            End Try
            'soft failure. Just wait a bit then retry
            Thread.Sleep(200)
        End While
        Dim success2 = Inject_ReceiveOnHitPacket()
        If not success2 Then
            'error injecting the hook
            Dim thread2 as New Thread(
              Sub() 
                MsgBox("Unable to perform Inject_ReceiveOnHitPacket. Please inform developer", MsgBoxStyle.Critical)
              End Sub
            )
            thread2.Start()
            Throw New DSProcessAttachException("Unable to check packets")
        End If
        SetupNodeDumpHook()
        SetupLobbyDumpHook()
        Sync_MemoryBlockList(MainWindow.dgvBlockedNodes.Rows)
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
            TearDownDebugLog()
            TearDownLobbyDumpHook()
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

            Try
                _targetProcess = proc
                _targetProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, False, _targetProcess.Id)
            If _targetProcessHandle = 0 Then
                Throw New DSProcessAttachException("OpenProcess() failed. Check Permissions")
            End If
            Catch ex As Exception

            End Try
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
        If ReadUInt8(dsBase + &HAFC39F) = &H74& Then
            WriteBytes(dsBase + &HAFC39F, {&HEB})
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
            Dim cmpLoc As IntPtr = dsBase + &HBA256C

            Dim hookLoc As IntPtr = dsBase + &H15A550

            If value Then
                'Changes instruction doing the compare rather than changing the value it compares against
                WriteBytes(cmpLoc, {&H1})

                'ASM in Resources\ASM-NamedNodes.txt
                Dim code() As Byte = {&H8B, &H44, &H24, &H10, &H50, &H8B, &HC3, &H8B, &HD9, &H81, &HE3, &H0, &HFB, &H0, &H0, &H81,
                            &HFB, &H0, &HFB, &H0, &H0, &H8B, &HD8, &HF, &H84, &H5, &H0, &H0, &H0, &HE9, &H46, &H0,
                            &H0, &H0, &H8B, &H5B, &HD0, &H83, &HFB, &H0, &HF, &H84, &H14, &H0, &H0, &H0, &H8B, &H5B,
                            &H14, &H83, &HFB, &H0, &HF, &H84, &H8, &H0, &H0, &H0, &H83, &HC3, &H30, &HE9, &H7, &H0,
                            &H0, &H0, &H8B, &HD8, &HE9, &H1F, &H0, &H0, &H0, &H50, &HB8, &H0, &H0, &H0, &H0, &H83,
                            &HF8, &H20, &HF, &H84, &H9, &H0, &H0, &H0, &H8A, &H13, &H88, &H17, &H40, &H43, &H47, &HEB,
                            &HEE, &H83, &HEB, &H20, &H83, &HEF, &H20, &H58, &H58, &H56, &HE9, &H0, &H0, &H0, &H0}

                namedNodeMemory = New AllocatedMemory(_targetProcessHandle, code.Length)

                'Location of jmp instruction in bytearray
                Dim jmpOffset As Integer = &H6A

                'Modify final JMP above to return to instruction after original hook
                Dim returnAddress = BitConverter.GetBytes(CType((hookLoc + 5) - (namedNodeMemory.address + jmpOffset) - 5, Int32))

                Array.Copy(returnAddress, 0, code, jmpOffset + 1, returnAddress.Length)
                WriteProcessMemory(_targetProcessHandle, namedNodeMemory, code, code.Length, 0)

                namedNodeHook = New JmpHook(_targetProcessHandle, hookLoc, namedNodeMemory)
                namedNodeHook.Activate()
            Else
                'Disable Node Drawing
                WriteBytes(cmpLoc, {&H0})

                If namedNodeHook IsNot Nothing Then
                    namedNodeHook.Dispose()
                    namedNodeHook = Nothing
                End If
                If namedNodeMemory IsNot Nothing Then
                    namedNodeMemory.Dispose()
                    namedNodeMemory = Nothing
                End If
            End If
     
        End Set
    End Property

    Private Sub InstallNamecrashFix()
        Dim originalContent() As Byte = {&H66, &H8B, &H10, &H83, &HC0, &H02, &H66, &H85, &HD2}
        Dim processContent = ReadBytes(dsBase + &H058A46, originalContent.Length)
        If Not processContent.SequenceEqual(originalContent) Then
            'The memory is not as expected. We have probably already installed the hooks
            Return
        End If

        'The machinecode and all the mechanics behind this are courtesy of eur0pa
        Dim code() As Byte = My.Resources.namecrash.Clone()
        
        'Offsets which we want to point to our buffer
        Dim bufferLocations = New Dictionary(Of Integer, Integer) From {
            {1, 204},
            {2, 243},
            {3, 264}
        }
        'This is (hook offset in DS, target offset in code)
        Dim hookLocations = New Dictionary(Of Integer, Integer) From {
            {&H058A46, 0},
            {&H82AA00, 26},
            {&H9B2B70, 55},
            {&H7EFC60, 84},
            {&H39C4A3, 114},
            {&H058A62, 145},
            {&H18CACF, 176}
        }
        'This is (return offset in DS, jmp offset in code)
        Dim returnLocations As New Dictionary(Of Integer, Integer) From {
            {&H058A51, 20},
            {&H82AA12, 49},
            {&H9B2B82, 78},
            {&H7EFC71, 108},
            {&H39C4B2, 139},
            {&H058A71, 170},
            {&H18BD40, 250},
            {&H18CB21, 275},
            {&H18CAD6, 255}
        }


        Dim memory As New AllocatedMemory(_targetProcessHandle, code.Length)
        Dim jmpInstruction() As Byte = {&HE9}
        For Each returnLocation In returnLocations
            If returnLocation.Key = &H18BD40 Then
                jmpInstruction = {&HE8}
            Else
                jmpInstruction = {&HE9}
            End If
            Dim jmpOffset As Int32 = (dsBase + returnLocation.Key) - (memory.address + returnLocation.Value) - 5
            Dim instruction() As Byte = jmpInstruction.Concat(BitConverter.GetBytes(jmpOffset)).ToArray()
            Array.Copy(instruction, 0, code, returnLocation.Value, instruction.Length)
        Next
        
        For Each bufferLocation In bufferLocations
            'Could make another buffer but might as well use the spare space in the existing one
            Dim bufOffset As Int32 = (memory.address + code.Length) + 20
            Dim instruction() As Byte = BitConverter.GetBytes(bufOffset).ToArray()
            Array.Copy(instruction, 0, code, bufferLocation.Value, instruction.Length)
        Next
        
        WriteProcessMemory(_targetProcessHandle, memory, code, code.Length, 0)

        'Install the fix permanently
        memory.Leak()
        For Each hookLocation In hookLocations
            JmpHook.Permanent(_targetProcessHandle, (dsBase + hookLocation.Key), (memory.address + hookLocation.Value))
        Next
    End Sub
    Private Sub SetupDebugLog()
        Dim localWritePipe As New IntPtr()
        If Not CreatePipe(debugLogPipeHandle, localWritePipe, 0, 0) Then
            Throw New DSProcessAttachException("Failed to create logging Pipe")
        End If
        Dim myProcess = Process.GetCurrentProcess()
        Dim foreignWritePipe As New IntPtr()
        If Not DuplicateHandle(myProcess.Handle, localWritePipe, _targetProcessHandle, foreignWritePipe, 0, True, 3) Then
            Throw New DSProcessAttachException("Failed to duplicate logging Handle")
        End If

        Dim code() As Byte = {
            &H00, &H00, &H00, &H00, &H8B, &H44, &H24, &H04, &H8B, &H54, &H24, &H08, &H55, &HE8, &H00, &H00,
            &H00, &H00, &H5D, &H81, &HED, &H12, &H00, &H00, &H00, &H52, &H50, &H89, &HE2, &HB8, &H04, &H00,
            &H00, &H00, &HE8, &H1B, &H00, &H00, &H00, &H58, &H5A, &HB8, &H01, &H00, &H00, &H00, &H66, &H83,
            &H7C, &H42, &HFE, &H00, &H74, &H03, &H40, &HEB, &HF5, &H01, &HC0, &HE8, &H02, &H00, &H00, &H00,
            &H5D, &HC3, &H50, &H52, &H83, &HEC, &H04, &H89, &HE1, &H6A, &H00, &H51, &H50, &H52, &H8B, &H85,
            &H00, &H00, &H00, &H00, &H50, &H8B, &H85, &H63, &H00, &H00, &H00, &HFF, &H10, &H83, &HC4, &H04,
            &H5A, &H58, &HC3, &H60, &HC2, &H0C, &H01, &H90
        }
        
        Dim pipeBytes() As Byte = BitConverter.GetBytes(CType(foreignWritePipe, UInt32))
        Array.Copy(pipeBytes, 0, code, 0, pipeBytes.Length)

        debugLogMemory = New AllocatedMemory(_targetProcessHandle, code.Length)
        WriteProcessMemory(_targetProcessHandle, debugLogMemory, code, code.Length, 0)

        Dim funcAddressBytes() As Byte = BitConverter.GetBytes(CType(debugLogMemory.address + 4, UInt32))
        Dim loggingFuncPtrLoc As IntPtr = &H136304C
        WriteProcessMemory(_targetProcessHandle, loggingFuncPtrLoc, funcAddressBytes, funcAddressBytes.Length, 0)

        
        debugLogThread = New Thread(AddressOf DebugLogThreadMain)
        debugLogThread.IsBackground = True
        debugLogThread.Start()
    End Sub
    Private Sub TearDownDebugLog()
        If IsNothing(debugLogMemory) Then Return
        Dim loggingFuncPtrLoc As IntPtr = &H136304C
        Dim zero() As Byte = {0, 0, 0 ,0}
        WriteProcessMemory(_targetProcessHandle, loggingFuncPtrLoc, zero, zero.Length, 0)
        debugLogMemory.Dispose()
        debugLogMemory = Nothing

        If Not IsNothing(debugLogThreadHandle) Then
            CancelSynchronousIo(debugLogThreadHandle)
            debugLogThreadHandle = Nothing
        End If
        If Not IsNothing(debugLogThread) Then
            debugLogThread.Join()
            debugLogThread = Nothing
        End If
    End Sub
    Public Property enableDebugLog As Boolean
        Get
            Return Not IsNothing(debugLogMemory)
        End Get
        Set(value As Boolean)
            If value = enableDebugLog Then Return
            If value Then
                SetupDebugLog()
            Else
                TearDownDebugLog()
            End If
            End Set
        End Property
    Private Sub DebugLogThreadMain()
        Dim severityMap As New Dictionary(Of Integer, String)
        severityMap.Add(200000001,  "DBG")
        severityMap.Add(1000000001, "INF")
        severityMap.Add(1300000001, "WRN")
        severityMap.Add(1400000001, "ERR")

        Dim buffer = New Byte() {}
        Dim readBuffer = New Byte(8000) {}
        Dim readBytes As New Int32
        SyncLock debugLog
            AddDebugLogLine("INF", "###################################################")
            AddDebugLogLine("INF", "Logger connected")
            AddDebugLogLine("INF", "###################################################")
        End SyncLock
        While Not IsNothing(debugLogMemory)
            Dim selfHandle = GetCurrentThread()
            Dim selfHandleCopy As IntPtr
            DuplicateHandle(Process.GetCurrentProcess.Handle, selfHandle,
                            Process.GetCurrentProcess.Handle, selfHandleCopy, 0, True, 3)
            debugLogThreadHandle = selfHandleCopy
            If Not ReadFile(debugLogPipeHandle, readBuffer, 8000, readBytes, 0) then
                Exit While
            Else
                Dim bufferPos = 0
                buffer = buffer.Concat(readBuffer.Take(readBytes)).ToArray()
                SyncLock debugLog
                    While True
                        Dim strEnd = -1
                        For p = bufferPos + 4 To buffer.Length-2 Step 2
                            If buffer(p) = 0 AndAlso buffer(p + 1) = 0 Then
                                strEnd = p
                                Exit For
                            End If
                        Next
                        If strEnd = -1
                            buffer = buffer.Skip(bufferPos).ToArray()
                            Exit While
                        End If
                    
                        Dim severity As UInt32 = BitConverter.ToUInt32(buffer, bufferPos)
                        Dim decoded As String = Encoding.Unicode.GetChars(buffer, bufferPos + 4, strEnd - bufferPos - 4)
                        bufferPos = strEnd + 2
                        AddDebugLogLine(severityMap(severity), decoded)
                    End While
                End SyncLock
            End If
        End While
        SyncLock debugLog
            AddDebugLogLine("INF", "###################################################")
            AddDebugLogLine("INF", "Logger disconnected")
            AddDebugLogLine("INF", "###################################################")
        End SyncLock
    End Sub
    Private Sub AddDebugLogLine(severity As String, line As String)
        debugLog.Add(New DebugLogEntry(DateTime.Now, severity, line))
    End Sub
    Private Sub SetupLobbyDumpHook()
        'ASM in ASM\ASM-LobbyDump.txt
        Dim code() As Byte = {
            &H50, &H53, &H51, &H52, &H56, &HE8, &H00, &H00, &H00, &H00, &H5B, &H81, &HEB, &H0A, &H00, &H00,
            &H00, &H81, &HC3, &H00, &H00, &H00, &H00, &H81, &HC3, &H00, &H02, &H00, &H00, &H8B, &H70, &H08,
            &H8B, &H48, &H04, &H89, &HF0, &H29, &HC8, &H3D, &H40, &H06, &H00, &H00, &H75, &H18, &H39, &HCE,
            &H76, &H14, &H8B, &H41, &H18, &H8B, &H51, &H1C, &H83, &HC3, &H08, &H83, &HC1, &H20, &H89, &H43,
            &HF8, &H89, &H53, &HFC, &HEB, &HE8, &H5E, &H5A, &H59, &H5B, &H58
        }


        Dim bufferSize = &H200 + 8*50
        Dim hookLoc As IntPtr = dsBase + &H323A0D
        lobbyDumpMemory = New AllocatedMemory(_targetProcessHandle, bufferSize)
        lobbyDumpHook = New JmpHook(_targetProcessHandle, hookLoc, lobbyDumpMemory, 7)
        code = lobbyDumpHook.PatchCode(code)
        WriteProcessMemory(_targetProcessHandle, lobbyDumpMemory, code, bufferSize, 0)
        lobbyDumpHook.Activate()
    End Sub
    Private Sub TearDownLobbyDumpHook()
        If lobbyDumpHook IsNot Nothing THen
            lobbyDumpHook.Dispose()
            lobbyDumpHook = Nothing
        End If
        If lobbyDumpMemory IsNot Nothing Then
            lobbyDumpMemory.Dispose()
            lobbyDumpMemory = Nothing
        End If
    End Sub

    Private Sub SetupNodeDumpHook()
        'ASM in ASM\ASM-NodeDump.txt
        Dim code() As Byte = {&H50, &H53, &H51, &H52, &H56, &H57, &HBF, &H0, &H0, &H0, &H0, &HB8, &H0, &H0, &H0, &H0,
                    &HBB, &H0, &H0, &H0, &H0, &HB9, &H0, &H0, &H0, &H0, &HBA, &H0, &H0, &H0, &H0, &H8A,
                    &H1F, &H80, &HFB, &H0, &HF, &H84, &H30, &H0, &H0, &H0, &H8A, &H6, &H8A, &H1F, &H46, &H47,
                    &H41, &H38, &HD8, &HF, &H85, &H13, &H0, &H0, &H0, &H83, &HF9, &H11, &H75, &HEC, &H29, &HCE,
                    &H29, &HCF, &HB9, &H0, &H0, &H0, &H0, &HE9, &HE, &H0, &H0, &H0, &H29, &HCE, &H29, &HCF,
                    &HB9, &H0, &H0, &H0, &H0, &H83, &HC7, &H30, &HEB, &HC5, &H8A, &H1E, &H88, &H1F, &H46, &H47,
                    &H41, &H83, &HF9, &H30, &HF, &H84, &H2, &H0, &H0, &H0, &HEB, &HEE, &H5F, &H5E, &H5A, &H59,
                    &H5B, &H58}

        'Note to self, buffer is overly large.  Trim down some day.
        Dim bufferSize = 4096
        Dim hookLoc As IntPtr = dsBase + &H7E637E
        nodeDumpMemory = New AllocatedMemory(_targetProcessHandle, bufferSize)
        nodeDumpHook = New JmpHook(_targetProcessHandle, hookLoc, nodeDumpMemory, 5)

        'Adjust EDI
        Dim dumpStorageAddr() As Byte = BitConverter.GetBytes(CType(nodeDumpMemory.address + &H200, UInt32))
        Array.Copy(dumpStorageAddr, 0, code, &H7, dumpStorageAddr.Length)

        code = nodeDumpHook.PatchCode(code)
        WriteProcessMemory(_targetProcessHandle, nodeDumpMemory, code, bufferSize, 0)
        nodeDumpHook.Activate()
    End Sub
    Private Sub TearDownNodeDumpHook()
        If nodeDumpHook IsNot Nothing THen
            nodeDumpHook.Dispose()
            nodeDumpHook = Nothing
        End If
        If nodeDumpMemory IsNot Nothing Then
            nodeDumpMemory.Dispose()
            nodeDumpMemory = Nothing
        End If
    End Sub

    Public Property MaxNodes As Integer
        Get
            Dim tmpptr As IntPtr
            tmpptr = ReadIntPtr(dsBase + &HF7F838)
            tmpptr = ReadIntPtr(tmpptr + &H38)
            If Not tmpptr = 0 Then
                Dim value = ReadInt32(tmpptr + &H70)
                If value > 64 Then Return -1
                Return value
            Else
                Return -1
            End If
        End Get
        Set(value As Integer)
            Dim tmpptr As IntPtr
            tmpptr = ReadIntPtr(dsBase + &HF7F838)
            tmpptr = ReadIntPtr(tmpptr + &H38)
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
            Return ReadSteamIdAscii(ReadIntPtr(dsBase + &HF7E204) + &HA00)
        End Get
    End Property
    Public Property SelfSteamName As String
        Get
            Dim byt() As Byte
            byt = ReadBytes(ReadIntPtr(dsBase + &HF62DD4) + &H30, &H40)
            Return Encoding.Unicode.GetString(byt)
        End Get
        Set(value As String)
            Dim byt(&H22) As Byte

            byt = Encoding.Unicode.GetBytes(value)
            WriteBytes(ReadIntPtr(dsBase + &HF62DD4) + &H30, byt)
        End Set
    End Property

    Public Sub ConnectToSteamId(ByVal steamId As String)
        Dim data(70) As Byte
        data(0) = &H1
        Dim selfSteamName As String = ReadSteamName(ReadInt32(dsBase + &HF62DD4) + &H30)
        Dim selfSteamNameBytes() As Byte = Encoding.Unicode.GetBytes(selfSteamName)
        Array.Copy(selfSteamNameBytes, 0, data, 1, selfSteamNameBytes.Length)

        sendP2PPacket(steamId, data)
    End Sub

    Public Sub DisconnectSteamId(ByVal steamId As String)
        Dim data(70) As Byte
        data(0) = &H2
        Dim selfSteamName As String = ReadSteamName(ReadInt32(dsBase + &HF62DD4) + &H30)
        Dim selfSteamNameBytes() As Byte = Encoding.Unicode.GetBytes(selfSteamName)
        Array.Copy(selfSteamNameBytes, 0, data, 1, selfSteamNameBytes.Length)

        sendP2PPacket(steamId, data)
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
        'Dim code() As Byte = {
        '    &HE8, &H0, &H0, &H0, &H0, &H8B, &H10, &H8B, &H12, &H6A, &H1, &H6A, &H2, &HBF, &H0, &H0,
        '    &H0, &H0, &H57, &HB9, &H0, &H0, &H0, &H0, &H51, &H68, &H0, &H0, &H0, &H0, &H68, &H0,
        '    &H0, &H0, &H0, &H8B, &HC8, &HFF, &HD2, &HC3}

        'Set steam_api.SteamNetworking call
        'Dim steamApiNetworkingRelative() As Byte = BitConverter.GetBytes(CType(steamApiNetworking - connectMemory.address - 5, Int32))
        'Array.Copy(steamApiNetworkingRelative, 0, code, &H1, steamApiNetworkingRelative.Length)

        'Dim dataPacketLen() As Byte = BitConverter.GetBytes(CType(data.Length, UInt32))
        'Array.Copy(dataPacketLen, 0, code, &HE, dataPacketLen.Length)

        'Dim dataPacketPtrBytes() As Byte = BitConverter.GetBytes(CType(dataPacketPtr, UInt32))
        'Array.Copy(dataPacketPtrBytes, 0, code, &H14, dataPacketPtrBytes.Length)

        'Dim steamIdLeft() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Left(targetSteamId, 8), 16))
        'Array.Copy(steamIdLeft, 0, code, &H1A, steamIdLeft.Length)

        'Dim steamIdRight() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Right(targetSteamId, 8), 16))
        'Array.Copy(steamIdRight, 0, code, &H1F, steamIdRight.Length)


        'Attempt at compiling ASM through new method
        Dim a As New ASM
        a.pos = CInt(connectMemory.address)

        a.AddVar("connectmemory", connectMemory.address)
        a.AddVar("steamapinetworking", steamApiNetworking)
        a.AddVar("datapacketlen", Convert.ToUInt32(data.Length))
        a.AddVar("datapacketptr", connectMemory.address + &H100)
        a.AddVar("steamidleft", Convert.ToUInt32(Microsoft.VisualBasic.Left(targetSteamId, 8), 16))
        a.AddVar("steamidright", Convert.ToUInt32(Microsoft.VisualBasic.Right(targetSteamId, 8), 16))



        a.Asm("call steamapinetworking")
        a.Asm("mov edx, [eax]")
        a.Asm("mov edx, [edx]")
        a.Asm("push 1")
        a.Asm("push 2")
        a.Asm("mov edi, datapacketlen")
        a.Asm("push edi")
        a.Asm("mov ecx, datapacketptr")
        a.Asm("push ecx")
        a.Asm("push steamidleft")
        a.Asm("push steamidright")
        a.Asm("mov ecx, eax")
        a.Asm("call edx")
        a.Asm("ret")



        WriteProcessMemory(_targetProcessHandle, connectMemory, a.bytes, a.bytes.Length, 0)

        CreateRemoteThread(_targetProcessHandle, 0, 0, connectMemory, 0, 0, 0)
    End Sub

    Private Function Inject_ReceiveOnHitPacket() As Boolean
        Dim allocatedCodeSize = 512 'this should be plenty of space

        Dim recieveType18Packet_location As IntPtr = &H0D2F8B4
        Dim recieveType18Packet_abortread_location As IntPtr = &Hd2f87b

        'Extremely weak check to be sure we're at the right spot
        Dim checkBytes As Byte() = {&H39, &H1d, &H44, &Hd6, &H37, &H01}
        If Not ReadBytes(recieveType18Packet_location - 8, checkBytes.Length).SequenceEqual(checkBytes) Then
            Return False
        End If

        'Init in-memory location to save the steamid of the person we detect with this
        type18TmpStorageSteamId = New AllocatedMemory(_targetProcessHandle, 8)

        'Init in-memory location to save the list of people who last hit us
        'save their steamid and the speffects they applied, and have room for 200
        onHitListTmpStorageSteamId = New AllocatedMemory(_targetProcessHandle, (8+4+4)*onHitListTmpStorageSteamIdSize)

        'set up the list of forbidden SpEffects
        Dim badSpeffectList_VB() As UInt32 = {
            6, 7, 13, 33, 34, 71, 72, 73, 74, 80, 1520, 1530, 3220, 3240, 3250, 5210, 5211, 5212, 5213, 
            5214, 5295, 5296, 5221, 5222, 5280, 5290, 5220, 5281, 5291, 5282, 5292, 5283, 5293, 5284, 5294,
            -1} 'include terminator
        badSpeffectListSize = 4*badSpeffectList_VB.Length
        badSpeffectList = New AllocatedMemory(_targetProcessHandle, badSpeffectListSize)
        For i As Integer = 0 To badSpeffectList_VB.Length-1:
            WriteInt32(badSpeffectList.address + i*4, badSpeffectList_VB(i))
        Next

        'inject into the point in readparseType18NetMessage right before it would return the packet it recieved.
        'Check if the speffects in the packet are bad or not, and -1 them out and save the sender steamid if so
        'ASM in ASM\ASM-OnHitPacketCheck.txt
        Dim onHitListTmpStorageSteamId_asm_addr = BitConverter.GetBytes(CType(CType(onHitListTmpStorageSteamId, IntPtr), Integer))
        Dim badSpeffectList_asm_addr = BitConverter.GetBytes(CType(CType(badSpeffectList, IntPtr), Integer))
        Dim type18TmpStorageSteamId_asm_addr = BitConverter.GetBytes(CType(CType(type18TmpStorageSteamId, IntPtr), Integer))

        Dim readType18MessagedetourCode() As Byte = { 
        &H50,
        &H53,
        &H51,
        &H52,
        &HB9, onHitListTmpStorageSteamId_asm_addr(0), onHitListTmpStorageSteamId_asm_addr(1), onHitListTmpStorageSteamId_asm_addr(2), onHitListTmpStorageSteamId_asm_addr(3),
        &Hba, &H70, &H0c, &H00, &H00,
        &H8a, &H04, &H11,
        &H88, &H44, &H11, &H10,
        &H83, &Hfa, &H00,
        &H4a,
        &H7d, &Hf3,
        &H8b, &H06,
        &H8b, &H40, &H0c,
        &H8b, &H58, &H2c,
        &H89, &H59, &H04,
        &H8b, &H58, &H28,
        &H89, &H19,
        &HA1, &HF8, &H88, &H40, &H01,
        &H8B, &H1D, &H0C, &H89, &H40, &H01,
        &H89, &H41, &H08,
        &H89, &H59, &H0c,
        &HB9, badSpeffectList_asm_addr(0), badSpeffectList_asm_addr(1), badSpeffectList_asm_addr(2), badSpeffectList_asm_addr(3),
        &HBA, type18TmpStorageSteamId_asm_addr(0), type18TmpStorageSteamId_asm_addr(1), type18TmpStorageSteamId_asm_addr(2), type18TmpStorageSteamId_asm_addr(3),
        &H83, &H39, &HFF,
        &H74, &H3A,
        &H3B, &H01,
        &H74, &H09,
        &H3B, &H19,
        &H74, &H05,
        &H83, &HC1, &H04,
        &HEB, &HEE,
        &H8B, &H06,
        &H8B, &H40, &H0C,
        &H8B, &H58, &H2C,
        &H89, &H5A, &H04,
        &H8B, &H58, &H28,
        &H89, &H1A,
        &HC7, &H05, &HF8, &H88, &H40, &H01, &HFF, &HFF, &HFF, &HFF,
        &HC7, &H05, &H0C, &H89, &H40, &H01, &HFF, &HFF, &HFF, &HFF,
        &H5A,
        &H59,
        &H5B,
        &H58,
        &HE9}

        ReceiveOnHitPacketDetour = New AllocatedMemory(_targetProcessHandle, allocatedCodeSize)

        ReceiveOnHitPacketHook = New JmpHook(_targetProcessHandle, recieveType18Packet_location, ReceiveOnHitPacketDetour, 5)

        'set the jmp address for the abortread_return
        Dim abortread_returnAddress = BitConverter.GetBytes(CType((recieveType18Packet_abortread_location - (ReceiveOnHitPacketDetour.address + readType18MessagedetourCode.Length + 4)), Int32))
        readType18MessagedetourCode = readType18MessagedetourCode.Concat({abortread_returnAddress(0), abortread_returnAddress(1), abortread_returnAddress(2), abortread_returnAddress(3)}).ToArray()

        'add the normalread end
        readType18MessagedetourCode = readType18MessagedetourCode.Concat({&H5A, &H59, &H5B, &H58}).ToArray()
        readType18MessagedetourCode = ReceiveOnHitPacketHook.PatchCode(readType18MessagedetourCode)

        Debug.Assert(readType18MessagedetourCode.Count <= allocatedCodeSize, "You need more space for the readType18MessagedetourCode code")

        WriteProcessMemory(_targetProcessHandle, ReceiveOnHitPacketDetour, readType18MessagedetourCode, allocatedCodeSize, 0)

        ReceiveOnHitPacketHook.Activate()

        Return True
    End Function

    Private Function Inject_P2PPacket() As Boolean
        Dim allocatedCodeSize = 256 'this should be plenty of space

        Dim steamApiNetworking As IntPtr = steamApiBase + &H2F70

        'Extremely weak check to be sure we're at the right spot
        If Not ReadUInt8(steamApiNetworking) = &HA1& Then
            Return False
        End If

        'locate injection points
        Dim steamApiNetworking_addr As IntPtr = ReadInt32(steamApiNetworking + 1) 'get the dynamic address of the steamApiNetworking vtable or something
        Dim steamApiNetworking_ptrs As IntPtr = ReadInt32(steamApiNetworking_addr) 'follow 1st pointer to base of list of pointers to steamApiNetworking functions
        Dim sendP2PPacket_addrptr As IntPtr = ReadInt32(steamApiNetworking_ptrs)
        Dim sendP2PPacket_addr As IntPtr = ReadInt32(sendP2PPacket_addrptr + 0) 'get address of 1st function in the steamApiNetworking vtable
        Dim readP2PPacket_1 As IntPtr = ReadInt32(steamApiNetworking_ptrs + 4)
        Dim readP2PPacket_2 As IntPtr = ReadInt32(readP2PPacket_1 + 0)
        Dim readP2PPacket_3 As IntPtr = ReadInt32(readP2PPacket_2 + 8)

        'Check to make sure all of the steamApi is loaded before we inject (in cause we're playing with another mod that delays it's loading)
        If readP2PPacket_3 = 0 Then
            Return False
        End If

        'search through the readP2PPacket function to get the end
        'in order to get the last _instruction_ (which could be the original retn or a leftover jmp from us in the past)
        'find the known return chunk epilog, and get the 3rd one
        '(this will work as long as the function control flow doesn't change massively)
        Dim readP2PPacket_3_end_offset = 0
        Dim epilog_ops() As Byte = {&H5B, &H8B, &HE5, &H5D}
        Dim epilogs_found = 0
        While True
            Dim readP2PPacket_3_bytes = ReadBytes(readP2PPacket_3+readP2PPacket_3_end_offset, 4)
            If readP2PPacket_3_bytes.SequenceEqual(epilog_ops) Then
                epilogs_found += 1
                'add 4 to account for the epilog ops length
                readP2PPacket_3_end_offset += 4

                'if this is the 3rd epilog, end
                If epilogs_found = 3 Then
                    Exit While
                End If
            Else
                readP2PPacket_3_end_offset += 1
                If readP2PPacket_3_end_offset < 0 Or readP2PPacket_3_end_offset > 1000
                    Throw New ApplicationException("Unable to find last instruction in readP2PPacket function")
                End If
            End If
        End While

        Dim readP2PPacket_end As IntPtr = readP2PPacket_3 + readP2PPacket_3_end_offset

        'Init in-memory list switch
        listTypeInMemory = New AllocatedMemory(_targetProcessHandle, 4)

        'Init in-memory block list
        blocklistInMemorySize = 8 * 200 '200 block slots should be good for now
        blocklistInMemory = New AllocatedMemory(_targetProcessHandle, blocklistInMemorySize)

        'Init in-memory white list
        whitelistInMemorySize = 8 * 200 '200 slots should be good for now
        whitelistInMemory = New AllocatedMemory(_targetProcessHandle, whitelistInMemorySize)

        Dim listtype_asm_addr = BitConverter.GetBytes(CType(CType(listTypeInMemory, IntPtr), Integer))
        Dim blocklist_asm_addr = BitConverter.GetBytes(CType(CType(blocklistInMemory, IntPtr), Integer))
        Dim blocklist_asm_length = BitConverter.GetBytes(blocklistInMemorySize)
        Dim whitelist_asm_addr = BitConverter.GetBytes(CType(CType(whitelistInMemory, IntPtr), Integer))
        Dim whitelist_asm_length = BitConverter.GetBytes(whitelistInMemorySize)

        'for 1st instruction of sendP2PPacket, jump to a check if the target is in our blocklist. Immediate return if so
        'ASM in ASM\ASM-SendPacketBlockList.txt
        Dim sendP2PdetourCode() As Byte = { 
            &H50, &H53, &H51, &H52, &H8B, &H44, &H24, &H14, &H8B, &H54,
            &H24, &H18, &HBB, &H00, &H00, &H00, &H00, &H8B, &H0D,
            listtype_asm_addr(0), listtype_asm_addr(1), listtype_asm_addr(2), listtype_asm_addr(3),
            &H83, &HF9, &H00, &H74, &H02, &HEB, &H1D, &HB9,
            blocklist_asm_addr(0), blocklist_asm_addr(1), blocklist_asm_addr(2), blocklist_asm_addr(3),
            &H3B, &H04, &H19, &H75, &H06, &H3B, &H54, &H19,
            &H04, &H74, &H2A, &H83, &HC3, &H08, &H81, &HFB,
            blocklist_asm_length(0), blocklist_asm_length(1), blocklist_asm_length(2), blocklist_asm_length(3),
            &H7C, &HEA, &HEB, &H24, &HB9,
            whitelist_asm_addr(0), whitelist_asm_addr(1), whitelist_asm_addr(2), whitelist_asm_addr(3),
            &H3B, &H04, &H19, &H75, &H06, &H3B, &H54, &H19, &H04, &H74,
            &H14, &H83, &HC3, &H08, &H81, &HFB,
            whitelist_asm_length(0), whitelist_asm_length(1), whitelist_asm_length(2), whitelist_asm_length(3),
            &H7C, &HEA, &HEB, &H00, &H5A, &H59, &H5B, &H58, &HC2, &H18,
            &H00, &H5A, &H59, &H5B, &H58 }

        Debug.Assert(sendP2PdetourCode.Count <= allocatedCodeSize, "You need more space for the SendP2PPacket code")

        blocklistSendDetour = New AllocatedMemory(_targetProcessHandle, allocatedCodeSize) 'alloc memory for new code

        blocklistSendHook = New JmpHook(_targetProcessHandle, sendP2PPacket_addr, blocklistSendDetour, 6) 'inject jump into function

        sendP2PdetourCode = blocklistSendHook.PatchCode(sendP2PdetourCode) 'patch new code to include overwritten old code and return

        WriteProcessMemory(_targetProcessHandle, blocklistSendDetour, sendP2PdetourCode, allocatedCodeSize, 0) 'write new code to alloc'd memory

        blocklistSendHook.Activate()

        'for the last (return) instruction of ReadP2PPacket, jump to a check if the target that the function tells us the packet is from is in our blocklist. Return false if so
        'ASM in ASM\ASM-ReadPacketBlockList.txt
        Dim readP2PdetourCode() As Byte = {
            &H50, &H53, &H51, &H52, &H8B, &H44, &H24, &H20, &H8B, &H00,
            &H8B, &H54, &H24, &H20, &H8B, &H52, &H04, &HBB, &H00, &H00,
            &H00, &H00, &H8B, &H0D,
            listtype_asm_addr(0), listtype_asm_addr(1), listtype_asm_addr(2), listtype_asm_addr(3),
            &H83, &HF9, &H00, &H74, &H02, &HEB, &H1D, &HB9,
            blocklist_asm_addr(0), blocklist_asm_addr(1), blocklist_asm_addr(2), blocklist_asm_addr(3),
            &H3B, &H04, &H19, &H75, &H06, &H3B, &H54, &H19, &H04, &H74,
            &H2A, &H83, &HC3, &H08, &H81, &HFB,
            blocklist_asm_length(0), blocklist_asm_length(1), blocklist_asm_length(2), blocklist_asm_length(3),
            &H7C, &HEA, &HEB, &H26, &HB9,
            whitelist_asm_addr(0), whitelist_asm_addr(1), whitelist_asm_addr(2), whitelist_asm_addr(3),
            &H3B, &H04, &H19, &H75, &H06, &H3B, &H54, &H19, &H04, &H74,
            &H16, &H83, &HC3, &H08, &H81, &HFB,
            whitelist_asm_length(0), whitelist_asm_length(1), whitelist_asm_length(2), whitelist_asm_length(3),
            &H7C, &HEA, &HEB, &H00, &H5A, &H59, &H5B, &H58, &HB0, &H00,
            &HC2, &H14, &H00, &H5A, &H59, &H5B, &H58 }

        Debug.Assert(readP2PdetourCode.Count <= allocatedCodeSize, "You need more space for the ReadP2PPacket code")

        blocklistRecvDetour = New AllocatedMemory(_targetProcessHandle, allocatedCodeSize)

        'it's ok to write 1 bytes past the end of the function, since there should be at least 1 filler 0xCC byte there
        blocklistRecvHook = New JmpHook(_targetProcessHandle, readP2PPacket_end, blocklistRecvDetour, 5)

        readP2PdetourCode = blocklistRecvHook.PatchCode(readP2PdetourCode)

        WriteProcessMemory(_targetProcessHandle, blocklistRecvDetour, readP2PdetourCode, allocatedCodeSize, 0)

        blocklistRecvHook.Activate()

        Return True
    End Function

    Public Sub Sync_MemoryBlockList(blockednodes As DataGridViewRowCollection)
        Debug.Assert(blocklistInMemorySize >= blockednodes.Count * 8, "Blocklist of size="+blockednodes.Count.ToString()+" larger than allocated memory of count="+blocklistInMemorySize.ToString()+". Need to increase size.")

        'convert steam64 strings to in-memory (steam64) ints
        Dim steamid_array(blocklistInMemorySize) As Byte
        Dim i = 0
        For Each blockNode As DataGridViewRow In blockednodes
            'remove the postfix notation about the block type from the id
            Dim steamID As String = blockNode.Cells("steamId").Value
            If steamID.Contains("_") Then
                steamID = steamID.Remove(steamID.LastIndexOf("_"),2)
            End If

            Dim upperSteamId() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Left(steamID, 8), 16))
            Dim lowerSteamId() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Right(steamID, 8), 16))
            'little endian order the ints
            steamid_array(i + 0) = lowerSteamId(0)
            steamid_array(i + 1) = lowerSteamId(1)
            steamid_array(i + 2) = lowerSteamId(2)
            steamid_array(i + 3) = lowerSteamId(3)
            steamid_array(i + 4) = upperSteamId(0)
            steamid_array(i + 5) = upperSteamId(1)
            steamid_array(i + 6) = upperSteamId(2)
            steamid_array(i + 7) = upperSteamId(3)
            i += 8
        Next
        WriteProcessMemory(_targetProcessHandle, blocklistInMemory, steamid_array, blocklistInMemorySize, 0)
    End Sub

    Public Sub Sync_MemoryWhiteList(whitenodes As String())
        Debug.Assert(WhitelistInMemorySize >= whitenodes.Count * 8, "Whitelist larger than allocated memory. Need to increase size.")

        'convert steam64 strings to in-memory (steam64) ints
        Dim steamid_array(whitelistInMemorySize) As Byte
        Dim i = 0
        For Each steamID In whitenodes
            If steamID <> "" Then
                Dim upperSteamId() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Left(steamID, 8), 16))
                Dim lowerSteamId() As Byte = BitConverter.GetBytes(Convert.ToInt32(Microsoft.VisualBasic.Right(steamID, 8), 16))
                'little endian order the ints
                steamid_array(i + 0) = lowerSteamId(0)
                steamid_array(i + 1) = lowerSteamId(1)
                steamid_array(i + 2) = lowerSteamId(2)
                steamid_array(i + 3) = lowerSteamId(3)
                steamid_array(i + 4) = upperSteamId(0)
                steamid_array(i + 5) = upperSteamId(1)
                steamid_array(i + 6) = upperSteamId(2)
                steamid_array(i + 7) = upperSteamId(3)
                i += 8
            End If
        Next
        WriteProcessMemory(_targetProcessHandle, whitelistInMemory, steamid_array, whitelistInMemorySize, 0)
    End Sub

    Public Sub UpdateNodes()
        Dim nodeCount As Integer = ReadInt32(dsBase + &HF62DD0)
        Dim basicNodeInfo As New Dictionary(Of String, String)
        Dim steamNodeList = ReadInt32(dsBase + &HF62DCC)
        Dim steamNodesPtr As IntPtr = ReadIntPtr(steamNodeList)
        For i = 0 To nodeCount - 1
            Dim node As New DSNode
            ' SteamPlayerData
            Dim steamData1 As Integer = ReadInt32(steamNodesPtr + &HC)
            ' SteamOnlineIDData
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
        Dim nodePtr As IntPtr
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
                    MainWindow.blockUserForMinAccountAge(nodeSteamId)
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

        Dim sessionManagerAddress = dsBase + &HF62CC0
        Dim p2pSystem = ReadIntPtr(sessionManagerAddress + &H64)
        Dim connectionList = ReadIntPtr(p2pSystem + &H54)
        Dim connectionListEntry = ReadIntPtr(connectionList)
        Dim connection As IntPtr
        While connectionListEntry <> connectionList
            connection = ReadIntPtr(connectionListEntry + &H08)
            Dim connectionStatus = ReadInt32(connection + &H08)
            If connectionStatus > 2 Then
                nodeSteamId = LCase(Hex(ReadUInt64(connection + &H170))).PadLeft(16, "0")
                If ConnectedNodes.ContainsKey(nodeSteamId) Then
                    Dim node = ConnectedNodes(nodeSteamId)
                    node.Ping = ReadInt32(connection + &H178)
                End If
            End If
            connectionListEntry = ReadIntPtr(connectionListEntry)
        End While



        Dim selfPtr As IntPtr = ReadIntPtr(dsBase + &HF7E204)
        SelfNode.SoulLevel = ReadInt32(selfPtr + &HA30)
        SelfNode.MPZone = ReadInt32(selfPtr + &HA14)
        SelfNode.World = ReadInt8(selfPtr + &HA13) & "-" & ReadInt8(selfPtr + &HA12)
        SelfNode.PhantomType = ReadInt32(selfPtr + &HA28)

        Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
        heroPtr = ReadIntPtr(heroPtr + &H8)
        SelfNode.Indictments = ReadInt32(heroPtr + &HEC)
        SelfNode.Covenant = ReadInt8(heroPtr + &H10B)
    End Sub
    Public Function ReadLobbyList As List(Of UInt64)
        Dim outList As New List(Of UInt64)
        Dim steamIdPtr As IntPtr
        For i = 1 To 50
            steamIdPtr = lobbyDumpMemory.address + &H200 + 8 * (i-1)
            If steamIdPtr = 0 Then
                Exit For
            End If
            outList.Add(ReadUInt64(steamIdPtr))
        Next
        Return outList
    End Function
    Public ReadOnly Property HasDarkmoonRingEquipped As Boolean
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
            heroPtr = ReadIntPtr(heroPtr + &H8)
            Return ReadInt32(heroPtr + &H280) = 102 Or ReadInt32(heroPtr + &H284) = 102
        End Get
    End Property
    Public ReadOnly Property HasCatCovenantRingEquipped As Boolean
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
            heroPtr = ReadIntPtr(heroPtr + &H8)
            Return ReadInt32(heroPtr + &H280) = 103 Or ReadInt32(heroPtr + &H284) = 103
        End Get
    End Property

    Public ReadOnly Property ClearCount As Integer
    Get
            Dim statsPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
            Return ReadInt32(statsPtr + &H3C)
    End Get
    End Property
    Public ReadOnly Property Deaths As Integer
        Get
            Dim statsPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
            Return ReadInt32(statsPtr + &H5C)
        End Get
    End Property
    Public ReadOnly Property PhantomType As Integer
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF7DC70)
            heroPtr = ReadIntPtr(heroPtr + &H4)
            heroPtr = ReadIntPtr(heroPtr)
            Return ReadInt32(heroPtr + &H70)
        End Get
    End Property
    Public ReadOnly Property TeamType As Integer
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF7DC70)
            heroPtr = ReadIntPtr(heroPtr + &H4)
            heroPtr = ReadIntPtr(heroPtr)
            Return ReadInt32(heroPtr + &H74)
        End Get
    End Property
    Public ReadOnly Property TimePlayed As Integer
        Get
            Dim statsPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
            Return ReadInt32(statsPtr + &H68)
        End Get
    End Property
    Public ReadOnly Property Sin As Integer
    Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF78700)
            heroPtr = ReadIntPtr(heroPtr + &H8)
            Return ReadInt32(heroPtr + &HEC)
    End Get
    End Property


    Public ReadOnly Property redCooldown As Single
        Get
            Return ReadFloat(dsBase + &HF795E0)
        End Get
    End Property
    Public ReadOnly Property blueCooldown As Single
        Get
            Return ReadFloat(dsBase + &HF79658)
        End Get
    End Property


    Public ReadOnly Property FlagsArtoriasDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H2300) And &H40000000)
        End Get
    End Property
    Public ReadOnly Property FlagsBedOfChaosDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H200000)
        End Get
    End Property
    Public ReadOnly Property FlagsBellGargoylesDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H10000000)
        End Get
    End Property
    Public ReadOnly Property FlagsCapraDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &HF70) And &H2000000)
        End Get
    End Property
    Public ReadOnly Property FlagsCeaselessDischargeDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H3C70) And &H8000000)
        End Get
    End Property
    Public ReadOnly Property FlagsCentipedeDemonDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H3C70) And &H4000000)
        End Get
    End Property
    Public ReadOnly Property FlagsDarkAnorLondo As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H4630) And &H8000)
        End Get
    End Property
    Public ReadOnly Property FlagsDemonFiresageDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H3C30) And &H20)
        End Get
    End Property
    Public ReadOnly Property FlagsFourKingsDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H40000)
        End Get
    End Property
    Public ReadOnly Property FlagsGapingDragonDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H20000000)
        End Get
    End Property
    Public ReadOnly Property FlagsGwynDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H10000)
        End Get
    End Property
    Public ReadOnly Property FlagsGwyndolinDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H4670) And &H8000000)
        End Get
    End Property
    Public ReadOnly Property FlagsIronGolemDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H100000)
        End Get
    End Property
    Public ReadOnly Property FlagsKalameetDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H2300) And &H8000000)
        End Get
    End Property
    Public ReadOnly Property FlagsManusDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H2300) And &H20000000)
        End Get
    End Property
    Public ReadOnly Property FlagsMoonlightButterflyDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H1E70) And &H8000000)
        End Get
    End Property
    Public ReadOnly Property FlagsNewLondoDrained As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H4B0C) And &H8000000)
        End Get
    End Property
    Public ReadOnly Property FlagsNitoDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H1000000)
        End Get
    End Property
    Public ReadOnly Property FlagsOnSDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H80000)
        End Get
    End Property
    Public ReadOnly Property FlagsPinwheelDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H2000000)
        End Get
    End Property
    Public ReadOnly Property FlagsPriscillaDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H8000000)
        End Get
    End Property
    Public ReadOnly Property FlagsQuelaagDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H400000)
        End Get
    End Property
    Public ReadOnly Property FlagsSanctuaryGuardianDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H2300) And &H80000000)
        End Get
    End Property
    Public ReadOnly Property FlagsSeathDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H20000)
        End Get
    End Property
    Public ReadOnly Property FlagsSifDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &H0) And &H4000000)
        End Get
    End Property
    Public ReadOnly Property FlagsTaurusDead As Boolean
        Get
            Dim flagsPtr As IntPtr = ReadIntPtr(dsBase + &HF7D7D4)
            flagsPtr = ReadIntPtr(flagsPtr)

            Return (ReadInt32(flagsPtr + &HF70) And &H4000000)
        End Get
    End Property


    Public ReadOnly Property xPos As Single
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF7DC70)
            heroPtr = ReadIntPtr(heroPtr + &H4)
            heroPtr = ReadIntPtr(heroPtr)
            heroPtr = ReadIntPtr(heroPtr + &H28)
            heroPtr = ReadIntPtr(heroPtr + &H1C)

            Return ReadFloat(heroPtr + &H10)
        End Get
    End Property
    Public ReadOnly Property yPos As Single
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF7DC70)
            heroPtr = ReadIntPtr(heroPtr + &H4)
            heroPtr = ReadIntPtr(heroPtr)
            heroPtr = ReadIntPtr(heroPtr + &H28)
            heroPtr = ReadIntPtr(heroPtr + &H1C)

            Return ReadFloat(heroPtr + &H14)
        End Get
    End Property
    Public ReadOnly Property zPos As Single
        Get
            Dim heroPtr As IntPtr = ReadIntPtr(dsBase + &HF7DC70)
            heroPtr = ReadIntPtr(heroPtr + &H4)
            heroPtr = ReadIntPtr(heroPtr)
            heroPtr = ReadIntPtr(heroPtr + &H28)
            heroPtr = ReadIntPtr(heroPtr + &H1C)

            Return ReadFloat(heroPtr + &H18)
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
            Return New IntPtr(BitConverter.ToInt32(_rtnBytes, 0))
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
    Private Function ReadSteamName(ByVal addr As IntPtr) As String
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
    Public Sub WriteAsciiStr(ByVal addr As IntPtr, str As String)
        WriteProcessMemory(_targetProcessHandle, addr, System.Text.Encoding.ASCII.GetBytes(str), str.Length, Nothing)
    End Sub
End Class
