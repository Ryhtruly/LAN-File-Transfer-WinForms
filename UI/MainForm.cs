using System;
using System.IO;
using System.Windows.Forms;
using P2PFileSharingApp.Core;
using P2PFileSharingApp.Network;

namespace P2PFileSharingApp.UI
{
    public partial class MainForm : Form
    {
        private readonly P2PServer _server = new P2PServer();
        private P2PClient? _client;
        private bool _serverRunning = false;
        private string _currentRemotePath = "";
        private FileSystemWatcher? _watcher;

        public MainForm()
        {
            // ── Setup UI ──
            InitializeComponent();

            // ── Setup Local Root Directory ──
            string rootPath = Path.Combine(Application.StartupPath, "P2P_Shared_Root");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);
            _server.SharedFolderPath = rootPath;
            lblSharedPath.Text = "📂 " + rootPath;

            // ── FileSystemWatcher (Auto-Sync Local Tree) ──
            _watcher = new FileSystemWatcher(rootPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcher.Created += (s, e) => this.Invoke(new Action(RefreshLocalTree));
            _watcher.Deleted += (s, e) => this.Invoke(new Action(RefreshLocalTree));
            _watcher.Renamed += (s, e) => this.Invoke(new Action(RefreshLocalTree));
            RefreshLocalTree();

            // ── Tab Events ──
            btnTabServer.Click += (_, __) => ShowTab(true);
            btnTabClient.Click += (_, __) => ShowTab(false);

            // ── Server Tree Events ──
            btnAddToRoot.Click           += BtnAddToRoot_Click;
            btnToggleServer.Click        += BtnToggleServer_Click;
            btnGrantRead.Click     += BtnGrantRead_Click;
            btnGrantWrite.Click    += BtnGrantWrite_Click;
            btnDenyClient.Click    += BtnDenyClient_Click;

            // ── Client Tab Actions ──
            btnConnect.Click       += BtnConnect_Click;
            btnDisconnect.Click    += BtnDisconnect_Click;
            btnRefreshRemote.Click += BtnRefreshRemote_Click;
            btnDownload.Click  += BtnDownload_Click;
            btnUpload.Click    += BtnUpload_Click;
            btnDelete.Click    += BtnDelete_Click;
            btnRename.Click    += BtnRename_Click;
            btnMkDir.Click     += BtnMkDir_Click;

            // ── Server Events ──
            _server.OnLog                += msg => AppendLog(msg);
            _server.OnClientConnected    += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;

            // ── Client Tree Events ──
            tvRemote.NodeMouseDoubleClick += TvRemote_NodeMouseDoubleClick;
            btnBackRemote.Click           += BtnBackRemote_Click;

            // ── Clean up on close ──
            this.FormClosing += (_, __) => { _watcher?.Dispose(); _server.Stop(); _client?.Disconnect(); };
        }

        // ═══════════════════════════════════════
        // TAB SWITCHING
        // ═══════════════════════════════════════

