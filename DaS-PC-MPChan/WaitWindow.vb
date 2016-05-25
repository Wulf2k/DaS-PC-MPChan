Imports System
Imports System.Windows.Forms

Partial Friend Class WaitWindow
    Inherits Form
    Public Sub New(parentLocation As Point, parentWidth As Integer, parentHeight As Integer)
        InitializeComponent()

        Me.Left = parentLocation.X + (parentWidth / 2) - (Me.Width / 2)
        Me.Top = parentLocation.Y + (parentHeight / 2) - (Me.Height / 2)
    End Sub

    Protected Overrides Sub OnPaint(e As System.Windows.Forms.PaintEventArgs)
        MyBase.OnPaint(e)
        ControlPaint.DrawBorder3D(e.Graphics, Me.ClientRectangle, Border3DStyle.Raised)
    End Sub

    Friend Sub SetMessage(message As String)
        Me.Invoke(Sub() Me.MessageLabel.Text = message)
    End Sub

    Friend Sub Cancel()
        Me.Invoke(New MethodInvoker(AddressOf Me.Close), Nothing)
    End Sub
End Class
