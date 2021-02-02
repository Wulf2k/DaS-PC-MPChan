Public Class asm
    Public bytes() As Byte = {}
    Public pos As Int32

    Private reg8 As Hashtable = New Hashtable
    Private reg16 As Hashtable = New Hashtable
    Private reg32 As Hashtable = New Hashtable
    Private code As Hashtable = New Hashtable
    Private vars As Hashtable = New Hashtable

    Public Sub New()
        pos = 0
        init()
    End Sub

    Private Sub init()

        reg8.Clear()
        reg8.Add("al", 0)
        reg8.Add("cl", 1)
        reg8.Add("dl", 2)
        reg8.Add("bl", 3)
        reg8.Add("ah", 4)
        reg8.Add("ch", 5)
        reg8.Add("dh", 6)
        reg8.Add("bh", 7)

        reg16.Clear()
        reg16.Add("ax", 0)
        reg16.Add("cx", 1)
        reg16.Add("dx", 2)
        reg16.Add("bx", 3)

        reg32.Clear()
        reg32.Add("eax", 0)
        reg32.Add("ecx", 1)
        reg32.Add("edx", 2)
        reg32.Add("ebx", 3)
        reg32.Add("esp", 4)
        reg32.Add("ebp", 5)
        reg32.Add("esi", 6)
        reg32.Add("edi", 7)

        code.Clear()
        code.Add("inc", &H40)
        code.Add("dec", &H48)
        'code.Add("push", &H50)
        code.Add("pop", &H58)
        code.Add("pushad", &H60)
        code.Add("popad", &H61)
    End Sub

    Public Sub Add(ByVal newbytes() As Byte)
        bytes = bytes.Concat(newbytes).ToArray
    End Sub
    Public Sub AddVar(ByVal name As String, hexval As String)
        AddVar(name, Convert.ToUInt32(Microsoft.VisualBasic.Right(hexval, hexval.Length - 2), 16))
    End Sub
    Public Sub AddVar(ByVal name As String, val As IntPtr)
        AddVar(name, CUInt(val))
    End Sub
    Public Sub AddVar(ByVal name As String, val As UInt32)
        vars.Add(name.Replace(":", ""), val)
    End Sub
    Public Sub Clear()
        bytes = {}
    End Sub

    Private Sub ParseInput(ByVal str As String,
                           ByRef cmd As String,
                           ByRef reg1 As String, ByRef reg2 As String,
                           ByRef ptr1 As Boolean, ByRef ptr2 As Boolean,
                           ByRef plus1 As UInt32, ByRef plus2 As UInt32,
                           ByRef val1 As UInt32, ByRef val2 As UInt32)

        'Raw parameters
        Dim params As String = ""
        Dim param1 As String = ""
        Dim param2 As String = ""


        'Separate Command from params
        If str.Contains(" ") Then
            cmd = str.Split(" ")(0)
            params = Microsoft.VisualBasic.Right(str, str.Length - cmd.Length)
            params = params.Replace(" ", "")
        Else
            cmd = str
        End If

        'Check for section name
        If cmd.Contains(":") Then
            vars.Add(cmd.Replace(":", ""), pos)
            Return
        End If

        'Split params
        If params.Contains(",") Then
            param2 = params.Split(",")(1)
        End If
        param1 = params.Split(",")(0)

        'Check if immediate or pointers
        If param1.Contains("[") Then
            ptr1 = True
            param1 = param1.Replace("[", "")
            param1 = param1.Replace("]", "")
        End If
        If param2.Contains("[") Then
            ptr2 = True
            param2 = param2.Replace("[", "")
            param2 = param2.Replace("]", "")
        End If

        'Check if there are offsets in params
        If param1.Contains("+") Or param1.Contains("-") Then
            If param1.Contains("0x") Then
                plus1 = Convert.ToUInt32(param1(3) & Microsoft.VisualBasic.Right(param1, param1.Length - 6), 16)
            Else
                plus1 = Convert.ToUInt32(param1(3) & Microsoft.VisualBasic.Right(param1, param1.Length - 4))
            End If
            param1 = param1.Split("+")(0)
            param1 = param1.Split("-")(0)
        End If
        If param2.Contains("+") Or param2.Contains("-") Then
            If param2.Contains("0x") Then
                'plus2 = Convert.ToUInt32(param2(3) & Microsoft.VisualBasic.Right(param2, param2.Length - 6), 16)
                plus2 = Convert.ToUInt32(Microsoft.VisualBasic.Right(param2, param2.Length - 4), 16)
                If param2(3) = "-" Then plus2 *= -1
            Else
                plus2 = Convert.ToUInt32(param2(3) & Microsoft.VisualBasic.Right(param2, param2.Length - 4))
            End If
            param2 = param2.Split("+")(0)
            param2 = param2.Split("-")(0)
        End If

        'If not registers, convert params from hex to dec
        If param1.Contains("0x") Then
            val1 = Convert.ToUInt32(param1, 16)
        End If
        If param2.Contains("0x") Then
            val2 = Convert.ToUInt32(param2, 16)
        End If

        'If numeric, set values
        If IsNumeric(param1) Then
            val1 = param1
        End If
        If IsNumeric(param2) Then
            val2 = param2
        End If

        'Define registers, if not values
        If reg32.Contains(param1) Then reg1 = param1
        If reg32.Contains(param2) Then reg2 = param2
        If reg16.Contains(param1) Then reg1 = param1
        If reg16.Contains(param2) Then reg2 = param2
        If reg8.Contains(param1) Then reg1 = param1
        If reg8.Contains(param2) Then reg2 = param2


        'If param is previously defined section
        If vars.Contains(param1) Then
            val1 = vars(param1)
        End If
        If vars.Contains(param2) Then
            val2 = vars(param2)
        End If



    End Sub
    Public Sub Asm(ByVal str As String)
        Dim cmd As String = ""

        'Registers used
        Dim reg1 As String = ""
        Dim reg2 As String = ""

        'Are registers immediate or pointers
        Dim ptr1 As Boolean = False
        Dim ptr2 As Boolean = False

        'Offsets from registers
        Dim plus1 As UInt32 = 0
        Dim plus2 As UInt32 = 0

        'Values, if not registers
        Dim val1 As UInt32 = 0
        Dim val2 As UInt32 = 0

        ParseInput(str, cmd, reg1, reg2, ptr1, ptr2, plus1, plus2, val1, val2)

        Dim newbytes() As Byte = {}



        'Check if command is simple 1-byte command
        If code.Contains(cmd) Then
            newbytes = {0}
            newbytes(0) = code(cmd)
            If reg32.Contains(reg1) Then
                newbytes(0) = newbytes(0) Or reg32(reg1)
            End If
            Add(newbytes)
            pos += newbytes.Count
            Return
        End If




        Select Case cmd
            Case "call"
                If Not ptr1 Then
                    If reg32.Contains(reg1) Then
                        'Is only a register
                        newbytes = {&HFF, &HD0}
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                    Else
                        newbytes = {&HE8}
                        Dim addr As UInt32 = Convert.ToUInt32(val1) - pos - 5
                        newbytes = newbytes.Concat(BitConverter.GetBytes(addr)).ToArray

                    End If
                Else
                    'Is an offset from a register
                    If Math.Abs(plus1) < &H100 Then
                        If plus1 = 0 Then
                            newbytes = {&HFF, &H10}
                            newbytes(1) = newbytes(1) Or reg32(reg1)
                        Else
                            newbytes = {&HFF, &H50, 0}
                            newbytes(1) = newbytes(1) Or reg32(reg1)
                            newbytes(2) = plus1
                        End If
                    Else
                        newbytes = {&HFF, &H90}
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                        newbytes = newbytes.Concat(BitConverter.GetBytes(plus1)).ToArray
                    End If
                End If
                Add(newbytes)
                pos += newbytes.Count
                Return


            Case "jmp"
                If Not ptr1 Then
                    If reg32.Contains(reg1) Then
                        'Is only a register
                        newbytes = {&HFF, &HE0}
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                    Else
                        newbytes = {&HE9}
                        Dim addr As UInt32 = Convert.ToUInt32(val1) - pos - 5
                        newbytes = newbytes.Concat(BitConverter.GetBytes(addr)).ToArray

                    End If
                Else
                    'Is an offset from a register
                    If Math.Abs(plus1) < &H100 Then
                        If plus1 = 0 Then
                            newbytes = {&HFF, &H20}
                            newbytes(1) = newbytes(1) Or reg32(reg1)
                        Else
                            newbytes = {&HFF, &H60, 0}
                            newbytes(1) = newbytes(1) Or reg32(reg1)
                            newbytes(2) = plus1 And &HFF
                        End If
                    Else
                        newbytes = {&HFF, &HA0}
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                        newbytes = newbytes.Concat(BitConverter.GetBytes(plus1)).ToArray
                    End If
                End If
                Add(newbytes)
                pos += newbytes.Count
                Return


            Case "mov"
                '88 = r/m8, r8
                '89 = r/m16/32, r16/32
                '8a = r8, r/m8
                '8b = r16/32, r16/32

                'b0+r = r8, imm8
                'b8+r = r16/32, imm16/32
                'TODO:  Complete
                If reg8.Contains(reg1) And reg8.Contains(reg2) Then
                    newbytes = {&H88, &HC0}
                    newbytes(1) = newbytes(1) Or reg8(reg1)
                    newbytes(1) = newbytes(1) Or reg8(reg2) * 8
                    'TODO:  Complete
                End If


                If reg32.Contains(reg1) And reg2 = "" Then
                    newbytes = {&HB8}
                    newbytes(0) = newbytes(0) Or reg32(reg1)
                    newbytes = newbytes.Concat(BitConverter.GetBytes(val2)).ToArray
                End If

                If reg32.Contains(reg1) And reg32.Contains(reg2) Then
                    newbytes = {&H89, 0}

                    If ptr1 Then
                        newbytes(1) = newbytes(1) Or (reg32(reg2) * 8)
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                    End If
                    If ptr2 Then
                        newbytes(0) = newbytes(0) Or &H2
                        newbytes(1) = newbytes(1) Or (reg32(reg1) * 8)
                        newbytes(1) = newbytes(1) Or reg32(reg2)
                    End If

                    If Not (ptr1 Or ptr2) Then
                        newbytes(1) = newbytes(1) Or (reg32(reg2) * 8)
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                        newbytes(1) = newbytes(1) Or &HC0
                    End If

                    Dim offset As Uint32
                    offset = plus1 + plus2

                    If (ptr1 And reg1 = "esp") Or (ptr2 And reg2 = "esp") Then
                        newbytes = newbytes.Concat({&H24}).ToArray
                    End If

                    If Math.Abs(offset) < &H100 Then
                        If offset > 0 Or (ptr2 And reg2 = "ebp") Or (ptr1 And reg1 = "ebp") Then
                            newbytes(1) = newbytes(1) Or &H40
                            newbytes = newbytes.Concat({offset And &HFF}).ToArray
                        End If
                    End If
                    If Math.Abs(offset) > &HFF Then
                        newbytes(1) = newbytes(1) Or &H80
                        newbytes = newbytes.Concat(BitConverter.GetBytes(offset)).ToArray
                    End If
                End If

                Add(newbytes)
                pos += newbytes.Count
                Return


            Case "push"
                If Not ptr1 Then
                    If reg32.Contains(reg1) Then
                        'Is only a register
                        newbytes = {&H50}
                        newbytes(0) = newbytes(0) Or reg32(reg1)
                    Else
                        If Math.Abs(val1) < &H100 Then
                            newbytes = {&H6A, 0}
                            newbytes(1) = val1 And &HFF
                        Else
                            newbytes = {&H68}
                            newbytes = newbytes.Concat(BitConverter.GetBytes(val1)).ToArray
                        End If
                    End If
                Else
                    'Is an offset from a register
                    If Math.Abs(plus1) < &H100 Then
                        If plus1 = 0 Then
                            'No Offset
                            newbytes = {&HFF, &H30}
                        Else
                            'Offset between 0 and 0xFF
                            newbytes = {&HFF, &H70, 0}
                            newbytes(2) = plus1 And &HFF
                        End If
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                    Else
                        'Offset is > 0xFF
                        newbytes = {&HFF, &HB0}
                        newbytes(1) = newbytes(1) Or reg32(reg1)
                        newbytes = newbytes.Concat(BitConverter.GetBytes(plus1)).ToArray
                    End If
                End If
                Add(newbytes)
                pos += newbytes.Count
                Return

            Case "ret"
                newbytes = {&HC2}
                If Math.Abs(val1) > 0 Then
                    newbytes = newbytes.Concat(BitConverter.GetBytes(Convert.ToInt16(val1))).ToArray
                Else
                    newbytes(0) = newbytes(0) Or 1
                End If
                Add(newbytes)
                pos += newbytes.Count
                Return

        End Select
    End Sub
    Public Overrides Function ToString() As String
        Dim tmpstr As String = ""

        For Each byt In bytes
            tmpstr += "0x" & Hex(byt).PadLeft(2, "0") & ", "
        Next

        Return tmpstr
    End Function

End Class
