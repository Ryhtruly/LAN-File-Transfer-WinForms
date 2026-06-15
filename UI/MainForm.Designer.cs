namespace P2PFileSharingApp.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelTopBar = new System.Windows.Forms.Panel();
            lblAppName = new System.Windows.Forms.Label();
            btnTabServer = new System.Windows.Forms.Button();
            btnTabClient = new System.Windows.Forms.Button();
            lblStatus = new System.Windows.Forms.Label();
            panelContent = new System.Windows.Forms.Panel();
            panelServer = new System.Windows.Forms.Panel();
            grpLog = new System.Windows.Forms.GroupBox();
            lvLog = new System.Windows.Forms.ListView();
            colTime = new System.Windows.Forms.ColumnHeader();
            colAction = new System.Windows.Forms.ColumnHeader();
            splitServer = new System.Windows.Forms.SplitContainer();
            grpSharedFiles = new System.Windows.Forms.GroupBox();
            tvLocal = new System.Windows.Forms.TreeView();
            lblSharedPath = new System.Windows.Forms.Label();
            panelServerActions = new System.Windows.Forms.Panel();
            lblRoomName = new System.Windows.Forms.Label();
            txtRoomName = new System.Windows.Forms.TextBox();
            btnAddToRoot = new System.Windows.Forms.Button();
            btnToggleServer = new System.Windows.Forms.Button();
            grpClients = new System.Windows.Forms.GroupBox();
            lvClients = new System.Windows.Forms.ListView();
            colIP = new System.Windows.Forms.ColumnHeader();
            colPerm = new System.Windows.Forms.ColumnHeader();
            panelPermActions = new System.Windows.Forms.Panel();
            btnGrantRead = new System.Windows.Forms.Button();
            btnGrantWrite = new System.Windows.Forms.Button();
            btnDenyClient = new System.Windows.Forms.Button();
            panelClient = new System.Windows.Forms.Panel();
            grpRemoteFiles = new System.Windows.Forms.GroupBox();
            tvRemote = new System.Windows.Forms.TreeView();
            panelClientActions = new System.Windows.Forms.Panel();
            btnDownload = new System.Windows.Forms.Button();
            btnUpload = new System.Windows.Forms.Button();
            btnDelete = new System.Windows.Forms.Button();
            btnRename = new System.Windows.Forms.Button();
            btnMkDir = new System.Windows.Forms.Button();
            btnBackRemote = new System.Windows.Forms.Button();
            panelConnectBar = new System.Windows.Forms.Panel();
            lblConnectTo = new System.Windows.Forms.Label();
            cboServers = new System.Windows.Forms.ComboBox();
            lblPort = new System.Windows.Forms.Label();
            txtPort = new System.Windows.Forms.TextBox();
            btnConnect = new System.Windows.Forms.Button();
            lblConnStatus = new System.Windows.Forms.Label();
            panelTopBar.SuspendLayout();
            panelContent.SuspendLayout();
            panelServer.SuspendLayout();
            grpLog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitServer).BeginInit();
            splitServer.Panel1.SuspendLayout();
            splitServer.Panel2.SuspendLayout();
            splitServer.SuspendLayout();
            grpSharedFiles.SuspendLayout();
            panelServerActions.SuspendLayout();
            grpClients.SuspendLayout();
            panelPermActions.SuspendLayout();
            panelClient.SuspendLayout();
            grpRemoteFiles.SuspendLayout();
            panelClientActions.SuspendLayout();
            panelConnectBar.SuspendLayout();
            SuspendLayout();
            // 
            // panelTopBar
            // 
            panelTopBar.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
            panelTopBar.Controls.Add(lblAppName);
            panelTopBar.Controls.Add(btnTabServer);
            panelTopBar.Controls.Add(btnTabClient);
            panelTopBar.Controls.Add(lblStatus);
            panelTopBar.Dock = System.Windows.Forms.DockStyle.Top;
            panelTopBar.Location = new System.Drawing.Point(0, 0);
            panelTopBar.Name = "panelTopBar";
            panelTopBar.Size = new System.Drawing.Size(1184, 60);
            panelTopBar.TabIndex = 0;
            // 
            // lblAppName
            // 
            lblAppName.AutoSize = true;
            lblAppName.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblAppName.ForeColor = System.Drawing.Color.White;
            lblAppName.Location = new System.Drawing.Point(16, 15);
            lblAppName.Name = "lblAppName";
            lblAppName.Size = new System.Drawing.Size(212, 32);
            lblAppName.TabIndex = 0;
            lblAppName.Text = "NEXUS P2P Share";
            // 
            // btnTabServer
            // 
            btnTabServer.Location = new System.Drawing.Point(260, 12);
            btnTabServer.Name = "btnTabServer";
            btnTabServer.Size = new System.Drawing.Size(130, 36);
            btnTabServer.TabIndex = 1;
            btnTabServer.Text = "My Server";
            btnTabServer.UseVisualStyleBackColor = true;
            // 
            // btnTabClient
            // 
            btnTabClient.Location = new System.Drawing.Point(398, 12);
            btnTabClient.Name = "btnTabClient";
            btnTabClient.Size = new System.Drawing.Size(130, 36);
            btnTabClient.TabIndex = 2;
            btnTabClient.Text = "Connect";
            btnTabClient.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = System.Drawing.Color.White;
            lblStatus.Location = new System.Drawing.Point(982, 21);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(112, 20);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "Server dang tat";
            // 
            // panelContent
            // 
            panelContent.Controls.Add(panelServer);
            panelContent.Controls.Add(panelClient);
            panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            panelContent.Location = new System.Drawing.Point(0, 60);
            panelContent.Name = "panelContent";
            panelContent.Size = new System.Drawing.Size(1184, 701);
            panelContent.TabIndex = 1;
            // 
            // panelServer
            // 
            panelServer.Controls.Add(grpLog);
            panelServer.Controls.Add(splitServer);
            panelServer.Dock = System.Windows.Forms.DockStyle.Fill;
            panelServer.Location = new System.Drawing.Point(0, 0);
            panelServer.Name = "panelServer";
            panelServer.Padding = new System.Windows.Forms.Padding(12);
            panelServer.Size = new System.Drawing.Size(1184, 701);
            panelServer.TabIndex = 0;
            // 
            // grpLog
            // 
            grpLog.Controls.Add(lvLog);
            grpLog.Dock = System.Windows.Forms.DockStyle.Bottom;
            grpLog.Location = new System.Drawing.Point(12, 447);
            grpLog.Name = "grpLog";
            grpLog.Size = new System.Drawing.Size(1160, 242);
            grpLog.TabIndex = 1;
            grpLog.TabStop = false;
            grpLog.Text = "Nhat ky hoat dong";
            // 
            // lvLog
            // 
            lvLog.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colTime, colAction });
            lvLog.Dock = System.Windows.Forms.DockStyle.Fill;
            lvLog.FullRowSelect = true;
            lvLog.GridLines = true;
            lvLog.Location = new System.Drawing.Point(3, 23);
            lvLog.Name = "lvLog";
            lvLog.Size = new System.Drawing.Size(1154, 216);
            lvLog.TabIndex = 0;
            lvLog.UseCompatibleStateImageBehavior = false;
            lvLog.View = System.Windows.Forms.View.Details;
            // 
            // colTime
            // 
            colTime.Text = "Thoi gian";
            colTime.Width = 120;
            // 
            // colAction
            // 
            colAction.Text = "Su kien";
            colAction.Width = 1000;
            // 
            // splitServer
            // 
            splitServer.Dock = System.Windows.Forms.DockStyle.Fill;
            splitServer.Location = new System.Drawing.Point(12, 12);
            splitServer.Name = "splitServer";
            // 
            // splitServer.Panel1
            // 
            splitServer.Panel1.Controls.Add(grpSharedFiles);
            splitServer.Panel1.Controls.Add(panelServerActions);
            // 
            // splitServer.Panel2
            // 
            splitServer.Panel2.Controls.Add(grpClients);
            splitServer.Panel2.Controls.Add(panelPermActions);
            splitServer.Size = new System.Drawing.Size(1160, 677);
            splitServer.SplitterDistance = 560;
            splitServer.TabIndex = 0;
            // 
            // grpSharedFiles
            // 
            grpSharedFiles.Controls.Add(tvLocal);
            grpSharedFiles.Controls.Add(lblSharedPath);
            grpSharedFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            grpSharedFiles.Location = new System.Drawing.Point(0, 0);
            grpSharedFiles.Name = "grpSharedFiles";
            grpSharedFiles.Size = new System.Drawing.Size(560, 625);
            grpSharedFiles.TabIndex = 0;
            grpSharedFiles.TabStop = false;
            grpSharedFiles.Text = "Drive cua toi";
            // 
            // tvLocal
            // 
            tvLocal.Dock = System.Windows.Forms.DockStyle.Fill;
            tvLocal.Location = new System.Drawing.Point(3, 23);
            tvLocal.Name = "tvLocal";
            tvLocal.Size = new System.Drawing.Size(554, 579);
            tvLocal.TabIndex = 0;
            // 
            // lblSharedPath
            // 
            lblSharedPath.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblSharedPath.Location = new System.Drawing.Point(3, 602);
            lblSharedPath.Name = "lblSharedPath";
            lblSharedPath.Size = new System.Drawing.Size(554, 20);
            lblSharedPath.TabIndex = 1;
            lblSharedPath.Text = "Thu muc chia se";
            // 
            // panelServerActions
            // 
            panelServerActions.Controls.Add(lblRoomName);
            panelServerActions.Controls.Add(txtRoomName);
            panelServerActions.Controls.Add(btnAddToRoot);
            panelServerActions.Controls.Add(btnToggleServer);
            panelServerActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelServerActions.Location = new System.Drawing.Point(0, 625);
            panelServerActions.Name = "panelServerActions";
            panelServerActions.Size = new System.Drawing.Size(560, 52);
            panelServerActions.TabIndex = 1;
            // 
            // lblRoomName
            // 
            lblRoomName.AutoSize = true;
            lblRoomName.Location = new System.Drawing.Point(12, 16);
            lblRoomName.Name = "lblRoomName";
            lblRoomName.Size = new System.Drawing.Size(78, 20);
            lblRoomName.TabIndex = 0;
            lblRoomName.Text = "Ten phong";
            // 
            // txtRoomName
            // 
            txtRoomName.Location = new System.Drawing.Point(96, 12);
            txtRoomName.Name = "txtRoomName";
            txtRoomName.Size = new System.Drawing.Size(180, 27);
            txtRoomName.TabIndex = 1;
            txtRoomName.Text = "Phong cua Dung";
            // 
            // btnAddToRoot
            // 
            btnAddToRoot.Location = new System.Drawing.Point(292, 11);
            btnAddToRoot.Name = "btnAddToRoot";
            btnAddToRoot.Size = new System.Drawing.Size(120, 30);
            btnAddToRoot.TabIndex = 2;
            btnAddToRoot.Text = "Tai len Root";
            btnAddToRoot.UseVisualStyleBackColor = true;
            // 
            // btnToggleServer
            // 
            btnToggleServer.Location = new System.Drawing.Point(426, 11);
            btnToggleServer.Name = "btnToggleServer";
            btnToggleServer.Size = new System.Drawing.Size(120, 30);
            btnToggleServer.TabIndex = 3;
            btnToggleServer.Text = "Bat Server";
            btnToggleServer.UseVisualStyleBackColor = true;
            // 
            // grpClients
            // 
            grpClients.Controls.Add(lvClients);
            grpClients.Dock = System.Windows.Forms.DockStyle.Fill;
            grpClients.Location = new System.Drawing.Point(0, 0);
            grpClients.Name = "grpClients";
            grpClients.Size = new System.Drawing.Size(596, 625);
            grpClients.TabIndex = 0;
            grpClients.TabStop = false;
            grpClients.Text = "May khach dang ket noi";
            // 
            // lvClients
            // 
            lvClients.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colIP, colPerm });
            lvClients.Dock = System.Windows.Forms.DockStyle.Fill;
            lvClients.FullRowSelect = true;
            lvClients.GridLines = true;
            lvClients.Location = new System.Drawing.Point(3, 23);
            lvClients.Name = "lvClients";
            lvClients.Size = new System.Drawing.Size(590, 599);
            lvClients.TabIndex = 0;
            lvClients.UseCompatibleStateImageBehavior = false;
            lvClients.View = System.Windows.Forms.View.Details;
            // 
            // colIP
            // 
            colIP.Text = "IP";
            colIP.Width = 180;
            // 
            // colPerm
            // 
            colPerm.Text = "Quyen";
            colPerm.Width = 180;
            // 
            // panelPermActions
            // 
            panelPermActions.Controls.Add(btnGrantRead);
            panelPermActions.Controls.Add(btnGrantWrite);
            panelPermActions.Controls.Add(btnDenyClient);
            panelPermActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelPermActions.Location = new System.Drawing.Point(0, 625);
            panelPermActions.Name = "panelPermActions";
            panelPermActions.Size = new System.Drawing.Size(596, 52);
            panelPermActions.TabIndex = 1;
            // 
            // btnGrantRead
            // 
            btnGrantRead.Location = new System.Drawing.Point(16, 11);
            btnGrantRead.Name = "btnGrantRead";
            btnGrantRead.Size = new System.Drawing.Size(120, 30);
            btnGrantRead.TabIndex = 0;
            btnGrantRead.Text = "Quyen doc";
            btnGrantRead.UseVisualStyleBackColor = true;
            // 
            // btnGrantWrite
            // 
            btnGrantWrite.Location = new System.Drawing.Point(150, 11);
            btnGrantWrite.Name = "btnGrantWrite";
            btnGrantWrite.Size = new System.Drawing.Size(120, 30);
            btnGrantWrite.TabIndex = 1;
            btnGrantWrite.Text = "Quyen sua";
            btnGrantWrite.UseVisualStyleBackColor = true;
            // 
            // btnDenyClient
            // 
            btnDenyClient.Location = new System.Drawing.Point(284, 11);
            btnDenyClient.Name = "btnDenyClient";
            btnDenyClient.Size = new System.Drawing.Size(120, 30);
            btnDenyClient.TabIndex = 2;
            btnDenyClient.Text = "Chan truy cap";
            btnDenyClient.UseVisualStyleBackColor = true;
            // 
            // panelClient
            // 
            panelClient.Controls.Add(grpRemoteFiles);
            panelClient.Controls.Add(panelConnectBar);
            panelClient.Dock = System.Windows.Forms.DockStyle.Fill;
            panelClient.Location = new System.Drawing.Point(0, 0);
            panelClient.Name = "panelClient";
            panelClient.Padding = new System.Windows.Forms.Padding(12);
            panelClient.Size = new System.Drawing.Size(1184, 701);
            panelClient.TabIndex = 1;
            // 
            // grpRemoteFiles
            // 
            grpRemoteFiles.Controls.Add(tvRemote);
            grpRemoteFiles.Controls.Add(panelClientActions);
            grpRemoteFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            grpRemoteFiles.Location = new System.Drawing.Point(12, 72);
            grpRemoteFiles.Name = "grpRemoteFiles";
            grpRemoteFiles.Size = new System.Drawing.Size(1160, 617);
            grpRemoteFiles.TabIndex = 1;
            grpRemoteFiles.TabStop = false;
            grpRemoteFiles.Text = "Tai nguyen tu xa";
            // 
            // tvRemote
            // 
            tvRemote.Dock = System.Windows.Forms.DockStyle.Fill;
            tvRemote.Location = new System.Drawing.Point(3, 23);
            tvRemote.Name = "tvRemote";
            tvRemote.Size = new System.Drawing.Size(1154, 539);
            tvRemote.TabIndex = 0;
            // 
            // panelClientActions
            // 
            panelClientActions.Controls.Add(btnDownload);
            panelClientActions.Controls.Add(btnUpload);
            panelClientActions.Controls.Add(btnDelete);
            panelClientActions.Controls.Add(btnRename);
            panelClientActions.Controls.Add(btnMkDir);
            panelClientActions.Controls.Add(btnBackRemote);
            panelClientActions.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelClientActions.Location = new System.Drawing.Point(3, 562);
            panelClientActions.Name = "panelClientActions";
            panelClientActions.Size = new System.Drawing.Size(1154, 52);
            panelClientActions.TabIndex = 1;
            // 
            // btnDownload
            // 
            btnDownload.Location = new System.Drawing.Point(16, 11);
            btnDownload.Name = "btnDownload";
            btnDownload.Size = new System.Drawing.Size(100, 30);
            btnDownload.TabIndex = 0;
            btnDownload.Text = "Download";
            btnDownload.UseVisualStyleBackColor = true;
            // 
            // btnUpload
            // 
            btnUpload.Location = new System.Drawing.Point(122, 11);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new System.Drawing.Size(100, 30);
            btnUpload.TabIndex = 1;
            btnUpload.Text = "Upload";
            btnUpload.UseVisualStyleBackColor = true;
            // 
            // btnDelete
            // 
            btnDelete.Location = new System.Drawing.Point(228, 11);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new System.Drawing.Size(100, 30);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Xoa";
            btnDelete.UseVisualStyleBackColor = true;
            // 
            // btnRename
            // 
            btnRename.Location = new System.Drawing.Point(334, 11);
            btnRename.Name = "btnRename";
            btnRename.Size = new System.Drawing.Size(100, 30);
            btnRename.TabIndex = 3;
            btnRename.Text = "Doi ten";
            btnRename.UseVisualStyleBackColor = true;
            // 
            // btnMkDir
            // 
            btnMkDir.Location = new System.Drawing.Point(440, 11);
            btnMkDir.Name = "btnMkDir";
            btnMkDir.Size = new System.Drawing.Size(100, 30);
            btnMkDir.TabIndex = 4;
            btnMkDir.Text = "Thu muc";
            btnMkDir.UseVisualStyleBackColor = true;
            // 
            // btnBackRemote
            // 
            btnBackRemote.Location = new System.Drawing.Point(546, 11);
            btnBackRemote.Name = "btnBackRemote";
            btnBackRemote.Size = new System.Drawing.Size(100, 30);
            btnBackRemote.TabIndex = 5;
            btnBackRemote.Text = "Back";
            btnBackRemote.UseVisualStyleBackColor = true;
            // 
            // panelConnectBar
            // 
            panelConnectBar.Controls.Add(lblConnectTo);
            panelConnectBar.Controls.Add(cboServers);
            panelConnectBar.Controls.Add(lblPort);
            panelConnectBar.Controls.Add(txtPort);
            panelConnectBar.Controls.Add(btnConnect);
            panelConnectBar.Controls.Add(lblConnStatus);
            panelConnectBar.Dock = System.Windows.Forms.DockStyle.Top;
            panelConnectBar.Location = new System.Drawing.Point(12, 12);
            panelConnectBar.Name = "panelConnectBar";
            panelConnectBar.Size = new System.Drawing.Size(1160, 60);
            panelConnectBar.TabIndex = 0;
            // 
            // lblConnectTo
            // 
            lblConnectTo.AutoSize = true;
            lblConnectTo.Location = new System.Drawing.Point(12, 21);
            lblConnectTo.Name = "lblConnectTo";
            lblConnectTo.Size = new System.Drawing.Size(75, 20);
            lblConnectTo.TabIndex = 0;
            lblConnectTo.Text = "Server LAN";
            // 
            // cboServers
            // 
            cboServers.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboServers.FormattingEnabled = true;
            cboServers.Location = new System.Drawing.Point(93, 17);
            cboServers.Name = "cboServers";
            cboServers.Size = new System.Drawing.Size(280, 28);
            cboServers.TabIndex = 1;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new System.Drawing.Point(388, 21);
            lblPort.Name = "lblPort";
            lblPort.Size = new System.Drawing.Size(35, 20);
            lblPort.TabIndex = 2;
            lblPort.Text = "Port";
            // 
            // txtPort
            // 
            txtPort.Location = new System.Drawing.Point(429, 17);
            txtPort.Name = "txtPort";
            txtPort.Size = new System.Drawing.Size(70, 27);
            txtPort.TabIndex = 3;
            txtPort.Text = "8888";
            // 
            // btnConnect
            // 
            btnConnect.Location = new System.Drawing.Point(515, 15);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new System.Drawing.Size(110, 30);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Ket noi";
            btnConnect.UseVisualStyleBackColor = true;
            // 
            // lblConnStatus
            // 
            lblConnStatus.AutoSize = true;
            lblConnStatus.Location = new System.Drawing.Point(642, 21);
            lblConnStatus.Name = "lblConnStatus";
            lblConnStatus.Size = new System.Drawing.Size(99, 20);
            lblConnStatus.TabIndex = 5;
            lblConnStatus.Text = "Chua ket noi";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1184, 761);
            Controls.Add(panelContent);
            Controls.Add(panelTopBar);
            MinimumSize = new System.Drawing.Size(1000, 700);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "NEXUS P2P File Sharing";
            panelTopBar.ResumeLayout(false);
            panelTopBar.PerformLayout();
            panelContent.ResumeLayout(false);
            panelServer.ResumeLayout(false);
            grpLog.ResumeLayout(false);
            splitServer.Panel1.ResumeLayout(false);
            splitServer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitServer).EndInit();
            splitServer.ResumeLayout(false);
            grpSharedFiles.ResumeLayout(false);
            panelServerActions.ResumeLayout(false);
            panelServerActions.PerformLayout();
            grpClients.ResumeLayout(false);
            panelPermActions.ResumeLayout(false);
            panelClient.ResumeLayout(false);
            grpRemoteFiles.ResumeLayout(false);
            panelClientActions.ResumeLayout(false);
            panelConnectBar.ResumeLayout(false);
            panelConnectBar.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelContent;
        private System.Windows.Forms.Panel panelTopBar;
        private System.Windows.Forms.Label lblAppName;
        private System.Windows.Forms.Button btnTabServer;
        private System.Windows.Forms.Button btnTabClient;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panelServer;
        private System.Windows.Forms.SplitContainer splitServer;
        private System.Windows.Forms.GroupBox grpSharedFiles;
        private System.Windows.Forms.TreeView tvLocal;
        private System.Windows.Forms.Label lblSharedPath;
        private System.Windows.Forms.Panel panelServerActions;
        private System.Windows.Forms.Label lblRoomName;
        private System.Windows.Forms.TextBox txtRoomName;
        private System.Windows.Forms.Button btnToggleServer;
        private System.Windows.Forms.Button btnAddToRoot;
        private System.Windows.Forms.GroupBox grpClients;
        private System.Windows.Forms.ListView lvClients;
        private System.Windows.Forms.ColumnHeader colIP;
        private System.Windows.Forms.ColumnHeader colPerm;
        private System.Windows.Forms.Panel panelPermActions;
        private System.Windows.Forms.Button btnGrantRead;
        private System.Windows.Forms.Button btnGrantWrite;
        private System.Windows.Forms.Button btnDenyClient;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.ListView lvLog;
        private System.Windows.Forms.ColumnHeader colTime;
        private System.Windows.Forms.ColumnHeader colAction;
        private System.Windows.Forms.Panel panelClient;
        private System.Windows.Forms.Panel panelConnectBar;
        private System.Windows.Forms.Label lblConnectTo;
        private System.Windows.Forms.ComboBox cboServers;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblConnStatus;
        private System.Windows.Forms.GroupBox grpRemoteFiles;
        private System.Windows.Forms.TreeView tvRemote;
        private System.Windows.Forms.Panel panelClientActions;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.Button btnMkDir;
        private System.Windows.Forms.Button btnBackRemote;
    }
}
