using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LanAutoDiscovery;
using Microsoft.VisualBasic;
using P2PFileSharingApp.Core;
using P2PFileSharingApp.Network;

namespace P2PFileSharingApp.UI
{
    public partial class MainForm : Form
    {
        private const int ServerPort = 8888;

        private readonly P2PServer _server = new P2PServer();
        private readonly LanClientDiscoveryService _discoveryService = new LanClientDiscoveryService();
        private P2PClient? _client;
        private LanServerBroadcaster? _serverBroadcaster;
        private bool _serverRunning;
        private string _currentRemotePath = string.Empty;
        private string _connectedServerDisplay = string.Empty;
        private FileSystemWatcher? _watcher;

        public MainForm()
        {
            InitializeComponent();

            string rootPath = Path.Combine(Application.StartupPath, "P2P_Shared_Root");
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            _server.SharedFolderPath = rootPath;
            lblSharedPath.Text = "Thu muc chia se: " + rootPath;

            _watcher = new FileSystemWatcher(rootPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcher.Created += (_, __) => BeginInvoke(new Action(RefreshLocalTree));
            _watcher.Deleted += (_, __) => BeginInvoke(new Action(RefreshLocalTree));
            _watcher.Renamed += (_, __) => BeginInvoke(new Action(RefreshLocalTree));
            RefreshLocalTree();

            btnTabServer.Click += (_, __) => ShowTab(true);
            btnTabClient.Click += (_, __) => ShowTab(false);

            btnAddToRoot.Click += BtnAddToRoot_Click;
            btnToggleServer.Click += BtnToggleServer_Click;
            btnGrantRead.Click += (_, __) => SetSelectedClientPermission(PermissionLevel.ReadOnly);
            btnGrantWrite.Click += (_, __) => SetSelectedClientPermission(PermissionLevel.ReadWrite);
            btnDenyClient.Click += (_, __) => SetSelectedClientPermission(PermissionLevel.Denied);

            btnConnect.Click += BtnConnect_Click;
            btnDownload.Click += BtnDownload_Click;
            btnUpload.Click += BtnUpload_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRename.Click += BtnRename_Click;
            btnMkDir.Click += BtnMkDir_Click;
            btnBackRemote.Click += BtnBackRemote_Click;
            tvRemote.NodeMouseDoubleClick += TvRemote_NodeMouseDoubleClick;

            _server.OnLog += AppendLog;
            _server.OnClientConnected += OnClientConnected;
            _server.OnClientDisconnected += OnClientDisconnected;

            _discoveryService.ServersChanged += DiscoveryService_ServersChanged;
            cboServers.SelectedIndexChanged += CboServers_SelectedIndexChanged;
            _discoveryService.Start();

            FormClosing += MainForm_FormClosing;
            ShowTab(true);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _watcher?.Dispose();
            _client?.Disconnect();
            _server.Stop();
            _serverBroadcaster?.StopAsync().GetAwaiter().GetResult();
            _discoveryService.StopAsync().GetAwaiter().GetResult();
        }

        private void ShowTab(bool serverTab)
        {
            panelServer.Visible = serverTab;
            panelClient.Visible = !serverTab;
            btnTabServer.Enabled = !serverTab;
            btnTabClient.Enabled = serverTab;
        }

        private void BtnAddToRoot_Click(object? sender, EventArgs e)
        {
            var choice = MessageBox.Show(
                "Ban muon them File hay Thu muc vao Root?\n\nYes = Thu muc\nNo = File",
                "Them vao Root",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (choice == DialogResult.Cancel)
            {
                return;
            }

            try
            {
                if (choice == DialogResult.Yes)
                {
                    using var fbd = new FolderBrowserDialog { Description = "Chon thu muc de them vao Root" };
                    if (fbd.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    string destDir = Path.Combine(_server.SharedFolderPath, new DirectoryInfo(fbd.SelectedPath).Name);
                    CopyDirectory(fbd.SelectedPath, destDir);
                    MessageBox.Show("Da copy thu muc vao Root thanh cong.", "Thanh cong");
                }
                else
                {
                    using var ofd = new OpenFileDialog { Title = "Chon file de them vao Root", Multiselect = true };
                    if (ofd.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    foreach (var file in ofd.FileNames)
                    {
                        string destFile = Path.Combine(_server.SharedFolderPath, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }

                    MessageBox.Show("Da copy file vao Root thanh cong.", "Thanh cong");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Loi khi copy: " + ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            Directory.CreateDirectory(destinationDir);

            foreach (var file in dir.GetFiles())
            {
                file.CopyTo(Path.Combine(destinationDir, file.Name), true);
            }

            foreach (var subDir in dir.GetDirectories())
            {
                CopyDirectory(subDir.FullName, Path.Combine(destinationDir, subDir.Name));
            }
        }

        private void BtnToggleServer_Click(object? sender, EventArgs e)
        {
            if (!_serverRunning)
            {
                _server.Start(ServerPort);
                StartLanBroadcast();
                _serverRunning = true;
                btnToggleServer.Text = "Tat Server";
                UpdateServerStatus(true);
            }
            else
            {
                StopLanBroadcast();
                _server.Stop();
                _serverRunning = false;
                btnToggleServer.Text = "Bat Server";
                lvClients.Items.Clear();
                UpdateServerStatus(false);
            }
        }

        private void StartLanBroadcast()
        {
            StopLanBroadcast();
            string roomName = string.IsNullOrWhiteSpace(txtRoomName.Text) ? "Phong cua Dung" : txtRoomName.Text.Trim();
            _serverBroadcaster = new LanServerBroadcaster(roomName, ServerPort);
            _serverBroadcaster.Start();
            AppendLog("[DISCOVERY] Dang phat server LAN: " + roomName);
        }

        private void StopLanBroadcast()
        {
            if (_serverBroadcaster is null)
            {
                return;
            }

            _serverBroadcaster.StopAsync().GetAwaiter().GetResult();
            _serverBroadcaster = null;
            AppendLog("[DISCOVERY] Da dung phat server LAN.");
        }

        private void OnClientConnected(string ip)
        {
            BeginInvoke(new Action(() =>
            {
                foreach (ListViewItem existing in lvClients.Items)
                {
                    if (existing.Text == ip)
                    {
                        RefreshClientPermission(existing);
                        return;
                    }
                }

                var item = new ListViewItem(ip);
                item.SubItems.Add(GetPermLabel(PermissionManager.GetPermission(ip)));
                lvClients.Items.Add(item);
                AppendLog("Client ket noi: " + ip);
            }));
        }

        private void OnClientDisconnected(string ip)
        {
            BeginInvoke(new Action(() =>
            {
                foreach (ListViewItem item in lvClients.Items)
                {
                    if (item.Text == ip)
                    {
                        lvClients.Items.Remove(item);
                        break;
                    }
                }

                AppendLog("Client ngat ket noi: " + ip);
            }));
        }

        private void SetSelectedClientPermission(PermissionLevel level)
        {
            if (lvClients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui long chon mot may khach trong danh sach.", "Chua chon");
                return;
            }

            var item = lvClients.SelectedItems[0];
            string ip = item.Text;
            PermissionManager.SetPermission(ip, level);
            item.SubItems[1].Text = GetPermLabel(level);
            AppendLog($"Da dat quyen [{GetPermLabel(level)}] cho IP: {ip}");

            if (level == PermissionLevel.Denied)
            {
                _server.DisconnectClient(ip);
            }
        }

        private static void RefreshClientPermission(ListViewItem item)
        {
            item.SubItems[1].Text = GetPermLabel(PermissionManager.GetPermission(item.Text));
        }

        private static string GetPermLabel(PermissionLevel level)
        {
            return level switch
            {
                PermissionLevel.ReadOnly => "Quyen doc",
                PermissionLevel.ReadWrite => "Quyen sua",
                PermissionLevel.Denied => "Bi chan",
                _ => "Khong ro"
            };
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (cboServers.SelectedItem is not DiscoveredServer selectedServer)
            {
                MessageBox.Show("Chua tim thay server LAN nao de ket noi.", "Thong bao");
                return;
            }

            _client?.Disconnect();
            _client = new P2PClient();
            _client.OnLog += msg => AppendLog("[CLIENT] " + msg);
            _currentRemotePath = string.Empty;
            _connectedServerDisplay = selectedServer.DisplayName;

            int port = selectedServer.Port;
            _ = int.TryParse(txtPort.Text.Trim(), out port);
            if (port <= 0)
            {
                port = selectedServer.Port;
            }

            btnConnect.Enabled = false;
            btnConnect.Text = "Dang ket noi...";
            lblConnStatus.Text = "Dang ket noi...";

            var (ok, msg) = await _client.ConnectAsync(selectedServer.IpAddress, port);

            if (ok)
            {
                lblConnStatus.Text = $"Da ket noi: {selectedServer.DisplayName} ({selectedServer.Endpoint})";
                btnConnect.Text = "Ngat va ket noi lai";
                await RefreshRemoteTreeAsync();
            }
            else
            {
                lblConnStatus.Text = "Ket noi that bai";
                btnConnect.Text = "Ket noi";
                MessageBox.Show("Khong the ket noi:\n" + msg, "Loi ket noi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            btnConnect.Enabled = true;
        }

        private async void BtnDownload_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || !EnsureItemSelected(tvRemote, out string itemName, out bool isFile))
            {
                return;
            }

            if (!isFile)
            {
                MessageBox.Show("Chi ho tro tai file, khong ho tro tai ca thu muc.", "Thong bao");
                return;
            }

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? itemName : $"{_currentRemotePath}/{itemName}";
            using var fbd = new FolderBrowserDialog { Description = "Chon thu muc luu file" };
            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            SetBusy(btnDownload, "Dang tai...", true);
            var (ok, msg) = await _client!.DownloadAsync(targetPath, fbd.SelectedPath);
            SetBusy(btnDownload, "Download", false);

            MessageBox.Show(msg, ok ? "Thanh cong" : "That bai");
        }

        private async void BtnUpload_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected())
            {
                return;
            }

            var choice = MessageBox.Show(
                "Ban muon tai len File hay Thu muc?\n\nYes = Thu muc\nNo = File",
                "Tai len",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (choice == DialogResult.Cancel)
            {
                return;
            }

            bool isOk;
            string message;

            if (choice == DialogResult.Yes)
            {
                using var fbd = new FolderBrowserDialog { Description = "Chon thu muc de upload" };
                if (fbd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                SetBusy(btnUpload, "Dang upload...", true);
                var result = await _client!.UploadDirectoryAsync(fbd.SelectedPath, _currentRemotePath);
                isOk = result.ok;
                message = result.msg;
            }
            else
            {
                using var ofd = new OpenFileDialog { Title = "Chon file de upload", Multiselect = false };
                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                SetBusy(btnUpload, "Dang upload...", true);
                var result = await _client!.UploadAsync(ofd.FileName, _currentRemotePath);
                isOk = result.ok;
                message = result.msg;
            }

            SetBusy(btnUpload, "Upload", false);
            MessageBox.Show(message, isOk ? "Thanh cong" : "That bai");

            if (isOk)
            {
                await RefreshRemoteTreeAsync();
            }
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || !EnsureItemSelected(tvRemote, out string itemName, out _))
            {
                return;
            }

            var confirm = MessageBox.Show($"Ban co chac muon xoa:\n{itemName}?", "Xac nhan xoa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? itemName : $"{_currentRemotePath}/{itemName}";
            SetBusy(btnDelete, "Dang xoa...", true);
            var (ok, msg) = await _client!.DeleteAsync(targetPath);
            SetBusy(btnDelete, "Xoa", false);

            MessageBox.Show(msg, ok ? "Thanh cong" : "That bai");
            if (ok)
            {
                await RefreshRemoteTreeAsync();
            }
        }

        private async void BtnRename_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || !EnsureItemSelected(tvRemote, out string itemName, out _))
            {
                return;
            }

            string newName = Interaction.InputBox("Nhap ten moi:", "Doi ten", itemName);
            if (string.IsNullOrWhiteSpace(newName) || newName == itemName)
            {
                return;
            }

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? itemName : $"{_currentRemotePath}/{itemName}";
            SetBusy(btnRename, "Dang doi...", true);
            var (ok, msg) = await _client!.RenameAsync(targetPath, newName);
            SetBusy(btnRename, "Doi ten", false);

            MessageBox.Show(msg, ok ? "Thanh cong" : "That bai");
            if (ok)
            {
                await RefreshRemoteTreeAsync();
            }
        }

        private async void BtnMkDir_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected())
            {
                return;
            }

            string folderName = Interaction.InputBox("Nhap ten thu muc moi:", "Tao thu muc moi", "New Folder");
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return;
            }

            string targetPath = string.IsNullOrEmpty(_currentRemotePath) ? folderName : $"{_currentRemotePath}/{folderName}";
            SetBusy(btnMkDir, "Dang tao...", true);
            var (ok, msg) = await _client!.MkDirAsync(targetPath);
            SetBusy(btnMkDir, "Thu muc", false);

            MessageBox.Show(msg, ok ? "Thanh cong" : "That bai");
            if (ok)
            {
                await RefreshRemoteTreeAsync();
            }
        }

        private async void TvRemote_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (!EnsureClientConnected() || e.Node == null || !e.Node.Text.StartsWith("[DIR] "))
            {
                return;
            }

            string folderName = e.Node.Text.Substring(6).Trim();
            _currentRemotePath = string.IsNullOrEmpty(_currentRemotePath) ? folderName : $"{_currentRemotePath}/{folderName}";
            await RefreshRemoteTreeAsync();
        }

        private async void BtnBackRemote_Click(object? sender, EventArgs e)
        {
            if (!EnsureClientConnected() || string.IsNullOrEmpty(_currentRemotePath))
            {
                return;
            }

            int lastSlash = _currentRemotePath.LastIndexOf('/');
            _currentRemotePath = lastSlash < 0 ? string.Empty : _currentRemotePath.Substring(0, lastSlash);
            await RefreshRemoteTreeAsync();
        }

        private void RefreshLocalTree()
        {
            tvLocal.Nodes.Clear();
            if (string.IsNullOrEmpty(_server.SharedFolderPath))
            {
                return;
            }

            var root = BuildTreeNode(_server.SharedFolderPath);
            root.Expand();
            tvLocal.Nodes.Add(root);
        }

        private async Task RefreshRemoteTreeAsync()
        {
            if (_client == null || !_client.IsConnected)
            {
                return;
            }

            string response = await _client.GetListAsync(_currentRemotePath);
            tvRemote.Invoke(new Action(() =>
            {
                tvRemote.Nodes.Clear();
                var parts = response.Split(new[] { ProtocolMessages.SEPARATOR }, 2);
                if (parts[0] != ProtocolMessages.RES_LIST)
                {
                    return;
                }

                string displayPath = string.IsNullOrEmpty(_currentRemotePath) ? "/" : $"/{_currentRemotePath}";
                var root = new TreeNode($"{_connectedServerDisplay} {displayPath}");

                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    foreach (string item in parts[1].Split('|'))
                    {
                        if (item.StartsWith("[DIR]"))
                        {
                            root.Nodes.Add("[DIR] " + item.Substring(5));
                        }
                        else if (item.StartsWith("[FILE]"))
                        {
                            root.Nodes.Add("[FILE] " + item.Substring(6).Split('*')[0]);
                        }
                    }
                }

                root.Expand();
                tvRemote.Nodes.Add(root);
            }));
        }