        private void ShowTab(bool server)
        {
            panelServer.Visible = server;
            panelClient.Visible = !server;

            // Active tab styling
            btnTabServer.Font      = new System.Drawing.Font("Segoe UI", 10f, server ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
            btnTabServer.BackColor = server ? System.Drawing.Color.FromArgb(0, 0, 0, 60) : System.Drawing.Color.Transparent;
            btnTabServer.ForeColor = System.Drawing.Color.White;

            btnTabClient.Font      = new System.Drawing.Font("Segoe UI", 10f, !server ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
            btnTabClient.BackColor = !server ? System.Drawing.Color.FromArgb(0, 0, 0, 60) : System.Drawing.Color.Transparent;
            btnTabClient.ForeColor = !server ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(200, 232, 255);
        }

        // ═══════════════════════════════════════
        // SERVER TAB LOGIC
        // ═══════════════════════════════════════

        private void BtnAddToRoot_Click(object? sender, EventArgs e)
        {
            var choice = MessageBox.Show("Bạn muốn thêm 1 File hay Cả Thư Mục vào Root?\n\nChọn [Yes] để thêm Thư Mục\nChọn [No] để thêm File", 
                "Thêm vào Root", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (choice == DialogResult.Cancel) return;

            try
            {
                if (choice == DialogResult.Yes)
                {
                    using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục để thêm vào Root" };
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        string destDir = Path.Combine(_server.SharedFolderPath, new DirectoryInfo(fbd.SelectedPath).Name);
                        CopyDirectory(fbd.SelectedPath, destDir);
                        MessageBox.Show("Đã copy thư mục vào Root thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    using var ofd = new OpenFileDialog { Title = "Chọn file để thêm vào Root", Multiselect = true };
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        foreach (var file in ofd.FileNames)
                        {
                            string destFile = Path.Combine(_server.SharedFolderPath, Path.GetFileName(file));
                            File.Copy(file, destFile, true);
                        }
                        MessageBox.Show("Đã copy file vào Root thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi copy: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
                file.CopyTo(Path.Combine(destinationDir, file.Name), true);

            foreach (var subDir in dir.GetDirectories())
                CopyDirectory(subDir.FullName, Path.Combine(destinationDir, subDir.Name));
        }

        private void BtnToggleServer_Click(object? sender, EventArgs e)
        {
            if (!_serverRunning)
            {
                _server.Start(8888);
                _serverRunning = true;
                btnToggleServer.Text      = "⏹  Tắt Server";
                btnToggleServer.BackColor = System.Drawing.Color.FromArgb(234, 67, 53);
                UpdateServerStatus(true);
            }
            else
            {
                _server.Stop();
                _serverRunning = false;
                btnToggleServer.Text      = "▶  Bật Server";
                btnToggleServer.BackColor = System.Drawing.Color.FromArgb(52, 168, 83);
                lvClients.Items.Clear();
                UpdateServerStatus(false);
            }
        }

        private void OnClientConnected(string ip)
        {
            this.BeginInvoke(new Action(() =>
            {
                // Kiểm tra IP đã trong danh sách chưa
                foreach (ListViewItem existing in lvClients.Items)
                    if (existing.Text == ip) { RefreshClientPermission(existing); return; }

                var item = new ListViewItem(ip);
                item.SubItems.Add(GetPermLabel(PermissionManager.GetPermission(ip)));
                lvClients.Items.Add(item);
                AppendLog($"🟢 Client kết nối: {ip}");
            }));
        }

        private void OnClientDisconnected(string ip)
        {
            this.BeginInvoke(new Action(() =>
            {
                foreach (ListViewItem item in lvClients.Items)
                    if (item.Text == ip) { lvClients.Items.Remove(item); break; }
                AppendLog($"🔴 Client ngắt kết nối: {ip}");
            }));
        }

        private void BtnGrantRead_Click(object? sender, EventArgs e)
            => SetSelectedClientPermission(PermissionLevel.ReadOnly);

        private void BtnGrantWrite_Click(object? sender, EventArgs e)
            => SetSelectedClientPermission(PermissionLevel.ReadWrite);

        private void BtnDenyClient_Click(object? sender, EventArgs e)
            => SetSelectedClientPermission(PermissionLevel.Denied);

        private void SetSelectedClientPermission(PermissionLevel level)
        {
            if (lvClients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một máy khách trong danh sách.", "Chưa chọn",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var item = lvClients.SelectedItems[0];
            string ip = item.Text;
            PermissionManager.SetPermission(ip, level);
            item.SubItems[1].Text = GetPermLabel(level);
            AppendLog($"🔑 Đã đặt quyền [{GetPermLabel(level)}] cho IP: {ip}");
            
            // Nếu chặn, lập tức ngắt kết nối
            if (level == PermissionLevel.Denied)
            {
                _server.DisconnectClient(ip);
                AppendLog($"🚫 Đã ngắt kết nối bắt buộc: {ip}");
            }
        }

        private static void RefreshClientPermission(ListViewItem item)
        {
            item.SubItems[1].Text = GetPermLabel(PermissionManager.GetPermission(item.Text));
        }

        private static string GetPermLabel(PermissionLevel lvl) => lvl switch
        {
            PermissionLevel.ReadOnly  => "👁 Quyền Xem/Đọc",
            PermissionLevel.ReadWrite => "✏ Quyền Chỉnh Sửa",
            PermissionLevel.Denied    => "🚫 Bị Chặn",
            _ => "❓ Không rõ"
        };

        // ═══════════════════════════════════════
        // CLIENT TAB LOGIC
        // ═══════════════════════════════════════

        private void UpdateClientUI(bool connected)
        {
            btnConnect.Enabled = !connected;
            btnConnect.Text = "🔌  Kết Nối";
            btnDisconnect.Enabled = connected;
            btnRefreshRemote.Enabled = connected;

            if (!connected)
            {
                lblConnStatus.Text = "● Chưa kết nối";
                lblConnStatus.ForeColor = System.Drawing.Color.FromArgb(234, 67, 53);
                tvRemote.Nodes.Clear();
            }
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            _client?.Disconnect();
            _client = new P2PClient();
            _client.OnLog += msg => AppendLog("[CLIENT] " + msg);
            _currentRemotePath = "";

            string ip = txtIP.Text.Trim();
            if (!int.TryParse(txtPort.Text.Trim(), out int port)) port = 8888;

            btnConnect.Enabled = false;
            lblConnStatus.Text      = "● Đang kết nối...";
            lblConnStatus.ForeColor = System.Drawing.Color.FromArgb(251, 188, 4);

            var (ok, msg) = await _client.ConnectAsync(ip, port);

            if (ok)
            {
                lblConnStatus.Text      = $"● Đã kết nối: {ip}:{port}";
                lblConnStatus.ForeColor = System.Drawing.Color.FromArgb(52, 168, 83);
                UpdateClientUI(true);
                await RefreshRemoteTreeAsync();
            }
            else
            {
                UpdateClientUI(false);
                MessageBox.Show("Không thể kết nối:\n" + msg, "Lỗi kết nối",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            _client?.Disconnect();
            UpdateClientUI(false);
            AppendLog("[CLIENT] Đã ngắt kết nối.");
        }

        private async void BtnRefreshRemote_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected()) return;
            btnRefreshRemote.Enabled = false;
            await RefreshRemoteTreeAsync();
            btnRefreshRemote.Enabled = true;
        }

        private async void BtnDownload_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || !EnsureItemSelected(tvRemote, out string itemName, out bool isFile)) return;
            if (!isFile)
            {
                MessageBox.Show("Chỉ hỗ trợ tải File, không hỗ trợ tải cả Thư mục.", "Chưa hỗ trợ", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? itemName : $"{_currentRemotePath}/{itemName}";

            // Chọn nơi lưu file
            using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục lưu file" };
            if (fbd.ShowDialog() != DialogResult.OK) return;

            SetBusy(btnDownload, "⏳ Đang tải...", true);
            var (ok, msg) = await _client!.DownloadAsync(targetPath, fbd.SelectedPath);
            SetBusy(btnDownload, "⬇  Download", false);

            MessageBox.Show(ok ? $"✅ {msg}" : $"❌ {msg}",
                ok ? "Tải thành công" : "Tải thất bại",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }

        private async void BtnUpload_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected()) return;

            var choice = MessageBox.Show("Bạn muốn tải lên 1 File hay Cả Thư Mục?\n\nChọn [Yes] để tải Thư Mục\nChọn [No] để tải File", 
                "Tải lên", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (choice == DialogResult.Cancel) return;

            bool isOk = false;
            string message = "";

            if (choice == DialogResult.Yes)
            {
                using var fbd = new FolderBrowserDialog { Description = "Chọn thư mục để upload" };
                if (fbd.ShowDialog() != DialogResult.OK) return;

                SetBusy(btnUpload, "⏳ Đang upload...", true);
                var res = await _client!.UploadDirectoryAsync(fbd.SelectedPath, _currentRemotePath);
                isOk = res.ok;
                message = res.msg;
            }
            else
            {
                using var ofd = new OpenFileDialog { Title = "Chọn file để upload", Multiselect = false };
                if (ofd.ShowDialog() != DialogResult.OK) return;

                SetBusy(btnUpload, "⏳ Đang upload...", true);
                var res = await _client!.UploadAsync(ofd.FileName, _currentRemotePath);
                isOk = res.ok;
                message = res.msg;
            }

            SetBusy(btnUpload, "⬆  Upload", false);

            MessageBox.Show(isOk ? $"✅ {message}" : $"❌ {message}",
                isOk ? "Upload thành công" : "Upload thất bại",
                MessageBoxButtons.OK, isOk ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            if (isOk) await RefreshRemoteTreeAsync();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || !EnsureItemSelected(tvRemote, out string itemName, out _)) return;

            var confirm = MessageBox.Show($"Bạn có chắc muốn xóa:\n{itemName}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? itemName : $"{_currentRemotePath}/{itemName}";

            SetBusy(btnDelete, "⏳ Đang xóa...", true);
            var (ok, msg) = await _client!.DeleteAsync(targetPath);
            SetBusy(btnDelete, "🗑  Xóa", false);

            MessageBox.Show(ok ? $"✅ {msg}" : $"❌ {msg}",
                ok ? "Xóa thành công" : "Thất bại",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            if (ok) await RefreshRemoteTreeAsync();
        }

        private async void BtnRename_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || !EnsureItemSelected(tvRemote, out string itemName, out _)) return;

            string? newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập tên mới:", "Đổi tên", itemName);
            if (string.IsNullOrWhiteSpace(newName) || newName == itemName) return;

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? itemName : $"{_currentRemotePath}/{itemName}";

            SetBusy(btnRename, "⏳ Đang đổi...", true);
            var (ok, msg) = await _client!.RenameAsync(targetPath, newName);
            SetBusy(btnRename, "✏  Đổi Tên", false);

            MessageBox.Show(ok ? $"✅ {msg}" : $"❌ {msg}",
                ok ? "Đổi tên thành công" : "Thất bại",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            if (ok) await RefreshRemoteTreeAsync();
        }

        private async void BtnMkDir_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected()) return;

            string? folderName = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập tên thư mục mới:", "Tạo thư mục mới", "New Folder");
            if (string.IsNullOrWhiteSpace(folderName)) return;

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? folderName : $"{_currentRemotePath}/{folderName}";

            SetBusy(btnMkDir, "⏳...", true);
            var (ok, msg) = await _client!.MkDirAsync(targetPath);
            SetBusy(btnMkDir, "📁  Thư Mục", false);

            MessageBox.Show(ok ? $"✅ {msg}" : $"❌ {msg}",
                ok ? "Tạo thành công" : "Thất bại",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);

            if (ok) await RefreshRemoteTreeAsync();
        }

        private async void TvRemote_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (!EnsureClientConnected()) return;
            if (e.Node == null || !e.Node.Text.StartsWith("📁 ")) return;

            string folderName = e.Node.Text.Substring(3).Trim();
            _currentRemotePath = string.IsNullOrEmpty(_currentRemotePath) ? folderName : $"{_currentRemotePath}/{folderName}";
            await RefreshRemoteTreeAsync();
        }

        private async void BtnBackRemote_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected()) return;
            if (string.IsNullOrEmpty(_currentRemotePath)) return; // Already at root

            int lastSlash = _currentRemotePath.LastIndexOf('/');
            if (lastSlash < 0) _currentRemotePath = "";
            else _currentRemotePath = _currentRemotePath.Substring(0, lastSlash);

            await RefreshRemoteTreeAsync();
        }

        // ═══════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════

        private void RefreshLocalTree()
        {
            tvLocal.Nodes.Clear();
            if (string.IsNullOrEmpty(_server.SharedFolderPath)) return;
            var root = BuildTreeNode(_server.SharedFolderPath);
            root.Expand();
            tvLocal.Nodes.Add(root);
        }

        private async System.Threading.Tasks.Task RefreshRemoteTreeAsync()
        {
            if (_client == null || !_client.IsConnected) return;
            string response = await _client.GetListAsync(_currentRemotePath);

            tvRemote.Invoke(new Action(() =>
            {
                tvRemote.Nodes.Clear();
                var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
                if (parts[0] != ProtocolMessages.RES_LIST) return;

                string ip = txtIP.Text.Trim();
                string displayPath = string.IsNullOrEmpty(_currentRemotePath) ? "/" : $"/{_currentRemotePath}";
                var root = new TreeNode($"📡 {ip} {displayPath}");

                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    foreach (string item in parts[1].Split('|'))
                    {
                        if (item.StartsWith("[DIR]"))
                            root.Nodes.Add("📁 " + item.Substring(5));
                        else if (item.StartsWith("[FILE]"))
                            root.Nodes.Add("📄 " + item.Substring(6).Split('*')[0]);
                    }
                }
                root.Expand();
                tvRemote.Nodes.Add(root);
            }));
        }

        private static TreeNode BuildTreeNode(string path)
        {
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name)) name = path;
            var node = new TreeNode("📁 " + name);

            try
            {
                foreach (string d in Directory.GetDirectories(path))
                    node.Nodes.Add(BuildTreeNode(d));
                foreach (string f in Directory.GetFiles(path))
                    node.Nodes.Add("📄 " + Path.GetFileName(f));
            }
            catch { }

            return node;
        }

        private bool EnsureClientConnected()
        {
            if (_client != null && _client.IsConnected) return true;
            MessageBox.Show("Bạn chưa kết nối đến máy nào.", "Chưa kết nối",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private static bool EnsureItemSelected(TreeView tv, out string itemName, out bool isFile)
        {
            itemName = string.Empty;
            isFile = false;
            if (tv.SelectedNode == null || (!tv.SelectedNode.Text.StartsWith("📄 ") && !tv.SelectedNode.Text.StartsWith("📁 ")))
            {
                MessageBox.Show("Vui lòng chọn một File (📄) hoặc Thư mục (📁) trong danh sách.", "Chưa chọn",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            isFile = tv.SelectedNode.Text.StartsWith("📄 ");
            itemName = tv.SelectedNode.Text.Substring(3).Trim();
            return true;
        }

        private static void SetBusy(Button btn, string text, bool busy)
        {
            btn.Text    = text;
            btn.Enabled = !busy;
        }

        private void UpdateServerStatus(bool running)
        {
            lblStatus.Text      = running ? "● Server: Đang chạy (Port 8888)" : "● Server: Đang tắt";
            lblStatus.ForeColor = running
                ? System.Drawing.Color.FromArgb(144, 238, 144)
                : System.Drawing.Color.FromArgb(255, 180, 180);
        }

        private void AppendLog(string msg)
        {
            if (lvLog.InvokeRequired)
            {
                lvLog.BeginInvoke(new Action(() => AppendLog(msg)));
                return;
            }
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(msg);
            lvLog.Items.Add(item);
            lvLog.EnsureVisible(lvLog.Items.Count - 1);
        }
    }
}
