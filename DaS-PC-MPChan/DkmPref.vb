Public Class DkmPref
    Private _name As String
    Private _value As Integer
    Private _helpText As String

    Public Sub New(ByVal name As String, ByVal value As String, ByVal helpText As String)
        Me._name = name
        Me._value = value
        Me._helpText = helpText
    End Sub

    Public ReadOnly Property Name() As String
        Get
            Return _name
        End Get
    End Property

    Public ReadOnly Property Value() As Integer
        Get
            Return _value
        End Get
    End Property

    Public ReadOnly Property HelpText() As String
        Get
            Return _helpText
        End Get
    End Property
End Class