        private static TreeNode BuildTreeNode(string path)
        {
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
            {
                name = path;
            }

            var node = new TreeNode("[DIR] " + name);
            try
            {
                foreach (string directory in Directory.GetDirectories(path))
                {
                    node.Nodes.Add(BuildTreeNode(directory));
                }

                foreach (string file in Directory.GetFiles(path))
                {
                    node.Nodes.Add("[FILE] " + Path.GetFileName(file));
                }
            }
            catch
            {
            }

            return node;
        }

        private bool EnsureClientConnected()
        {
            if (_client != null && _client.IsConnected)
            {
                return true;
            }

            MessageBox.Show("Ban chua ket noi den may nao.", "Chua ket noi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private static bool EnsureItemSelected(TreeView treeView, out string itemName, out bool isFile)
        {
            itemName = string.Empty;
            isFile = false;

            if (treeView.SelectedNode == null)
            {
                MessageBox.Show("Vui long chon mot muc trong danh sach.", "Chua chon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (treeView.SelectedNode.Text.StartsWith("[FILE] "))
            {
                itemName = treeView.SelectedNode.Text.Substring(7).Trim();
                isFile = true;
                return true;
            }

            if (treeView.SelectedNode.Text.StartsWith("[DIR] "))
            {
                itemName = treeView.SelectedNode.Text.Substring(6).Trim();
                return true;
            }

            MessageBox.Show("Vui long chon file hoac thu muc hop le.", "Chua chon", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private static void SetBusy(Button button, string text, bool busy)
        {
            button.Text = text;
            button.Enabled = !busy;
        }

        private void UpdateServerStatus(bool running)
        {
            lblStatus.Text = running ? "Server dang chay (Port 8888)" : "Server dang tat";
            lblConnStatus.Text = running ? lblConnStatus.Text : "Chua ket noi";
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

        private void DiscoveryService_ServersChanged(object? sender, IReadOnlyList<DiscoveredServer> servers)
        {
            if (cboServers.InvokeRequired)
            {
                cboServers.BeginInvoke(new Action(() => BindDiscoveredServers(servers)));
                return;
            }

            BindDiscoveredServers(servers);
        }

        private void BindDiscoveredServers(IReadOnlyList<DiscoveredServer> servers)
        {
            string? selectedServerId = (cboServers.SelectedItem as DiscoveredServer)?.ServerId;

            cboServers.BeginUpdate();
            cboServers.DataSource = servers.Count > 0 ? new List<DiscoveredServer>(servers) : null;
            cboServers.DisplayMember = nameof(DiscoveredServer.DisplayName);
            cboServers.EndUpdate();

            if (servers.Count == 0)
            {
                lblConnStatus.Text = "Chua tim thay server LAN";
                return;
            }

            if (selectedServerId is not null)
            {
                for (var i = 0; i < servers.Count; i++)
                {
                    if (servers[i].ServerId == selectedServerId)
                    {
                        cboServers.SelectedIndex = i;
                        return;
                    }
                }
            }

            cboServers.SelectedIndex = 0;
        }

        private void CboServers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboServers.SelectedItem is not DiscoveredServer selectedServer)
            {
                return;
            }

            txtPort.Text = selectedServer.Port.ToString();
            lblConnStatus.Text = "San sang ket noi: " + selectedServer.DisplayName;
        }
    }
}
