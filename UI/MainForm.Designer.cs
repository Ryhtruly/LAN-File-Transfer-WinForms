namespace P2PFileSharingApp.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ── Controls ──
            this.panelTopBar         = new System.Windows.Forms.Panel();
            this.lblAppName          = new System.Windows.Forms.Label();
            this.btnTabServer        = new System.Windows.Forms.Button();
            this.btnTabClient        = new System.Windows.Forms.Button();
            this.lblStatus           = new System.Windows.Forms.Label();
            this.panelContent        = new System.Windows.Forms.Panel();

            this.panelServer         = new System.Windows.Forms.Panel();
            this.splitServer         = new System.Windows.Forms.SplitContainer();
            this.grpSharedFiles      = new System.Windows.Forms.GroupBox();
            this.tvLocal             = new System.Windows.Forms.TreeView();
            this.panelServerActions  = new System.Windows.Forms.Panel();
            this.btnToggleServer     = new System.Windows.Forms.Button();
            this.btnAddToRoot        = new System.Windows.Forms.Button();
            this.lblSharedPath       = new System.Windows.Forms.Label();
            this.grpClients          = new System.Windows.Forms.GroupBox();
            this.lvClients           = new System.Windows.Forms.ListView();
            this.colIP               = new System.Windows.Forms.ColumnHeader();
            this.colPerm             = new System.Windows.Forms.ColumnHeader();
            this.panelPermActions    = new System.Windows.Forms.Panel();
            this.btnGrantRead        = new System.Windows.Forms.Button();
            this.btnGrantWrite       = new System.Windows.Forms.Button();
            this.btnDenyClient       = new System.Windows.Forms.Button();
            this.grpLog              = new System.Windows.Forms.GroupBox();
            this.lvLog               = new System.Windows.Forms.ListView();
            this.colTime             = new System.Windows.Forms.ColumnHeader();
            this.colAction           = new System.Windows.Forms.ColumnHeader();

            this.panelClient         = new System.Windows.Forms.Panel();
            this.panelConnectBar     = new System.Windows.Forms.Panel();
            this.lblConnectTo        = new System.Windows.Forms.Label();
            this.txtIP               = new System.Windows.Forms.TextBox();
            this.lblPort             = new System.Windows.Forms.Label();
            this.txtPort             = new System.Windows.Forms.TextBox();
            this.btnConnect          = new System.Windows.Forms.Button();
            this.lblConnStatus       = new System.Windows.Forms.Label();
            this.grpRemoteFiles      = new System.Windows.Forms.GroupBox();
            this.tvRemote            = new System.Windows.Forms.TreeView();
            this.panelClientActions  = new System.Windows.Forms.Panel();
            this.btnDownload         = new System.Windows.Forms.Button();
            this.btnUpload           = new System.Windows.Forms.Button();
            this.btnDelete           = new System.Windows.Forms.Button();
            this.btnRename           = new System.Windows.Forms.Button();
            this.btnMkDir            = new System.Windows.Forms.Button();
            this.btnBackRemote       = new System.Windows.Forms.Button();

            this.panelTopBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitServer)).BeginInit();
            this.splitServer.Panel1.SuspendLayout();
            this.splitServer.Panel2.SuspendLayout();
            this.splitServer.SuspendLayout();
            this.SuspendLayout();

            // ──────────────────────────────────
            // FORM
            // ──────────────────────────────────
            this.Text            = "NEXUS P2P File Sharing";
            this.Size            = new System.Drawing.Size(1100, 720);
            this.MinimumSize     = new System.Drawing.Size(900, 600);
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor       = System.Drawing.Color.FromArgb(248, 249, 250);
            this.Font            = new System.Drawing.Font("Segoe UI", 9.5f);
            this.AutoScroll      = true;
            this.Controls.Add(this.panelTopBar);
            this.Controls.Add(this.panelContent);

            // Content wrapper (Bỏ Dock=Fill, dùng Anchor để né TopBar)
            this.panelContent.Location = new System.Drawing.Point(0, 56);
            this.panelContent.Size = new System.Drawing.Size(1100, 720 - 56);
            this.panelContent.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.panelContent.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.panelContent.AutoScroll = true;
            this.panelContent.Controls.Add(this.panelClient);
            this.panelContent.Controls.Add(this.panelServer);

            // ──────────────────────────────────
            // TOP BAR (#1A73E8 Google Blue)
            // ──────────────────────────────────
            this.panelTopBar.BackColor  = System.Drawing.Color.FromArgb(26, 115, 232);
            this.panelTopBar.Dock       = System.Windows.Forms.DockStyle.Top;
            this.panelTopBar.Height     = 56;
            this.panelTopBar.Padding    = new System.Windows.Forms.Padding(16, 0, 16, 0);
            this.panelTopBar.Controls.Add(this.lblAppName);
            this.panelTopBar.Controls.Add(this.btnTabClient);
            this.panelTopBar.Controls.Add(this.btnTabServer);
            this.panelTopBar.Controls.Add(this.lblStatus);

            // App Name
            this.lblAppName.Text      = "⚡ NEXUS";
            this.lblAppName.ForeColor = System.Drawing.Color.White;
            this.lblAppName.Font      = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
            this.lblAppName.AutoSize  = true;
            this.lblAppName.Location  = new System.Drawing.Point(16, 14);

            // Tab Buttons (Server)
            this.btnTabServer.Text      = "My Server";
            this.btnTabServer.ForeColor = System.Drawing.Color.White;
            this.btnTabServer.BackColor = System.Drawing.Color.FromArgb(0, 0, 0, 60);
            this.btnTabServer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabServer.FlatAppearance.BorderSize  = 0;
            this.btnTabServer.Font      = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.btnTabServer.Size      = new System.Drawing.Size(150, 38);
            this.btnTabServer.Location  = new System.Drawing.Point(130, 9);
            this.btnTabServer.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnTabServer.Name      = "btnTabServer";

            // Tab Buttons (Client)
            this.btnTabClient.Text      = "Connect";
            this.btnTabClient.ForeColor = System.Drawing.Color.FromArgb(200, 232, 255);
            this.btnTabClient.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
            this.btnTabClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTabClient.FlatAppearance.BorderSize  = 0;
            this.btnTabClient.Font      = new System.Drawing.Font("Segoe UI", 10f);
            this.btnTabClient.Size      = new System.Drawing.Size(130, 38);
            this.btnTabClient.Location  = new System.Drawing.Point(288, 9);
            this.btnTabClient.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnTabClient.Name      = "btnTabClient";

            // Status label (right side)
            this.lblStatus.Text      = "● Server: Đang tắt";
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(255, 180, 180);
            this.lblStatus.Font      = new System.Drawing.Font("Segoe UI", 9f);
            this.lblStatus.AutoSize  = true;
            this.lblStatus.Anchor    = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lblStatus.Location  = new System.Drawing.Point(870, 19);

            // ──────────────────────────────────
            // PANEL SERVER (Tab 1)
            // ──────────────────────────────────
            this.panelServer.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.panelServer.MinimumSize = new System.Drawing.Size(900, 850);
            this.panelServer.Padding   = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.panelServer.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.panelServer.Controls.Add(this.splitServer);

            // SplitContainer
            this.splitServer.Dock              = System.Windows.Forms.DockStyle.Fill;
            this.splitServer.SplitterDistance  = 450;
            this.splitServer.SplitterWidth     = 6;
            this.splitServer.BackColor         = System.Drawing.Color.FromArgb(218, 220, 224);
            this.splitServer.Panel1.Controls.Add(this.panelServerActions);
            this.splitServer.Panel1.Controls.Add(this.grpSharedFiles);
            this.splitServer.Panel2.Controls.Add(this.panelPermActions);
            this.splitServer.Panel2.Controls.Add(this.grpClients);

            // GroupBox: Shared Files
            this.grpSharedFiles.Text      = "📁  Drive của tôi (Shared Folder)";
            this.grpSharedFiles.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.grpSharedFiles.ForeColor = System.Drawing.Color.FromArgb(95, 99, 104);
            this.grpSharedFiles.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.grpSharedFiles.Padding   = new System.Windows.Forms.Padding(8);
            this.grpSharedFiles.Controls.Add(this.tvLocal);
            this.grpSharedFiles.Controls.Add(this.lblSharedPath);

            // lblSharedPath
            this.lblSharedPath.Text      = "Chưa chọn thư mục chia sẻ";
            this.lblSharedPath.ForeColor = System.Drawing.Color.FromArgb(95, 99, 104);
            this.lblSharedPath.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Italic);
            this.lblSharedPath.Dock      = System.Windows.Forms.DockStyle.Bottom;
            this.lblSharedPath.Height    = 22;
            this.lblSharedPath.Padding   = new System.Windows.Forms.Padding(4, 4, 0, 0);

            // tvLocal
            this.tvLocal.Dock            = System.Windows.Forms.DockStyle.Fill;
            this.tvLocal.BorderStyle     = System.Windows.Forms.BorderStyle.None;
            this.tvLocal.BackColor       = System.Drawing.Color.White;
            this.tvLocal.ForeColor       = System.Drawing.Color.FromArgb(32, 33, 36);
            this.tvLocal.Font            = new System.Drawing.Font("Segoe UI", 9.5f);
            this.tvLocal.ItemHeight      = 24;
            this.tvLocal.ShowLines       = false;

            // panelServerActions
            this.panelServerActions.Dock       = System.Windows.Forms.DockStyle.Bottom;
            this.panelServerActions.Height     = 52;
            this.panelServerActions.BackColor  = System.Drawing.Color.White;
            this.panelServerActions.Padding    = new System.Windows.Forms.Padding(4);
            this.panelServerActions.Controls.Add(this.btnToggleServer);
            this.panelServerActions.Controls.Add(this.btnAddToRoot);

            this.btnAddToRoot.Text         = "➕  Tải lên Root";
            this.btnAddToRoot.BackColor    = System.Drawing.Color.FromArgb(26, 115, 232);
            this.btnAddToRoot.ForeColor    = System.Drawing.Color.White;
            this.btnAddToRoot.FlatStyle    = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddToRoot.FlatAppearance.BorderSize = 0;
            this.btnAddToRoot.Font         = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.btnAddToRoot.Size         = new System.Drawing.Size(180, 36);
            this.btnAddToRoot.Location     = new System.Drawing.Point(4, 8);
            this.btnAddToRoot.Cursor       = System.Windows.Forms.Cursors.Hand;
            this.btnAddToRoot.Name         = "btnAddToRoot";

            this.btnToggleServer.Text      = "▶  Bật Server";
            this.btnToggleServer.BackColor = System.Drawing.Color.FromArgb(52, 168, 83);
            this.btnToggleServer.ForeColor = System.Drawing.Color.White;
            this.btnToggleServer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleServer.FlatAppearance.BorderSize = 0;
            this.btnToggleServer.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.btnToggleServer.Size      = new System.Drawing.Size(140, 36);
            this.btnToggleServer.Location  = new System.Drawing.Point(192, 8);
            this.btnToggleServer.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnToggleServer.Name      = "btnToggleServer";

            // GroupBox: Clients
            this.grpClients.Text      = "👥  Máy Khách Đang Kết Nối";
            this.grpClients.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.grpClients.ForeColor = System.Drawing.Color.FromArgb(95, 99, 104);
            this.grpClients.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.grpClients.Padding   = new System.Windows.Forms.Padding(8);
            this.grpClients.Controls.Add(this.lvClients);
            this.grpClients.Controls.Add(this.panelPermActions);

            // lvClients
            this.lvClients.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lvClients.View      = System.Windows.Forms.View.Details;
            this.lvClients.FullRowSelect   = true;
            this.lvClients.GridLines       = true;
            this.lvClients.BorderStyle     = System.Windows.Forms.BorderStyle.None;
            this.lvClients.BackColor       = System.Drawing.Color.White;
            this.lvClients.Font            = new System.Drawing.Font("Segoe UI", 9.5f);
            this.lvClients.Columns.Add(this.colIP);
            this.lvClients.Columns.Add(this.colPerm);
            this.colIP.Text   = "Địa chỉ IP";
            this.colIP.Width  = 140;
            this.colPerm.Text = "Quyền Truy Cập";
            this.colPerm.Width = 130;

            // panelPermActions
            this.panelPermActions.Dock      = System.Windows.Forms.DockStyle.Bottom;
            this.panelPermActions.Height    = 52;
            this.panelPermActions.BackColor = System.Drawing.Color.White;
            this.panelPermActions.Padding   = new System.Windows.Forms.Padding(4);
            this.panelPermActions.Controls.Add(this.btnGrantRead);
            this.panelPermActions.Controls.Add(this.btnGrantWrite);
            this.panelPermActions.Controls.Add(this.btnDenyClient);

            this.btnGrantRead.Text      = "👁 Quyền Xem/Đọc";
            this.btnGrantRead.BackColor = System.Drawing.Color.FromArgb(251, 188, 4);
            this.btnGrantRead.ForeColor = System.Drawing.Color.FromArgb(32, 33, 36);
            this.btnGrantRead.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGrantRead.FlatAppearance.BorderSize = 0;
            this.btnGrantRead.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold);
            this.btnGrantRead.Size      = new System.Drawing.Size(120, 36);
            this.btnGrantRead.Location  = new System.Drawing.Point(4, 8);
            this.btnGrantRead.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnGrantRead.Name      = "btnGrantRead";

            this.btnGrantWrite.Text      = "✏ Quyền Chỉnh Sửa";
            this.btnGrantWrite.BackColor = System.Drawing.Color.FromArgb(52, 168, 83);
            this.btnGrantWrite.ForeColor = System.Drawing.Color.White;
            this.btnGrantWrite.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGrantWrite.FlatAppearance.BorderSize = 0;
            this.btnGrantWrite.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold);
            this.btnGrantWrite.Size      = new System.Drawing.Size(130, 36);
            this.btnGrantWrite.Location  = new System.Drawing.Point(128, 8);
            this.btnGrantWrite.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnGrantWrite.Name      = "btnGrantWrite";

            this.btnDenyClient.Text      = "🚫 Chặn Truy Cập";
            this.btnDenyClient.BackColor = System.Drawing.Color.FromArgb(234, 67, 53);
            this.btnDenyClient.ForeColor = System.Drawing.Color.White;
            this.btnDenyClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDenyClient.FlatAppearance.BorderSize = 0;
            this.btnDenyClient.Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold);
            this.btnDenyClient.Size      = new System.Drawing.Size(130, 36);
            this.btnDenyClient.Location  = new System.Drawing.Point(264, 8);
            this.btnDenyClient.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnDenyClient.Name      = "btnDenyClient";

            this.panelServer.Controls.Add(this.grpLog);

            // GroupBox: Log
            this.grpLog.Text      = "📋  Nhật Ký Hoạt Động";
            this.grpLog.Dock      = System.Windows.Forms.DockStyle.Bottom;
            this.grpLog.Height    = 300;
            this.grpLog.ForeColor = System.Drawing.Color.FromArgb(95, 99, 104);
            this.grpLog.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.grpLog.Padding   = new System.Windows.Forms.Padding(8);
            this.grpLog.Controls.Add(this.lvLog);

            this.lvLog.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lvLog.View      = System.Windows.Forms.View.Details;
            this.lvLog.FullRowSelect   = true;
            this.lvLog.GridLines       = true;
            this.lvLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvLog.BackColor = System.Drawing.Color.White;
            this.lvLog.Font      = new System.Drawing.Font("Segoe UI", 9.5f);
            this.lvLog.Columns.Add(this.colTime);
            this.lvLog.Columns.Add(this.colAction);
            this.colTime.Text    = "Thời gian";
            this.colTime.Width   = 120;
            this.colAction.Text  = "Sự kiện";
            this.colAction.Width = 600;

            // ──────────────────────────────────
            // PANEL CLIENT (Tab 2)
            // ──────────────────────────────────
            this.panelClient.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.panelClient.MinimumSize = new System.Drawing.Size(900, 850);
            this.panelClient.Padding   = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.panelClient.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.panelClient.Visible   = false;
            this.panelClient.Controls.Add(this.panelConnectBar);
            this.panelClient.Controls.Add(this.grpRemoteFiles);

            // panelConnectBar
            this.panelConnectBar.Dock       = System.Windows.Forms.DockStyle.Top;
            this.panelConnectBar.Height     = 60;
            this.panelConnectBar.BackColor  = System.Drawing.Color.White;
            this.panelConnectBar.Padding    = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.panelConnectBar.Controls.Add(this.lblConnectTo);
            this.panelConnectBar.Controls.Add(this.txtIP);
            this.panelConnectBar.Controls.Add(this.lblPort);
            this.panelConnectBar.Controls.Add(this.txtPort);
            this.panelConnectBar.Controls.Add(this.btnConnect);
            this.panelConnectBar.Controls.Add(this.lblConnStatus);

            this.lblConnectTo.Text      = "Địa chỉ IP:";
            this.lblConnectTo.ForeColor = System.Drawing.Color.FromArgb(60, 64, 67);
            this.lblConnectTo.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.lblConnectTo.AutoSize  = true;
            this.lblConnectTo.Location  = new System.Drawing.Point(8, 22);

            this.txtIP.Location    = new System.Drawing.Point(90, 18);
            this.txtIP.Size        = new System.Drawing.Size(200, 28);
            this.txtIP.Text        = "127.0.0.1";
            this.txtIP.Font        = new System.Drawing.Font("Segoe UI", 10.5f);
            this.txtIP.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtIP.Name        = "txtIP";

            this.lblPort.Text      = "Port:";
            this.lblPort.ForeColor = System.Drawing.Color.FromArgb(60, 64, 67);
            this.lblPort.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.lblPort.AutoSize  = true;
            this.lblPort.Location  = new System.Drawing.Point(300, 22);

            this.txtPort.Location    = new System.Drawing.Point(340, 18);
            this.txtPort.Size        = new System.Drawing.Size(70, 28);
            this.txtPort.Text        = "8888";
            this.txtPort.Font        = new System.Drawing.Font("Segoe UI", 10.5f);
            this.txtPort.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtPort.Name        = "txtPort";

            this.btnConnect.Text      = "🔌  Kết Nối";
            this.btnConnect.BackColor = System.Drawing.Color.FromArgb(26, 115, 232);
            this.btnConnect.ForeColor = System.Drawing.Color.White;
            this.btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnect.FlatAppearance.BorderSize = 0;
            this.btnConnect.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.btnConnect.Size      = new System.Drawing.Size(120, 36);
            this.btnConnect.Location  = new System.Drawing.Point(424, 13);
            this.btnConnect.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnConnect.Name      = "btnConnect";

            this.lblConnStatus.Text      = "● Chưa kết nối";
            this.lblConnStatus.ForeColor = System.Drawing.Color.FromArgb(234, 67, 53);
            this.lblConnStatus.Font      = new System.Drawing.Font("Segoe UI", 9f);
            this.lblConnStatus.AutoSize  = true;
            this.lblConnStatus.Location  = new System.Drawing.Point(560, 22);

            // GroupBox: Remote Files
            this.grpRemoteFiles.Text      = "🌐  Tài Nguyên Từ Xa";
            this.grpRemoteFiles.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.grpRemoteFiles.ForeColor = System.Drawing.Color.FromArgb(95, 99, 104);
            this.grpRemoteFiles.Font      = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            this.grpRemoteFiles.Padding   = new System.Windows.Forms.Padding(8);
            this.grpRemoteFiles.Controls.Add(this.tvRemote);
            this.grpRemoteFiles.Controls.Add(this.panelClientActions);

            this.tvRemote.Dock        = System.Windows.Forms.DockStyle.Fill;
            this.tvRemote.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tvRemote.BackColor   = System.Drawing.Color.White;
            this.tvRemote.ForeColor   = System.Drawing.Color.FromArgb(32, 33, 36);
            this.tvRemote.Font        = new System.Drawing.Font("Segoe UI", 9.5f);
            this.tvRemote.ItemHeight  = 24;
            this.tvRemote.ShowLines   = false;
            this.tvRemote.Name        = "tvRemote";

            // panelClientActions
            this.panelClientActions.Dock      = System.Windows.Forms.DockStyle.Bottom;
            this.panelClientActions.Height    = 56;
            this.panelClientActions.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            this.panelClientActions.Padding   = new System.Windows.Forms.Padding(4, 8, 4, 4);
            this.panelClientActions.Controls.Add(this.btnDownload);
            this.panelClientActions.Controls.Add(this.btnUpload);
            this.panelClientActions.Controls.Add(this.btnDelete);
            this.panelClientActions.Controls.Add(this.btnRename);
            this.panelClientActions.Controls.Add(this.btnMkDir);
            this.panelClientActions.Controls.Add(this.btnBackRemote);

            MakeActionButton(this.btnDownload, "⬇  Download", System.Drawing.Color.FromArgb(26, 115, 232),  System.Drawing.Color.White, 4);
            MakeActionButton(this.btnUpload,   "⬆  Upload",   System.Drawing.Color.FromArgb(52, 168, 83),   System.Drawing.Color.White, 126);
            MakeActionButton(this.btnDelete,   "🗑  Xóa",     System.Drawing.Color.FromArgb(234, 67, 53),   System.Drawing.Color.White, 248);
            MakeActionButton(this.btnRename,   "✏  Đổi Tên", System.Drawing.Color.FromArgb(251, 188, 4),   System.Drawing.Color.FromArgb(32,33,36), 370);
            MakeActionButton(this.btnMkDir,    "📁  Thư Mục",  System.Drawing.Color.FromArgb(95, 99, 104), System.Drawing.Color.White, 492);
            MakeActionButton(this.btnBackRemote, "⬅ Back",   System.Drawing.Color.FromArgb(60, 64, 67),  System.Drawing.Color.White, 614);

            this.btnDownload.Name = "btnDownload";
            this.btnUpload.Name   = "btnUpload";
            this.btnDelete.Name   = "btnDelete";
            this.btnRename.Name   = "btnRename";
            this.btnMkDir.Name    = "btnMkDir";
            this.btnBackRemote.Name = "btnBackRemote";

            // ──────────────────────────────────
            // RESUME LAYOUT
            // ──────────────────────────────────
            this.panelTopBar.ResumeLayout(false);
            this.panelTopBar.PerformLayout();
            this.splitServer.Panel1.ResumeLayout(false);
            this.splitServer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitServer)).EndInit();
            this.splitServer.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private static void MakeActionButton(System.Windows.Forms.Button btn, string text,
            System.Drawing.Color bg, System.Drawing.Color fg, int x)
        {
            btn.Text      = text;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font      = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            btn.Size      = new System.Drawing.Size(116, 36);
            btn.Location  = new System.Drawing.Point(x, 4);
            btn.Cursor    = System.Windows.Forms.Cursors.Hand;
        }

        #endregion

        // ── Field Declarations ──
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
        private System.Windows.Forms.TextBox txtIP;
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
