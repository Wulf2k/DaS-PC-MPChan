﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class DSCM
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
        Dim DataGridViewCellStyle1 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle4 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle5 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle6 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle7 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle8 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle9 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle10 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle11 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim DataGridViewCellStyle12 As System.Windows.Forms.DataGridViewCellStyle = New System.Windows.Forms.DataGridViewCellStyle()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(DSCM))
        Me.chkDebugDrawing = New System.Windows.Forms.CheckBox()
        Me.lblVer = New System.Windows.Forms.Label()
        Me.chkExpand = New System.Windows.Forms.CheckBox()
        Me.dgvMPNodes = New System.Windows.Forms.DataGridView()
        Me.txtCurrNodes = New System.Windows.Forms.TextBox()
        Me.lblNodes = New System.Windows.Forms.Label()
        Me.lblNodeDiv = New System.Windows.Forms.Label()
        Me.nmbMaxNodes = New System.Windows.Forms.NumericUpDown()
        Me.txtTargetSteamID = New System.Windows.Forms.TextBox()
        Me.lblYourId = New System.Windows.Forms.Label()
        Me.txtSelfSteamID = New System.Windows.Forms.TextBox()
        Me.lblTargetId = New System.Windows.Forms.Label()
        Me.btnAttemptId = New System.Windows.Forms.Button()
        Me.tabs = New System.Windows.Forms.TabControl()
        Me.tabActive = New System.Windows.Forms.TabPage()
        Me.tabFavorites = New System.Windows.Forms.TabPage()
        Me.dgvFavoriteNodes = New System.Windows.Forms.DataGridView()
        Me.tabRecent = New System.Windows.Forms.TabPage()
        Me.dgvRecentNodes = New System.Windows.Forms.DataGridView()
        Me.tabDSCMNet = New System.Windows.Forms.TabPage()
        Me.txtIRCDebug = New System.Windows.Forms.TextBox()
        Me.dgvDSCMNet = New System.Windows.Forms.DataGridView()
        Me.tabHelp = New System.Windows.Forms.TabPage()
        Me.btnAddFavorite = New System.Windows.Forms.Button()
        Me.btnRemFavorite = New System.Windows.Forms.Button()
        Me.lblNewVersion = New System.Windows.Forms.Label()
        Me.lblUrl = New System.Windows.Forms.Label()
        Me.chkDSCMNet = New System.Windows.Forms.CheckBox()
        Me.dsProcessStatus = New System.Windows.Forms.TextBox()
        Me.txtHelpView = New System.Windows.Forms.TextBox()
        CType(Me.dgvMPNodes, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nmbMaxNodes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabs.SuspendLayout()
        Me.tabActive.SuspendLayout()
        Me.tabFavorites.SuspendLayout()
        CType(Me.dgvFavoriteNodes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabRecent.SuspendLayout()
        CType(Me.dgvRecentNodes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabDSCMNet.SuspendLayout()
        CType(Me.dgvDSCMNet, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.tabHelp.SuspendLayout()
        Me.SuspendLayout()
        '
        'chkDebugDrawing
        '
        Me.chkDebugDrawing.AutoSize = True
        Me.chkDebugDrawing.BackColor = System.Drawing.SystemColors.Control
        Me.chkDebugDrawing.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkDebugDrawing.Location = New System.Drawing.Point(12, 4)
        Me.chkDebugDrawing.Name = "chkDebugDrawing"
        Me.chkDebugDrawing.Size = New System.Drawing.Size(113, 20)
        Me.chkDebugDrawing.TabIndex = 46
        Me.chkDebugDrawing.Text = "Node Drawing"
        Me.chkDebugDrawing.UseVisualStyleBackColor = False
        '
        'lblVer
        '
        Me.lblVer.AutoSize = True
        Me.lblVer.BackColor = System.Drawing.SystemColors.Control
        Me.lblVer.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblVer.Location = New System.Drawing.Point(700, 625)
        Me.lblVer.Name = "lblVer"
        Me.lblVer.Size = New System.Drawing.Size(76, 13)
        Me.lblVer.TabIndex = 49
        Me.lblVer.Text = "2016.05.09.15"
        '
        'chkExpand
        '
        Me.chkExpand.AutoSize = True
        Me.chkExpand.BackColor = System.Drawing.SystemColors.Control
        Me.chkExpand.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkExpand.Location = New System.Drawing.Point(12, 27)
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
        Me.dgvMPNodes.AllowUserToResizeRows = False
        DataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvMPNodes.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle1
        Me.dgvMPNodes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.dgvMPNodes.DefaultCellStyle = DataGridViewCellStyle2
        Me.dgvMPNodes.Location = New System.Drawing.Point(6, 6)
        Me.dgvMPNodes.Name = "dgvMPNodes"
        Me.dgvMPNodes.ReadOnly = True
        DataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle3.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvMPNodes.RowHeadersDefaultCellStyle = DataGridViewCellStyle3
        Me.dgvMPNodes.RowHeadersVisible = False
        Me.dgvMPNodes.Size = New System.Drawing.Size(740, 450)
        Me.dgvMPNodes.TabIndex = 53
        '
        'txtCurrNodes
        '
        Me.txtCurrNodes.Location = New System.Drawing.Point(688, 5)
        Me.txtCurrNodes.Name = "txtCurrNodes"
        Me.txtCurrNodes.ReadOnly = True
        Me.txtCurrNodes.Size = New System.Drawing.Size(38, 20)
        Me.txtCurrNodes.TabIndex = 54
        '
        'lblNodes
        '
        Me.lblNodes.AutoSize = True
        Me.lblNodes.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblNodes.Location = New System.Drawing.Point(633, 6)
        Me.lblNodes.Name = "lblNodes"
        Me.lblNodes.Size = New System.Drawing.Size(49, 16)
        Me.lblNodes.TabIndex = 55
        Me.lblNodes.Text = "Nodes"
        '
        'lblNodeDiv
        '
        Me.lblNodeDiv.AutoSize = True
        Me.lblNodeDiv.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblNodeDiv.Location = New System.Drawing.Point(726, 6)
        Me.lblNodeDiv.Name = "lblNodeDiv"
        Me.lblNodeDiv.Size = New System.Drawing.Size(12, 16)
        Me.lblNodeDiv.TabIndex = 56
        Me.lblNodeDiv.Text = "/"
        '
        'nmbMaxNodes
        '
        Me.nmbMaxNodes.Enabled = false
        Me.nmbMaxNodes.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.nmbMaxNodes.Location = New System.Drawing.Point(737, 3)
        Me.nmbMaxNodes.Maximum = New Decimal(New Integer() {32, 0, 0, 0})
        Me.nmbMaxNodes.Minimum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.nmbMaxNodes.Name = "nmbMaxNodes"
        Me.nmbMaxNodes.Size = New System.Drawing.Size(40, 22)
        Me.nmbMaxNodes.TabIndex = 57
        Me.nmbMaxNodes.Value = New Decimal(New Integer() {20, 0, 0, 0})
        '
        'txtTargetSteamID
        '
        Me.txtTargetSteamID.Font = New System.Drawing.Font("Consolas", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtTargetSteamID.Location = New System.Drawing.Point(645, 58)
        Me.txtTargetSteamID.Name = "txtTargetSteamID"
        Me.txtTargetSteamID.Size = New System.Drawing.Size(132, 23)
        Me.txtTargetSteamID.TabIndex = 59
        '
        'lblYourId
        '
        Me.lblYourId.AutoSize = True
        Me.lblYourId.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblYourId.Location = New System.Drawing.Point(531, 35)
        Me.lblYourId.Name = "lblYourId"
        Me.lblYourId.Size = New System.Drawing.Size(111, 16)
        Me.lblYourId.TabIndex = 61
        Me.lblYourId.Text = "Your Steam64 ID:"
        '
        'txtSelfSteamID
        '
        Me.txtSelfSteamID.Font = New System.Drawing.Font("Consolas", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txtSelfSteamID.Location = New System.Drawing.Point(645, 32)
        Me.txtSelfSteamID.Name = "txtSelfSteamID"
        Me.txtSelfSteamID.ReadOnly = True
        Me.txtSelfSteamID.Size = New System.Drawing.Size(132, 23)
        Me.txtSelfSteamID.TabIndex = 62
        '
        'lblTargetId
        '
        Me.lblTargetId.AutoSize = True
        Me.lblTargetId.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblTargetId.Location = New System.Drawing.Point(519, 61)
        Me.lblTargetId.Name = "lblTargetId"
        Me.lblTargetId.Size = New System.Drawing.Size(123, 16)
        Me.lblTargetId.TabIndex = 63
        Me.lblTargetId.Text = "Target Steam64 ID:"
        '
        'btnAttemptId
        '
        Me.btnAttemptId.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.btnAttemptId.Location = New System.Drawing.Point(645, 85)
        Me.btnAttemptId.Name = "btnAttemptId"
        Me.btnAttemptId.Size = New System.Drawing.Size(134, 23)
        Me.btnAttemptId.TabIndex = 65
        Me.btnAttemptId.Text = "Attempt Connection"
        Me.btnAttemptId.UseVisualStyleBackColor = True
        '
        'tabs
        '
        Me.tabs.Controls.Add(Me.tabActive)
        Me.tabs.Controls.Add(Me.tabFavorites)
        Me.tabs.Controls.Add(Me.tabRecent)
        Me.tabs.Controls.Add(Me.tabDSCMNet)
        Me.tabs.Controls.Add(Me.tabHelp)
        Me.tabs.Location = New System.Drawing.Point(10, 115)
        Me.tabs.Name = "tabs"
        Me.tabs.SelectedIndex = 0
        Me.tabs.Size = New System.Drawing.Size(765, 490)
        Me.tabs.TabIndex = 67
        Me.tabs.Visible = False
        '
        'tabActive
        '
        Me.tabActive.BackColor = System.Drawing.SystemColors.Control
        Me.tabActive.Controls.Add(Me.dgvMPNodes)
        Me.tabActive.Location = New System.Drawing.Point(4, 22)
        Me.tabActive.Name = "tabActive"
        Me.tabActive.Padding = New System.Windows.Forms.Padding(3)
        Me.tabActive.Size = New System.Drawing.Size(757, 464)
        Me.tabActive.TabIndex = 0
        Me.tabActive.Text = "Active"
        '
        'tabFavorites
        '
        Me.tabFavorites.BackColor = System.Drawing.SystemColors.Control
        Me.tabFavorites.Controls.Add(Me.dgvFavoriteNodes)
        Me.tabFavorites.Location = New System.Drawing.Point(4, 22)
        Me.tabFavorites.Name = "tabFavorites"
        Me.tabFavorites.Padding = New System.Windows.Forms.Padding(3)
        Me.tabFavorites.Size = New System.Drawing.Size(757, 464)
        Me.tabFavorites.TabIndex = 1
        Me.tabFavorites.Text = "Favorites"
        '
        'dgvFavoriteNodes
        '
        Me.dgvFavoriteNodes.AllowUserToAddRows = False
        Me.dgvFavoriteNodes.AllowUserToDeleteRows = False
        Me.dgvFavoriteNodes.AllowUserToResizeRows = False
        DataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle4.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvFavoriteNodes.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle4
        Me.dgvFavoriteNodes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle5.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.dgvFavoriteNodes.DefaultCellStyle = DataGridViewCellStyle5
        Me.dgvFavoriteNodes.Location = New System.Drawing.Point(6, 6)
        Me.dgvFavoriteNodes.Name = "dgvFavoriteNodes"
        Me.dgvFavoriteNodes.ReadOnly = True
        DataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle6.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle6.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle6.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle6.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle6.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvFavoriteNodes.RowHeadersDefaultCellStyle = DataGridViewCellStyle6
        Me.dgvFavoriteNodes.RowHeadersVisible = False
        Me.dgvFavoriteNodes.Size = New System.Drawing.Size(370, 450)
        Me.dgvFavoriteNodes.TabIndex = 54
        '
        'tabRecent
        '
        Me.tabRecent.BackColor = System.Drawing.SystemColors.Control
        Me.tabRecent.Controls.Add(Me.dgvRecentNodes)
        Me.tabRecent.Location = New System.Drawing.Point(4, 22)
        Me.tabRecent.Name = "tabRecent"
        Me.tabRecent.Size = New System.Drawing.Size(757, 464)
        Me.tabRecent.TabIndex = 2
        Me.tabRecent.Text = "Recent"
        '
        'dgvRecentNodes
        '
        Me.dgvRecentNodes.AllowUserToAddRows = False
        Me.dgvRecentNodes.AllowUserToDeleteRows = False
        Me.dgvRecentNodes.AllowUserToResizeRows = False
        DataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle7.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle7.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvRecentNodes.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle7
        Me.dgvRecentNodes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle8.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.dgvRecentNodes.DefaultCellStyle = DataGridViewCellStyle8
        Me.dgvRecentNodes.Location = New System.Drawing.Point(6, 6)
        Me.dgvRecentNodes.Name = "dgvRecentNodes"
        Me.dgvRecentNodes.ReadOnly = True
        DataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle9.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle9.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle9.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvRecentNodes.RowHeadersDefaultCellStyle = DataGridViewCellStyle9
        Me.dgvRecentNodes.RowHeadersVisible = False
        Me.dgvRecentNodes.Size = New System.Drawing.Size(370, 450)
        Me.dgvRecentNodes.TabIndex = 55
        '
        'tabDSCMNet
        '
        Me.tabDSCMNet.BackColor = System.Drawing.SystemColors.Control
        Me.tabDSCMNet.Controls.Add(Me.txtIRCDebug)
        Me.tabDSCMNet.Controls.Add(Me.dgvDSCMNet)
        Me.tabDSCMNet.Location = New System.Drawing.Point(4, 22)
        Me.tabDSCMNet.Name = "tabDSCMNet"
        Me.tabDSCMNet.Size = New System.Drawing.Size(757, 464)
        Me.tabDSCMNet.TabIndex = 3
        Me.tabDSCMNet.Text = "DSCM-Net"
        '
        'txtIRCDebug
        '
        Me.txtIRCDebug.Location = New System.Drawing.Point(6, 438)
        Me.txtIRCDebug.Name = "txtIRCDebug"
        Me.txtIRCDebug.Size = New System.Drawing.Size(740, 20)
        Me.txtIRCDebug.TabIndex = 55
        '
        'dgvDSCMNet
        '
        Me.dgvDSCMNet.AllowUserToAddRows = False
        Me.dgvDSCMNet.AllowUserToDeleteRows = False
        Me.dgvDSCMNet.AllowUserToResizeRows = False
        DataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle10.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvDSCMNet.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle10
        Me.dgvDSCMNet.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle11.BackColor = System.Drawing.SystemColors.Window
        DataGridViewCellStyle11.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle11.ForeColor = System.Drawing.SystemColors.ControlText
        DataGridViewCellStyle11.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle11.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.[False]
        Me.dgvDSCMNet.DefaultCellStyle = DataGridViewCellStyle11
        Me.dgvDSCMNet.Location = New System.Drawing.Point(6, 6)
        Me.dgvDSCMNet.Name = "dgvDSCMNet"
        Me.dgvDSCMNet.ReadOnly = True
        DataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle12.BackColor = System.Drawing.SystemColors.Control
        DataGridViewCellStyle12.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        DataGridViewCellStyle12.ForeColor = System.Drawing.SystemColors.WindowText
        DataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight
        DataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText
        DataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.[True]
        Me.dgvDSCMNet.RowHeadersDefaultCellStyle = DataGridViewCellStyle12
        Me.dgvDSCMNet.RowHeadersVisible = False
        Me.dgvDSCMNet.Size = New System.Drawing.Size(740, 425)
        Me.dgvDSCMNet.TabIndex = 54
        '
        'tabHelp
        '
        Me.tabHelp.Controls.Add(Me.txtHelpView)
        Me.tabHelp.Location = New System.Drawing.Point(4, 22)
        Me.tabHelp.Name = "tabHelp"
        Me.tabHelp.Padding = New System.Windows.Forms.Padding(3)
        Me.tabHelp.Size = New System.Drawing.Size(757, 464)
        Me.tabHelp.TabIndex = 4
        Me.tabHelp.Text = "Help"
        Me.tabHelp.UseVisualStyleBackColor = True
        '
        'btnAddFavorite
        '
        Me.btnAddFavorite.Location = New System.Drawing.Point(269, 615)
        Me.btnAddFavorite.Name = "btnAddFavorite"
        Me.btnAddFavorite.Size = New System.Drawing.Size(113, 23)
        Me.btnAddFavorite.TabIndex = 68
        Me.btnAddFavorite.Text = "Add Favorite"
        Me.btnAddFavorite.UseVisualStyleBackColor = True
        Me.btnAddFavorite.Visible = False
        '
        'btnRemFavorite
        '
        Me.btnRemFavorite.Location = New System.Drawing.Point(388, 615)
        Me.btnRemFavorite.Name = "btnRemFavorite"
        Me.btnRemFavorite.Size = New System.Drawing.Size(113, 23)
        Me.btnRemFavorite.TabIndex = 70
        Me.btnRemFavorite.Text = "Remove Favorite"
        Me.btnRemFavorite.UseVisualStyleBackColor = True
        Me.btnRemFavorite.Visible = False
        '
        'lblNewVersion
        '
        Me.lblNewVersion.AutoSize = True
        Me.lblNewVersion.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblNewVersion.ForeColor = System.Drawing.Color.Red
        Me.lblNewVersion.Location = New System.Drawing.Point(9, 76)
        Me.lblNewVersion.Name = "lblNewVersion"
        Me.lblNewVersion.Size = New System.Drawing.Size(183, 16)
        Me.lblNewVersion.TabIndex = 71
        Me.lblNewVersion.Text = "New testing version available"
        Me.lblNewVersion.Visible = False
        '
        'lblUrl
        '
        Me.lblUrl.AutoSize = True
        Me.lblUrl.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lblUrl.ForeColor = System.Drawing.Color.Red
        Me.lblUrl.Location = New System.Drawing.Point(9, 93)
        Me.lblUrl.Name = "lblUrl"
        Me.lblUrl.Size = New System.Drawing.Size(94, 16)
        Me.lblUrl.TabIndex = 72
        Me.lblUrl.Text = "http://wulf2k.ca"
        Me.lblUrl.Visible = False
        '
        'chkDSCMNet
        '
        Me.chkDSCMNet.AutoSize = True
        Me.chkDSCMNet.BackColor = System.Drawing.SystemColors.Control
        Me.chkDSCMNet.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.chkDSCMNet.Location = New System.Drawing.Point(12, 50)
        Me.chkDSCMNet.Name = "chkDSCMNet"
        Me.chkDSCMNet.Size = New System.Drawing.Size(119, 20)
        Me.chkDSCMNet.TabIndex = 73
        Me.chkDSCMNet.Text = "Join DSCM-Net"
        Me.chkDSCMNet.UseVisualStyleBackColor = False
        '
        'dsProcessStatus
        '
        Me.dsProcessStatus.Location = New System.Drawing.Point(10, 617)
        Me.dsProcessStatus.Name = "dsProcessStatus"
        Me.dsProcessStatus.ReadOnly = True
        Me.dsProcessStatus.Size = New System.Drawing.Size(187, 20)
        Me.dsProcessStatus.TabIndex = 74
        '
        'txtHelpView
        '
        Me.txtHelpView.Location = New System.Drawing.Point(7, 7)
        Me.txtHelpView.Multiline = True
        Me.txtHelpView.Name = "txtHelpView"
        Me.txtHelpView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtHelpView.Size = New System.Drawing.Size(744, 451)
        Me.txtHelpView.TabIndex = 0
        '
        'DSCM
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(784, 642)
        Me.Controls.Add(Me.dsProcessStatus)
        Me.Controls.Add(Me.chkDSCMNet)
        Me.Controls.Add(Me.lblUrl)
        Me.Controls.Add(Me.lblNewVersion)
        Me.Controls.Add(Me.btnRemFavorite)
        Me.Controls.Add(Me.btnAddFavorite)
        Me.Controls.Add(Me.tabs)
        Me.Controls.Add(Me.btnAttemptId)
        Me.Controls.Add(Me.lblTargetId)
        Me.Controls.Add(Me.txtSelfSteamID)
        Me.Controls.Add(Me.lblYourId)
        Me.Controls.Add(Me.txtTargetSteamID)
        Me.Controls.Add(Me.nmbMaxNodes)
        Me.Controls.Add(Me.lblNodeDiv)
        Me.Controls.Add(Me.lblNodes)
        Me.Controls.Add(Me.txtCurrNodes)
        Me.Controls.Add(Me.chkExpand)
        Me.Controls.Add(Me.lblVer)
        Me.Controls.Add(Me.chkDebugDrawing)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "DSCM"
        Me.Text = "Wulf's Dark Souls Connectivity Mod"
        CType(Me.dgvMPNodes, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nmbMaxNodes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabs.ResumeLayout(False)
        Me.tabActive.ResumeLayout(False)
        Me.tabFavorites.ResumeLayout(False)
        CType(Me.dgvFavoriteNodes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabRecent.ResumeLayout(False)
        CType(Me.dgvRecentNodes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabDSCMNet.ResumeLayout(False)
        Me.tabDSCMNet.PerformLayout()
        CType(Me.dgvDSCMNet, System.ComponentModel.ISupportInitialize).EndInit()
        Me.tabHelp.ResumeLayout(False)
        Me.tabHelp.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout

End Sub
    Friend WithEvents chkDebugDrawing As System.Windows.Forms.CheckBox
    Friend WithEvents lblVer As Label
    Friend WithEvents chkExpand As CheckBox
    Friend WithEvents dgvMPNodes As DataGridView
    Friend WithEvents txtCurrNodes As TextBox
    Friend WithEvents lblNodes As Label
    Friend WithEvents lblNodeDiv As Label
    Friend WithEvents nmbMaxNodes As NumericUpDown
    Friend WithEvents txtTargetSteamID As TextBox
    Friend WithEvents lblYourId As Label
    Friend WithEvents txtSelfSteamID As TextBox
    Friend WithEvents lblTargetId As Label
    Friend WithEvents btnAttemptId As System.Windows.Forms.Button
    Friend WithEvents tabs As TabControl
    Friend WithEvents tabActive As TabPage
    Friend WithEvents tabFavorites As TabPage
    Friend WithEvents tabRecent As TabPage
    Friend WithEvents btnAddFavorite As Button
    Friend WithEvents btnRemFavorite As Button
    Friend WithEvents dgvFavoriteNodes As DataGridView
    Friend WithEvents dgvRecentNodes As DataGridView
    Friend WithEvents lblNewVersion As Label
    Friend WithEvents lblUrl As Label
    Friend WithEvents tabDSCMNet As TabPage
    Friend WithEvents dgvDSCMNet As DataGridView
    Friend WithEvents chkDSCMNet As CheckBox
    Friend WithEvents txtIRCDebug As System.Windows.Forms.TextBox
    Friend WithEvents dsProcessStatus As System.Windows.Forms.TextBox
    Friend WithEvents tabHelp As System.Windows.Forms.TabPage
    Friend WithEvents txtHelpView As System.Windows.Forms.TextBox
End Class
