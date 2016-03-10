<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.nmbMPChannel = New System.Windows.Forms.NumericUpDown()
        Me.chkDebugDrawing = New System.Windows.Forms.CheckBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnReconnect = New System.Windows.Forms.Button()
        Me.chkNamedNodes = New System.Windows.Forms.CheckBox()
        CType(Me.nmbMPChannel, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.BackColor = System.Drawing.Color.LightGray
        Me.Label6.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.Location = New System.Drawing.Point(78, 9)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(80, 16)
        Me.Label6.TabIndex = 48
        Me.Label6.Text = "MP Channel"
        '
        'nmbMPChannel
        '
        Me.nmbMPChannel.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.nmbMPChannel.Location = New System.Drawing.Point(174, 7)
        Me.nmbMPChannel.Maximum = New Decimal(New Integer() {15, 0, 0, 0})
        Me.nmbMPChannel.Name = "nmbMPChannel"
        Me.nmbMPChannel.Size = New System.Drawing.Size(53, 22)
        Me.nmbMPChannel.TabIndex = 47
        '
        'chkDebugDrawing
        '
        Me.chkDebugDrawing.AutoSize = True
        Me.chkDebugDrawing.BackColor = System.Drawing.Color.LightGray
        Me.chkDebugDrawing.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkDebugDrawing.Location = New System.Drawing.Point(81, 37)
        Me.chkDebugDrawing.Name = "chkDebugDrawing"
        Me.chkDebugDrawing.Size = New System.Drawing.Size(113, 20)
        Me.chkDebugDrawing.TabIndex = 46
        Me.chkDebugDrawing.Text = "Node Drawing"
        Me.chkDebugDrawing.UseVisualStyleBackColor = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.LightGray
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(186, 98)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(137, 13)
        Me.Label1.TabIndex = 49
        Me.Label1.Text = "Dark Souls - 2016.03.09.21"
        '
        'btnReconnect
        '
        Me.btnReconnect.Location = New System.Drawing.Point(1, 89)
        Me.btnReconnect.Name = "btnReconnect"
        Me.btnReconnect.Size = New System.Drawing.Size(69, 23)
        Me.btnReconnect.TabIndex = 50
        Me.btnReconnect.Text = "Reconnect"
        Me.btnReconnect.UseVisualStyleBackColor = True
        '
        'chkNamedNodes
        '
        Me.chkNamedNodes.AutoSize = True
        Me.chkNamedNodes.BackColor = System.Drawing.Color.LightGray
        Me.chkNamedNodes.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkNamedNodes.Location = New System.Drawing.Point(81, 63)
        Me.chkNamedNodes.Name = "chkNamedNodes"
        Me.chkNamedNodes.Size = New System.Drawing.Size(205, 20)
        Me.chkNamedNodes.TabIndex = 51
        Me.chkNamedNodes.Text = "Named Nodes (Experimental)"
        Me.chkNamedNodes.UseVisualStyleBackColor = False
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(325, 116)
        Me.Controls.Add(Me.chkNamedNodes)
        Me.Controls.Add(Me.btnReconnect)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.nmbMPChannel)
        Me.Controls.Add(Me.chkDebugDrawing)
        Me.Name = "Form1"
        Me.Text = "Wulf's Dark Souls Connectivity Mod"
        CType(Me.nmbMPChannel, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents nmbMPChannel As System.Windows.Forms.NumericUpDown
    Friend WithEvents chkDebugDrawing As System.Windows.Forms.CheckBox
    Friend WithEvents Label1 As Label
    Friend WithEvents btnReconnect As Button
    Friend WithEvents chkNamedNodes As System.Windows.Forms.CheckBox
End Class
