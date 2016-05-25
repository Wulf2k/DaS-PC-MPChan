Partial Class WaitWindow
    ''' <summary>
    ''' Designer variable used to keep track of non-visual components.
    ''' </summary>
    Private components As System.ComponentModel.IContainer = Nothing

    ''' <summary>
    ''' Disposes resources used by the form.
    ''' </summary>
    ''' <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            If components IsNot Nothing Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    ''' <summary>
    ''' This method is required for Windows Forms designer support.
    ''' Do not change the method contents inside the source code editor. The Forms designer might
    ''' not be able to load this method if it was changed manually.
    ''' </summary>
    Private Sub InitializeComponent()
        Me.Marque = New System.Windows.Forms.ProgressBar()
        Me.MessageLabel = New System.Windows.Forms.Label()
        Me.TopMost = True
        Me.SuspendLayout()
        ' 
        ' Marque
        ' 
        Me.Marque.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Marque.Location = New System.Drawing.Point(12, 46)
        Me.Marque.MarqueeAnimationSpeed = 1
        Me.Marque.Name = "Marque"
        Me.Marque.Size = New System.Drawing.Size(276, 12)
        Me.Marque.Style = System.Windows.Forms.ProgressBarStyle.Marquee
        Me.Marque.TabIndex = 0
        ' 
        ' MessageLabel
        ' 
        Me.MessageLabel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.MessageLabel.BackColor = System.Drawing.Color.Transparent
        Me.MessageLabel.Location = New System.Drawing.Point(12, 12)
        Me.MessageLabel.Name = "MessageLabel"
        Me.MessageLabel.Size = New System.Drawing.Size(276, 23)
        Me.MessageLabel.TabIndex = 1
        Me.MessageLabel.Text = "Please wait ..."
        Me.MessageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        ' 
        ' WaitWindow
        ' 
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.Color.WhiteSmoke
        Me.ClientSize = New System.Drawing.Size(300, 70)
        Me.Controls.Add(Me.MessageLabel)
        Me.Controls.Add(Me.Marque)
        Me.Font = New System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CByte(0))
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "DSCM"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Please wait..."
        Me.ResumeLayout(False)
    End Sub
    Public MessageLabel As System.Windows.Forms.Label
    Private Marque As System.Windows.Forms.ProgressBar
End Class
