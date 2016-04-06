<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.nmbMPChannel = New System.Windows.Forms.NumericUpDown()
        Me.chkDebugDrawing = New System.Windows.Forms.CheckBox()
        Me.lblVer = New System.Windows.Forms.Label()
        Me.btnReconnect = New System.Windows.Forms.Button()
        Me.chkNamedNodes = New System.Windows.Forms.CheckBox()
        Me.chkExpand = New System.Windows.Forms.CheckBox()
        Me.dgvMPNodes = New System.Windows.Forms.DataGridView()
        Me.txtCurrNodes = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.nmbMaxNodes = New System.Windows.Forms.NumericUpDown()
        Me.lblAttemptCount = New System.Windows.Forms.Label()
        Me.txtTargetSteamID = New System.Windows.Forms.TextBox()
        Me.chkForce = New System.Windows.Forms.CheckBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.txtSelfSteamID = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        CType(Me.nmbMPChannel, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvMPNodes, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nmbMaxNodes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.BackColor = System.Drawing.Color.LightGray
        Me.Label6.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label6.Location = New System.Drawing.Point(13, 9)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(80, 16)
        Me.Label6.TabIndex = 48
        Me.Label6.Text = "MP Channel"
        '
        'nmbMPChannel
        '
        Me.nmbMPChannel.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.nmbMPChannel.Location = New System.Drawing.Point(109, 7)
        Me.nmbMPChannel.Maximum = New Decimal(New Integer() {15, 0, 0, 0})
        Me.nmbMPChannel.Name = "nmbMPChannel"
        Me.nmbMPChannel.Size = New System.Drawing.Size(40, 22)
        Me.nmbMPChannel.TabIndex = 47
        '
        'chkDebugDrawing
        '
        Me.chkDebugDrawing.AutoSize = True
        Me.chkDebugDrawing.BackColor = System.Drawing.Color.LightGray
        Me.chkDebugDrawing.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkDebugDrawing.Location = New System.Drawing.Point(16, 35)
        Me.chkDebugDrawing.Name = "chkDebugDrawing"
        Me.chkDebugDrawing.Size = New System.Drawing.Size(113, 20)
        Me.chkDebugDrawing.TabIndex = 46
        Me.chkDebugDrawing.Text = "Node Drawing"
        Me.chkDebugDrawing.UseVisualStyleBackColor = False
        '
        'lblVer
        '
        Me.lblVer.AutoSize = True
        Me.lblVer.BackColor = System.Drawing.Color.LightGray
        Me.lblVer.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblVer.Location = New System.Drawing.Point(369, 123)
        Me.lblVer.Name = "lblVer"
        Me.lblVer.Size = New System.Drawing.Size(137, 13)
        Me.lblVer.TabIndex = 49
        Me.lblVer.Text = "Dark Souls - 2016.04.05.22"
        '
        'btnReconnect
        '
        Me.btnReconnect.Location = New System.Drawing.Point(1, 113)
        Me.btnReconnect.Name = "btnReconnect"
        Me.btnReconnect.Size = New System.Drawing.Size(69, 23)
        Me.btnReconnect.TabIndex = 50
        Me.btnReconnect.Text = "Reattach"
        Me.btnReconnect.UseVisualStyleBackColor = True
        '
        'chkNamedNodes
        '
        Me.chkNamedNodes.AutoSize = True
        Me.chkNamedNodes.BackColor = System.Drawing.Color.LightGray
        Me.chkNamedNodes.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkNamedNodes.Location = New System.Drawing.Point(16, 61)
        Me.chkNamedNodes.Name = "chkNamedNodes"
        Me.chkNamedNodes.Size = New System.Drawing.Size(116, 20)
        Me.chkNamedNodes.TabIndex = 51
        Me.chkNamedNodes.Text = "Named Nodes"
        Me.chkNamedNodes.UseVisualStyleBackColor = False
        '
        'chkExpand
        '
        Me.chkExpand.AutoSize = True
        Me.chkExpand.BackColor = System.Drawing.Color.LightGray
        Me.chkExpand.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkExpand.Location = New System.Drawing.Point(16, 87)
        Me.chkExpand.Name = "chkExpand"
        Me.chkExpand.Size = New System.Drawing.Size(115, 20)
        Me.chkExpand.TabIndex = 52
        Me.chkExpand.Text = "Expand DSCM"
        Me.chkExpand.UseVisualStyleBackColor = False
        '
        'dgvMPNodes
        '
        Me.dgvMPNodes.AllowUserToAddRows = False
        Me.dgvMPNodes.AllowUserToDeleteRows = False
        Me.dgvMPNodes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvMPNodes.Location = New System.Drawing.Point(493, 122)
        Me.dgvMPNodes.Name = "dgvMPNodes"
        Me.dgvMPNodes.ReadOnly = True
        Me.dgvMPNodes.RowHeadersVisible = False
        Me.dgvMPNodes.Size = New System.Drawing.Size(518, 328)
        Me.dgvMPNodes.TabIndex = 53
        Me.dgvMPNodes.Visible = False
        '
        'txtCurrNodes
        '
        Me.txtCurrNodes.Location = New System.Drawing.Point(318, 8)
        Me.txtCurrNodes.Name = "txtCurrNodes"
        Me.txtCurrNodes.Size = New System.Drawing.Size(38, 20)
        Me.txtCurrNodes.TabIndex = 54
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(263, 9)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(49, 16)
        Me.Label1.TabIndex = 55
        Me.Label1.Text = "Nodes"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(356, 9)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(12, 16)
        Me.Label2.TabIndex = 56
        Me.Label2.Text = "/"
        '
        'nmbMaxNodes
        '
        Me.nmbMaxNodes.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.nmbMaxNodes.Location = New System.Drawing.Point(367, 6)
        Me.nmbMaxNodes.Maximum = New Decimal(New Integer() {32, 0, 0, 0})
        Me.nmbMaxNodes.Minimum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.nmbMaxNodes.Name = "nmbMaxNodes"
        Me.nmbMaxNodes.Size = New System.Drawing.Size(40, 22)
        Me.nmbMaxNodes.TabIndex = 57
        Me.nmbMaxNodes.Value = New Decimal(New Integer() {20, 0, 0, 0})
        '
        'lblAttemptCount
        '
        Me.lblAttemptCount.AutoSize = True
        Me.lblAttemptCount.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblAttemptCount.Location = New System.Drawing.Point(421, 88)
        Me.lblAttemptCount.Name = "lblAttemptCount"
        Me.lblAttemptCount.Size = New System.Drawing.Size(73, 16)
        Me.lblAttemptCount.TabIndex = 60
        Me.lblAttemptCount.Text = "Attempts: 0"
        '
        'txtTargetSteamID
        '
        Me.txtTargetSteamID.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTargetSteamID.Location = New System.Drawing.Point(377, 59)
        Me.txtTargetSteamID.Name = "txtTargetSteamID"
        Me.txtTargetSteamID.Size = New System.Drawing.Size(120, 22)
        Me.txtTargetSteamID.TabIndex = 59
        '
        'chkForce
        '
        Me.chkForce.AutoSize = True
        Me.chkForce.BackColor = System.Drawing.Color.LightGray
        Me.chkForce.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkForce.Location = New System.Drawing.Point(254, 87)
        Me.chkForce.Name = "chkForce"
        Me.chkForce.Size = New System.Drawing.Size(162, 20)
        Me.chkForce.TabIndex = 58
        Me.chkForce.Text = "Force Next Connection"
        Me.chkForce.UseVisualStyleBackColor = False
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(263, 36)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(111, 16)
        Me.Label3.TabIndex = 61
        Me.Label3.Text = "Your Steam64 ID:"
        '
        'txtSelfSteamID
        '
        Me.txtSelfSteamID.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSelfSteamID.Location = New System.Drawing.Point(377, 33)
        Me.txtSelfSteamID.Name = "txtSelfSteamID"
        Me.txtSelfSteamID.Size = New System.Drawing.Size(120, 22)
        Me.txtSelfSteamID.TabIndex = 62
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(251, 62)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(123, 16)
        Me.Label4.TabIndex = 63
        Me.Label4.Text = "Target Steam64 ID:"
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(509, 139)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.txtSelfSteamID)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.lblAttemptCount)
        Me.Controls.Add(Me.txtTargetSteamID)
        Me.Controls.Add(Me.chkForce)
        Me.Controls.Add(Me.nmbMaxNodes)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.txtCurrNodes)
        Me.Controls.Add(Me.dgvMPNodes)
        Me.Controls.Add(Me.chkExpand)
        Me.Controls.Add(Me.chkNamedNodes)
        Me.Controls.Add(Me.btnReconnect)
        Me.Controls.Add(Me.lblVer)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.nmbMPChannel)
        Me.Controls.Add(Me.chkDebugDrawing)
        Me.Name = "Form1"
        Me.Text = "Wulf's Dark Souls Connectivity Mod"
        CType(Me.nmbMPChannel, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvMPNodes, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nmbMaxNodes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents nmbMPChannel As System.Windows.Forms.NumericUpDown
    Friend WithEvents chkDebugDrawing As System.Windows.Forms.CheckBox
    Friend WithEvents lblVer As Label
    Friend WithEvents btnReconnect As Button
    Friend WithEvents chkNamedNodes As System.Windows.Forms.CheckBox
    Friend WithEvents chkExpand As CheckBox
    Friend WithEvents dgvMPNodes As DataGridView
    Friend WithEvents txtCurrNodes As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents nmbMaxNodes As NumericUpDown
    Friend WithEvents lblAttemptCount As Label
    Friend WithEvents txtTargetSteamID As TextBox
    Friend WithEvents chkForce As CheckBox
    Friend WithEvents Label3 As Label
    Friend WithEvents txtSelfSteamID As TextBox
    Friend WithEvents Label4 As Label
End Class
