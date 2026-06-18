using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using P2PFileSharingApp.Models;

using P2PFileSharingApp.Network;
using LanAutoDiscovery;

namespace P2PFileSharingApp.UI;

public partial class MainForm : Form
{
    private static readonly Color AppBack = Color.FromArgb(241, 244, 247);
    private static readonly Color PanelBack = Color.FromArgb(252, 253, 254);
    private static readonly Color Ink = Color.FromArgb(31, 41, 55);
    private static readonly Color Muted = Color.FromArgb(100, 116, 139);
    private static readonly Color Accent = Color.FromArgb(31, 111, 110);
    private static readonly Color AccentSoft = Color.FromArgb(221, 242, 239);
    private static readonly Color NeutralBadge = Color.FromArgb(232, 236, 240);
    private static readonly Color WarningSoft = Color.FromArgb(254, 243, 199);
    private static readonly Color DangerSoft = Color.FromArgb(254, 226, 226);
    private static readonly Color SuccessSoft = Color.FromArgb(209, 250, 229);
    private static readonly Color Danger = Color.FromArgb(220, 38, 38);
    private static readonly Color Success = Color.FromArgb(5, 150, 105);

    private readonly BindingList<PeerInfo> peers = [];
    
    private readonly P2PServer _server = new();
    private readonly LanClientDiscoveryService _discoveryService = new();
    private P2PClient? _client;
    private LanServerBroadcaster? _serverBroadcaster;
    private string P2PSharedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "P2PSharedRoot");
    private readonly ToolTip toolTips = new();
    private readonly ImageList fileIcons = new();

    private CancellationTokenSource? connectCts;
    private CancellationTokenSource? transferCts;
    private string localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "P2PDownloads");
    
    // Lưu mức quyền hiện tại do Server trả về khi kết nối
    private string remotePermission = "ReadOnly";

    private bool isConnected;
    private bool serverRunning;

    private Panel pageHost = null!;
    private Button btnServerTab = null!;
    private Button btnClientTab = null!;
    private Control serverView = null!;
    private Control clientView = null!;
    private ServerStatusIndicator lblServerState = null!;
    private Button btnToggleServer = null!;
    private ListView lvServerFiles = null!;
    private ListView lvPermissions = null!;
    private ListView lvServerLogs = null!;
    private ComboBox cboPermissionIp = null!;
    private TextBox txtPermissionName = null!;
    private ComboBox cboPermissionMode = null!;
    private Button btnAddPermission = null!;

    private ComboBox cboPeers = null!;
    private TextBox txtDisplayName = null!;
    private ComboBox cboServers = null!;
    private bool isBindingDiscoveredServers;
    private NumericUpDown numPort = null!;
    
    private Button btnSavePeer = null!;
    private Button btnConnect = null!;
    private ProgressBar progressConnect = null!;
    private Label lblConnection = null!;

    private ListView lvLocalFiles = null!;
    private ListView lvRemoteFiles = null!;
    private Button btnChooseFolder = null!;
    private Button btnUpload = null!;
    private Button btnDownload = null!;
    private Button btnDelete = null!;
    private Button btnCancelTransfer = null!;

    private RoundedProgressBar progressTransfer = null!;
    private Label lblTransfer = null!;
    private ListView lvClientLogs = null!;

    public MainForm()
    {
        InitializeComponent();
        
        if (!Directory.Exists(P2PSharedRoot))
            Directory.CreateDirectory(P2PSharedRoot);
        if (!Directory.Exists(localRoot))
            Directory.CreateDirectory(localRoot);

        _server.SharedFolderPath = P2PSharedRoot;

        Font = new Font("Segoe UI", 9.25F);
        toolTips.ShowAlways = true;
        InitializeFileIcons();
        BuildUi();
        LoadPeers();
        LoadLocalFiles();
        LoadServerFiles();
        
        LoadPermissionsDemo();
        UpdateActionState();
        AddServerLog("Server đang dừng. Bấm Bật server để bắt đầu.", LogType.Info);
        AddClientLog("Bật Chế độ demo để thử kết nối khi chưa có server thật.", LogType.Info);

        _discoveryService.ServersChanged += DiscoveryService_ServersChanged;
        _discoveryService.Start();
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
        string typedText = cboServers.Text;
        bool keepTypedText = cboServers.Focused && cboServers.SelectedIndex < 0;

        isBindingDiscoveredServers = true;
        cboServers.BeginUpdate();
        try
        {
            cboServers.Items.Clear();
            foreach (DiscoveredServer server in servers)
                cboServers.Items.Add(server);
        }
        finally
        {
            cboServers.EndUpdate();
            isBindingDiscoveredServers = false;
        }

        if (keepTypedText)
        {
            cboServers.SelectedIndex = -1;
            cboServers.Text = typedText;
            cboServers.SelectionStart = cboServers.Text.Length;
            return;
        }

        if (servers.Count == 0)
        {
            cboServers.SelectedIndex = -1;
            cboServers.Text = typedText;
            return;
        }

        if (selectedServerId != null)
        {
            for (int i = 0; i < cboServers.Items.Count; i++)
            {
                if (cboServers.Items[i] is DiscoveredServer server && server.ServerId == selectedServerId)
                {
                    cboServers.SelectedIndex = i;
                    return;
                }
            }
        }

        if (!cboServers.Focused && string.IsNullOrWhiteSpace(typedText))
            cboServers.SelectedIndex = 0;
        else
            cboServers.Text = typedText;
    }

    private static List<DiscoveredServer> MergeDiscoveredServers(
        IEnumerable<DiscoveredServer> udpServers,
        IEnumerable<DiscoveredServer> tcpServers)
    {
        Dictionary<string, DiscoveredServer> serversByEndpoint = new(StringComparer.OrdinalIgnoreCase);

        foreach (DiscoveredServer server in tcpServers)
            serversByEndpoint[server.Endpoint] = server;

        foreach (DiscoveredServer server in udpServers)
            serversByEndpoint[server.Endpoint] = server;

        return serversByEndpoint.Values
            .OrderBy(server => server.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(server => server.IpAddress, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<List<DiscoveredServer>> ScanLanByTcpAsync(int port, CancellationToken cancellationToken)
    {
        HashSet<string> targets = BuildLocalSubnetTargets();
        List<Task<DiscoveredServer?>> scanTasks = [];
        using SemaphoreSlim throttle = new(64);

        foreach (string ip in targets)
        {
            scanTasks.Add(Task.Run(async () =>
            {
                await throttle.WaitAsync(cancellationToken);
                try
                {
                    return await TryDiscoverServerByTcpAsync(ip, port, cancellationToken);
                }
                finally
                {
                    throttle.Release();
                }
            }, cancellationToken));
        }

        DiscoveredServer?[] results = await Task.WhenAll(scanTasks);
        return results.Where(server => server is not null).Cast<DiscoveredServer>().ToList();
    }

    private static async Task<DiscoveredServer?> TryDiscoverServerByTcpAsync(
        string ip,
        int port,
        CancellationToken cancellationToken)
    {
        try
        {
            using TcpClient client = new();
            Task connectTask = client.ConnectAsync(ip, port);
            Task timeoutTask = Task.Delay(300, cancellationToken);

            if (await Task.WhenAny(connectTask, timeoutTask) != connectTask)
                return null;

            await connectTask;

            if (!client.Connected)
                return null;

            return new DiscoveredServer
            {
                ServerId = $"tcp-{ip}-{port}",
                DisplayName = $"P2P {ip}",
                IpAddress = ip,
                Port = port,
                LastSeenUtc = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<string> BuildLocalSubnetTargets()
    {
        HashSet<string> targets = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> localIps = GetLocalIPv4Addresses();

        foreach (string localIp in localIps)
        {
            string[] parts = localIp.Split('.');
            if (parts.Length != 4)
                continue;

            string prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";
            for (int host = 1; host <= 254; host++)
            {
                string candidate = prefix + host;
                if (!localIps.Contains(candidate))
                    targets.Add(candidate);
            }
        }

        return targets;
    }

    private static HashSet<string> GetLocalIPv4Addresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
            .Where(address =>
                address.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(address.Address))
            .Select(address => address.Address.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        
        connectCts?.Cancel();
        transferCts?.Cancel();
        
        _discoveryService.StopAsync().GetAwaiter().GetResult();
        _serverBroadcaster?.StopAsync().GetAwaiter().GetResult();
        _server.Stop();
        _client?.Disconnect();
        
        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        BackColor = AppBack;

        TableLayoutPanel shell = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(18),
            BackColor = AppBack
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildHeader(), 0, 0);
        shell.Controls.Add(BuildTabNavigation(), 0, 1);

        pageHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBack,
            Padding = new Padding(0)
        };
        shell.Controls.Add(pageHost, 0, 2);

        serverView = BuildServerPage();
        clientView = BuildClientPage();
        serverView.Dock = DockStyle.Fill;
        clientView.Dock = DockStyle.Fill;
        pageHost.Controls.Add(serverView);
        pageHost.Controls.Add(clientView);
        ShowPage(serverView);
    }

    private Control BuildTabNavigation()
    {
        FlowLayoutPanel nav = new()
        {
            Dock = DockStyle.Fill,
            BackColor = AppBack,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 6, 0, 4),
            WrapContents = false
        };

        btnServerTab = NavButton("Server của mình");
        btnClientTab = NavButton("Kết nối server khác");
        btnServerTab.Click += (_, _) => ShowPage(serverView);
        btnClientTab.Click += (_, _) => ShowPage(clientView);
        nav.Controls.Add(btnServerTab);
        nav.Controls.Add(btnClientTab);
        return nav;
    }

    private static Button NavButton(string text)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = false,
            Width = 172,
            Height = 30,
            Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.5F),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(205, 214, 224);
        button.FlatAppearance.MouseOverBackColor = AccentSoft;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(190, 226, 220);
        return button;
    }

    private void ShowPage(Control page)
    {
        if (serverView != null)
            serverView.Visible = ReferenceEquals(page, serverView);
        if (clientView != null)
            clientView.Visible = ReferenceEquals(page, clientView);

        StyleNavButton(btnServerTab, serverView != null && ReferenceEquals(page, serverView));
        StyleNavButton(btnClientTab, clientView != null && ReferenceEquals(page, clientView));
    }

    private static void StyleNavButton(Button? button, bool active)
    {
        if (button == null)
            return;

        button.BackColor = active ? Accent : Color.White;
        button.ForeColor = active ? Color.White : Ink;
        button.FlatAppearance.BorderColor = active ? Accent : Color.FromArgb(205, 214, 224);
    }

    private Control BuildHeader()
    {
        TableLayoutPanel header = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = AppBack
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        Label logo = new()
        {
            Dock = DockStyle.Fill,
            Text = "■",
            Font = new Font("Segoe UI Semibold", 14F),
            ForeColor = Accent,
            TextAlign = ContentAlignment.MiddleCenter
        };
        header.Controls.Add(logo, 0, 0);

        Label title = new()
        {
            Dock = DockStyle.Fill,
            Text = "Ứng dụng truy xuất tài nguyên",
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(title, 1, 0);

        return header;
    }

    private Control BuildServerPage()
    {
        TableLayoutPanel page = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            BackColor = AppBack
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 198));

        Control statusPanel = BuildServerStatusPanel();
        page.Controls.Add(statusPanel, 0, 0);
        page.SetColumnSpan(statusPanel, 3);

        Control filesPanel = BuildServerFilesPanel();
        Control permissionsPanel = BuildPermissionsPanel();
        filesPanel.Margin = new Padding(0, 0, 10, 12);
        permissionsPanel.Margin = new Padding(6, 0, 0, 12);
        page.Controls.Add(filesPanel, 0, 1);
        page.SetColumnSpan(filesPanel, 2);
        page.Controls.Add(permissionsPanel, 2, 1);

        Control logPanel = BuildServerLogPanel();
        page.Controls.Add(logPanel, 0, 2);
        page.SetColumnSpan(logPanel, 3);

        return page;
    }

    private Control BuildServerStatusPanel()
    {
        TableLayoutPanel panel = CreatePanel(5, 2);
        panel.Padding = new Padding(14, 12, 14, 12);
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        Label title = SectionTitle("Server của máy mình");
        panel.Controls.Add(title, 0, 0);
        panel.SetColumnSpan(title, 4);

        lblServerState = new ServerStatusIndicator
        {
            Dock = DockStyle.Fill,
            Text = "Đang dừng",
            DotColor = Danger
        };
        panel.Controls.Add(lblServerState, 4, 0);

        Label summary = new()
        {
            Dock = DockStyle.Fill,
            Text = $"Tất cả file trong thư mục Root sẽ được chia sẻ.\n{P2PSharedRoot}",
            AutoEllipsis = true,
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(summary, 0, 1);

        Button btnOpenRoot = SecondaryButton("Mở thư mục");
        btnOpenRoot.Click += (_, _) => OpenRootFolder();
        panel.Controls.Add(btnOpenRoot, 1, 1);

        Button btnAddFile = PrimaryButton("Thêm File");
        btnAddFile.Click += (_, _) => AddFileToRoot();
        panel.Controls.Add(btnAddFile, 2, 1);

        Button btnAddFolder = PrimaryButton("Thêm Thư mục");
        btnAddFolder.Click += (_, _) => AddFolderToRoot();
        panel.Controls.Add(btnAddFolder, 3, 1);

        btnToggleServer = PrimaryButton("Bật server");
        btnToggleServer.Click += (_, _) => ToggleServer();
        panel.Controls.Add(btnToggleServer, 4, 1);

        return panel;
    }

    private Control BuildServerFilesPanel()
    {
        TableLayoutPanel panel = CreatePanel(1, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(SectionTitle("Tài nguyên đang chia sẻ"), 0, 0);
        lvServerFiles = CreateFileListView();
        panel.Controls.Add(lvServerFiles, 0, 1);

        return panel;
    }

    private Control BuildPermissionsPanel()
    {
        TableLayoutPanel panel = CreatePanel(1, 3);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(SectionTitle("Phân quyền IP"), 0, 0);
        panel.Controls.Add(BuildPermissionEditor(), 0, 1);

        lvPermissions = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            BackColor = PanelBack,
            ForeColor = Ink,
            ShowItemToolTips = true,
            Scrollable = true
        };
        ApplyListViewChrome(lvPermissions);
        lvPermissions.Columns.Add("IP", 130);
        lvPermissions.Columns.Add("Quyền", 120);
        lvPermissions.Columns.Add("Tên máy", 160);
        lvPermissions.Resize += (_, _) => FitPermissionColumns();
        lvPermissions.HandleCreated += (_, _) => FitPermissionColumns();
        panel.Controls.Add(lvPermissions, 0, 2);

        return panel;
    }

    private Control BuildPermissionEditor()
    {
        TableLayoutPanel editor = CreatePanel(2, 4);
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        cboPermissionIp = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
        cboPermissionIp.DropDown += (_, _) => {
            cboPermissionIp.Items.Clear();
            foreach(var peer in peers) cboPermissionIp.Items.Add(peer.Key);
        };
        editor.Controls.Add(cboPermissionIp, 0, 0);
        txtPermissionName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Tên hiển thị" };
        editor.Controls.Add(txtPermissionName, 1, 0);

        cboPermissionMode = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cboPermissionMode.Items.AddRange(["Chỉ tải về", "Chỉ tải lên", "Tải lên và tải về", "Toàn quyền"]);
        cboPermissionMode.SelectedIndex = 2;
        editor.Controls.Add(cboPermissionMode, 0, 1);
        editor.SetColumnSpan(cboPermissionMode, 2);

        btnAddPermission = PrimaryButton("Thêm / cập nhật quyền");
        btnAddPermission.Click += (_, _) => AddOrUpdatePermission();
        editor.Controls.Add(btnAddPermission, 0, 3);
        editor.SetColumnSpan(btnAddPermission, 2);

        return editor;
    }

    private Control BuildServerLogPanel()
    {
        TableLayoutPanel panel = CreatePanel(1, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(SectionTitle("Nhật ký server"), 0, 0);
        lvServerLogs = CreateLogListView();
        panel.Controls.Add(lvServerLogs, 0, 1);
        return panel;
    }

    private Control BuildClientPage()
    {
        TableLayoutPanel page = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = AppBack
        };
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 226));

        page.Controls.Add(BuildConnectionPanel(), 0, 0);
        page.Controls.Add(BuildClientFilePanel(), 0, 1);
        page.Controls.Add(BuildTransferPanel(), 0, 2);
        page.Controls.Add(BuildClientLogPanel(), 0, 3);

        return page;
    }

    private Control BuildConnectionPanel()
    {
        TableLayoutPanel panel = CreatePanel(8, 4);
        panel.Padding = new Padding(12, 8, 12, 8);
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 106));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label title = SectionTitle("Kết nối server khác");
        panel.Controls.Add(title, 0, 0);
        panel.SetColumnSpan(title, 8);

        panel.Controls.Add(FieldLabel("Máy đã lưu"), 0, 1);
        panel.Controls.Add(FieldLabel("Tên hiển thị"), 1, 1);
        panel.Controls.Add(FieldLabel("IP"), 2, 1);
        panel.Controls.Add(FieldLabel("Port"), 3, 1);
        panel.Controls.Add(FieldLabel("Thao tác"), 4, 1);
        panel.Controls.Add(FieldLabel("Tìm kiếm"), 5, 1);
        panel.Controls.Add(FieldLabel("Kết nối"), 6, 1);
        panel.Controls.Add(FieldLabel("Trạng thái"), 7, 1);

        cboPeers = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cboPeers.SelectedIndexChanged += (_, _) => FillSelectedPeer();
        panel.Controls.Add(cboPeers, 0, 2);

        txtDisplayName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Phòng của Hải" };
        panel.Controls.Add(txtDisplayName, 1, 2);

        cboServers = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown };
        cboServers.SelectedIndexChanged += cboServers_SelectedIndexChanged;
        panel.Controls.Add(cboServers, 2, 2);

        numPort = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 65535,
            Value = 8888
        };
        panel.Controls.Add(numPort, 3, 2);

        btnSavePeer = SecondaryButton("Lưu IP");
        btnSavePeer.Click += (_, _) => SavePeerFromInputs();
        panel.Controls.Add(btnSavePeer, 4, 2);

        Button btnScanLAN = SecondaryButton("Quét LAN");
        btnScanLAN.Click += async (_, _) => 
        {
            btnScanLAN.Enabled = false;
            btnScanLAN.Text = "Đang quét...";
            try
            {
                await _discoveryService.StopAsync();
                _discoveryService.Start();
                await Task.Delay(1500);

                int port = (int)numPort.Value;
                using CancellationTokenSource scanCts = new(TimeSpan.FromSeconds(4));
                List<DiscoveredServer> tcpServers = await ScanLanByTcpAsync(port, scanCts.Token);
                List<DiscoveredServer> udpServers = _discoveryService.Servers.ToList();
                List<DiscoveredServer> mergedServers = MergeDiscoveredServers(udpServers, tcpServers);

                BindDiscoveredServers(mergedServers);
                AddClientLog($"Quét LAN xong: UDP {udpServers.Count}, TCP {tcpServers.Count}, tổng {mergedServers.Count} server.", LogType.Info);
            }
            catch (Exception ex)
            {
                AddClientLog("Quét LAN thất bại: " + ex.Message, LogType.Error);
            }
            finally
            {
                btnScanLAN.Text = "Quét LAN";
                btnScanLAN.Enabled = true;
            }
        };
        panel.Controls.Add(btnScanLAN, 5, 2);

        btnConnect = PrimaryButton("Kết nối");
        btnConnect.Click += btnConnect_Click;
        panel.Controls.Add(btnConnect, 6, 2);

        progressConnect = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Style = ProgressBarStyle.Blocks,
            Visible = false
        };
        panel.Controls.Add(progressConnect, 4, 0);
        panel.SetColumnSpan(progressConnect, 3);

        lblConnection = StatusLabel("Chưa kết nối", Muted, NeutralBadge);
        panel.Controls.Add(lblConnection, 7, 2);

        return panel;
    }

    private Control BuildClientFilePanel()
    {
        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppBack
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        Control localPanel = BuildLocalPanel();
        Control remotePanel = BuildRemotePanel();
        localPanel.Margin = new Padding(0, 0, 8, 0);
        remotePanel.Margin = new Padding(8, 0, 0, 0);

        grid.Controls.Add(localPanel, 0, 0);
        grid.Controls.Add(remotePanel, 1, 0);

        return grid;
    }

    private Control BuildLocalPanel()
    {
        TableLayoutPanel panel = CreatePanel(3, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        panel.Controls.Add(SectionTitle("Thư mục để tải về"), 0, 0);
        
        Button btnOpenLocal = SecondaryButton("Mở thư mục");
        btnOpenLocal.Click += (_, _) => {
            try { System.Diagnostics.Process.Start("explorer.exe", localRoot); } catch {}
        };
        panel.Controls.Add(btnOpenLocal, 1, 0);

        btnChooseFolder = SecondaryButton("Chọn thư mục");
        btnChooseFolder.Click += (_, _) => ChooseLocalFolder();
        panel.Controls.Add(btnChooseFolder, 2, 0);

        lvLocalFiles = CreateFileListView();
        panel.SetColumnSpan(lvLocalFiles, 3);
        panel.Controls.Add(lvLocalFiles, 0, 1);
        return panel;
    }

    private Control BuildRemotePanel()
    {
        TableLayoutPanel panel = CreatePanel(5, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 47));

        panel.Controls.Add(SectionTitle("Dữ liệu trên server đang kết nối"), 0, 0);

        btnUpload = IconButton("↑", "Tải file từ máy mình lên server khác");
        btnUpload.Click += async (_, _) => await UploadSelectedLocalFilesAsync();
        panel.Controls.Add(btnUpload, 1, 0);

        btnDownload = IconButton("↓", "Tải file từ server khác về máy mình");
        btnDownload.Click += async (_, _) => await DownloadSelectedRemoteFilesAsync();
        panel.Controls.Add(btnDownload, 2, 0);

        btnDelete = IconButton("✕", "Xóa file đang chọn trên server khác");
        btnDelete.Click += (_, _) => DeleteSelectedRemoteFiles();
        panel.Controls.Add(btnDelete, 3, 0);

        btnCancelTransfer = IconButton("⏹", "Dừng tiến trình tải lên hoặc tải về");
        btnCancelTransfer.Click += (_, _) =>
        {
            if (transferCts == null)
            {
                AddClientLog("Không có tiến trình truyền file đang chạy.", LogType.Info);
                return;
            }

            transferCts.Cancel();
        };
        panel.Controls.Add(btnCancelTransfer, 4, 0);

        lvRemoteFiles = CreateFileListView();
        lvRemoteFiles.AllowDrop = true;
        lvRemoteFiles.DragEnter += lvRemoteFiles_DragEnter;
        lvRemoteFiles.DragDrop += lvRemoteFiles_DragDrop;
        toolTips.SetToolTip(lvRemoteFiles, "Kéo thả file từ Desktop hoặc File Explorer vào đây để tải lên server khác.");
        panel.SetColumnSpan(lvRemoteFiles, 5);
        panel.Controls.Add(lvRemoteFiles, 0, 1);
        return panel;
    }

    private Control BuildTransferPanel()
    {
        TableLayoutPanel panel = CreatePanel(1, 2);
        panel.Padding = new Padding(14, 6, 14, 10);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        lblTransfer = new Label
        {
            Dock = DockStyle.Fill,
            Text = "✓ Sẵn sàng truyền file - 0%",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Success,
            Font = new Font("Segoe UI Semibold", 9.5F)
        };
        panel.Controls.Add(lblTransfer, 0, 0);

        progressTransfer = new RoundedProgressBar { Dock = DockStyle.Fill, Maximum = 100 };
        panel.Controls.Add(progressTransfer, 0, 1);
        return panel;
    }

    private Control BuildClientLogPanel()
    {
        TableLayoutPanel panel = CreatePanel(1, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(SectionTitle("Nhật ký kết nối"), 0, 0);
        lvClientLogs = CreateLogListView();
        panel.Controls.Add(lvClientLogs, 0, 1);
        return panel;
    }

    private static TableLayoutPanel CreatePanel(int columns, int rows)
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            BackColor = PanelBack,
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 12)
        };
        return panel;
    }

    private static Label SectionTitle(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Segoe UI Semibold", 12.75F),
            ForeColor = Ink,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Label FieldLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 8.75F),
            TextAlign = ContentAlignment.BottomLeft
        };
    }

    private static Label StatusLabel(string text, Color color, Color backColor)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            ForeColor = color,
            BackColor = backColor,
            Font = new Font("Segoe UI Semibold", 9.5F),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static Button PrimaryButton(string text)
    {
        Button button = BaseButton(text);
        button.BackColor = Accent;
        button.ForeColor = Color.White;
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        Button button = BaseButton(text);
        button.BackColor = Color.FromArgb(233, 239, 244);
        button.ForeColor = Ink;
        return button;
    }

    private Button IconButton(string icon, string tooltip)
    {
        Button button = new CenteredIconButton
        {
            Dock = DockStyle.Fill,
            Text = icon,
            BackColor = Color.FromArgb(233, 239, 244),
            ForeColor = Ink,
            Font = new Font("Segoe UI Symbol", 12F),
            Margin = new Padding(4, 3, 4, 3),
            Cursor = Cursors.Hand,
            AccessibleName = tooltip
        };
        toolTips.SetToolTip(button, tooltip);
        return button;
    }

    private sealed class CenteredIconButton : Button
    {
        private bool hovered;
        private bool pressed;

        public CenteredIconButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color fill = pressed
                ? Color.FromArgb(197, 215, 224)
                : hovered ? Color.FromArgb(217, 230, 237) : BackColor;

            e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
            using SolidBrush brush = new(fill);
            using Pen border = new(Color.FromArgb(190, 201, 212));
            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            e.Graphics.FillRectangle(brush, rect);
            e.Graphics.DrawRectangle(border, rect);

            using SolidBrush textBrush = new(ForeColor);
            using StringFormat format = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoClip
            };
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(Text, Font, textBrush, ClientRectangle, format);

            if (Focused)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -4, -4));
        }
    }

    private sealed class ServerStatusIndicator : Control
    {
        public Color DotColor { get; set; } = Danger;

        public ServerStatusIndicator()
        {
            DoubleBuffered = true;
            Font = new Font("Segoe UI Semibold", 11F);
            ForeColor = Color.FromArgb(82, 82, 91);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? PanelBack);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle textRect = new(0, 0, Width - 30, Height);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textRect,
                ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            int size = 16;
            int x = Width - size - 8;
            int y = (Height - size) / 2;
            using SolidBrush dot = new(DotColor);
            e.Graphics.FillEllipse(dot, x, y, size, size);
            using Pen ring = new(Color.FromArgb(60, 0, 0, 0));
            e.Graphics.DrawEllipse(ring, x, y, size, size);
        }
    }

    private sealed class RoundedProgressBar : Control
    {
        private int value;

        public int Maximum { get; set; } = 100;

        public int Value
        {
            get => value;
            set
            {
                int capped = Math.Max(0, Math.Min(Maximum, value));
                if (this.value == capped)
                    return;

                this.value = capped;
                Invalidate();
            }
        }

        public RoundedProgressBar()
        {
            DoubleBuffered = true;
            MinimumSize = new Size(0, 11);
            BackColor = PanelBack;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? PanelBack);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int height = Math.Min(9, Math.Max(6, Height - 4));
            Rectangle track = new(0, (Height - height) / 2, Width, height);

            using SolidBrush trackBrush = new(Color.FromArgb(226, 232, 240));
            e.Graphics.FillRectangle(trackBrush, track);

            if (Maximum <= 0 || value <= 0)
                return;

            int fillWidth = Math.Max(1, (int)Math.Round(track.Width * (value / (double)Maximum)));
            Rectangle fill = new(track.X, track.Y, Math.Min(fillWidth, track.Width), track.Height);
            using SolidBrush fillBrush = new(Accent);
            e.Graphics.FillRectangle(fillBrush, fill);
        }
    }

    private static void ApplyListViewChrome(ListView listView)
    {
        listView.OwnerDraw = true;
        listView.Font = new Font("Segoe UI", 9.25F);
        listView.DrawColumnHeader += (_, e) =>
        {
            using SolidBrush fill = new(Color.FromArgb(247, 249, 251));
            using Pen line = new(Color.FromArgb(226, 232, 240));
            e.Graphics.FillRectangle(fill, e.Bounds);
            e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            e.Graphics.DrawLine(line, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 4);

            Rectangle textRect = Rectangle.Inflate(e.Bounds, -6, 0);
            using Font headerFont = new("Segoe UI Semibold", 9F);
            TextRenderer.DrawText(
                e.Graphics,
                e.Header?.Text ?? string.Empty,
                headerFont,
                textRect,
                Ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
        listView.DrawItem += (_, e) =>
        {
            if (e.Item == null)
                return;

            Color fillColor = PanelBack;
            if (e.Item.Selected)
                fillColor = Color.FromArgb(224, 238, 241);
            else if (e.Item.BackColor != Color.Empty && e.Item.BackColor != PanelBack)
                fillColor = e.Item.BackColor;

            using SolidBrush rowBrush = new(fillColor);
            e.Graphics.FillRectangle(rowBrush, e.Bounds);
        };
        listView.DrawSubItem += (_, e) =>
        {
            if (e.Item == null || e.SubItem == null)
                return;

            Color textColor = e.Item.Selected ? Ink : e.SubItem.ForeColor;
            if (textColor == Color.Empty)
                textColor = listView.ForeColor;

            Rectangle textRect = Rectangle.Inflate(e.Bounds, -6, 0);
            if (e.ColumnIndex == 0 && listView.SmallImageList != null && e.Item.ImageIndex >= 0)
            {
                Image image = listView.SmallImageList.Images[e.Item.ImageIndex];
                int imageY = e.Bounds.Top + (e.Bounds.Height - image.Height) / 2;
                e.Graphics.DrawImage(image, e.Bounds.Left + 5, imageY, image.Width, image.Height);
                textRect.X += image.Width + 3;
                textRect.Width -= image.Width + 3;
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem.Text,
                listView.Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        };
    }

    private static Button BaseButton(string text)
    {
        Button button = new()
        {
            Dock = DockStyle.Fill,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.25F),
            Margin = new Padding(5, 3, 5, 3),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(190, 201, 212);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(217, 230, 237);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(197, 215, 224);
        return button;
    }

    private ListView CreateFileListView()
    {
        ListView listView = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            BackColor = PanelBack,
            ForeColor = Ink,
            SmallImageList = fileIcons,
            Scrollable = true
        };
        ApplyListViewChrome(listView);
        listView.MouseMove += (_, e) =>
        {
            ListViewItem? current = listView.GetItemAt(e.X, e.Y);
            if (ReferenceEquals(listView.Tag, current))
                return;

            if (listView.Tag is ListViewItem previous)
                previous.BackColor = PanelBack;

            if (current != null)
                current.BackColor = Color.FromArgb(238, 244, 247);

            listView.Tag = current;
        };
        listView.MouseLeave += (_, _) =>
        {
            if (listView.Tag is ListViewItem previous)
                previous.BackColor = PanelBack;
            listView.Tag = null;
        };
        listView.Columns.Add("Tên", 280);
        listView.Columns.Add("Loại", 90);
        listView.Columns.Add("KB/MB", 110);
        listView.Columns.Add("Cập nhật", 150);
        listView.Resize += (_, _) => FitFileColumns(listView);
        listView.HandleCreated += (_, _) => FitFileColumns(listView);
        return listView;
    }

    private static ListView CreateLogListView()
    {
        ListView listView = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            BorderStyle = BorderStyle.None,
            BackColor = PanelBack,
            ForeColor = Ink,
            Scrollable = true
        };
        ApplyListViewChrome(listView);
        listView.Columns.Add("", 34);
        listView.Columns.Add("Giờ", 86);
        listView.Columns.Add("Loại", 96);
        listView.Columns.Add("Nội dung", 900);
        listView.Resize += (_, _) => FitLogColumns(listView);
        listView.HandleCreated += (_, _) => FitLogColumns(listView);
        return listView;
    }

    private void InitializeFileIcons()
    {
        fileIcons.ColorDepth = ColorDepth.Depth32Bit;
        fileIcons.ImageSize = new Size(16, 16);
        fileIcons.Images.Add("folder", DrawIconBitmap(Color.FromArgb(229, 171, 64), Color.FromArgb(255, 216, 115), true));
        fileIcons.Images.Add("file", DrawIconBitmap(Color.FromArgb(118, 138, 157), Color.FromArgb(238, 242, 246), false));
    }

    private static Bitmap DrawIconBitmap(Color stroke, Color fill, bool folder)
    {
        Bitmap bitmap = new(16, 16);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        if (folder)
        {
            using SolidBrush brush = new(fill);
            using Pen pen = new(stroke, 1.2F);
            g.FillRectangle(brush, 2, 5, 12, 8);
            g.DrawRectangle(pen, 2, 5, 12, 8);
            g.FillRectangle(brush, 3, 3, 5, 3);
            g.DrawRectangle(pen, 3, 3, 5, 3);
        }
        else
        {
            using SolidBrush brush = new(fill);
            using Pen pen = new(stroke, 1.2F);
            g.FillRectangle(brush, 4, 2, 8, 12);
            g.DrawRectangle(pen, 4, 2, 8, 12);
            g.DrawLine(pen, 6, 6, 10, 6);
            g.DrawLine(pen, 6, 9, 10, 9);
        }

        return bitmap;
    }

    private void OpenRootFolder()
    {
        if (Directory.Exists(P2PSharedRoot))
            System.Diagnostics.Process.Start("explorer.exe", P2PSharedRoot);
    }

    private void AddFileToRoot()
    {
        using OpenFileDialog dialog = new() { Multiselect = true };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            foreach (string file in dialog.FileNames)
            {
                string dest = Path.Combine(P2PSharedRoot, Path.GetFileName(file));
                try
                {
                    File.Copy(file, dest, true);
                }
                catch (Exception ex)
                {
                    AddServerLog($"Lỗi copy file: {ex.Message}", LogType.Error);
                }
            }
            AddServerLog($"Đã thêm {dialog.FileNames.Length} file vào Root.", LogType.Success);
            LoadServerFiles();
        }
    }

    private void AddFolderToRoot()
    {
        using FolderBrowserDialog dialog = new();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            string sourcePath = dialog.SelectedPath;
            string destPath = Path.Combine(P2PSharedRoot, new DirectoryInfo(sourcePath).Name);
            
            try
            {
                CopyDirectory(sourcePath, destPath);
                AddServerLog($"Đã copy thư mục vào Root.", LogType.Success);
                LoadServerFiles();
            }
            catch (Exception ex)
            {
                AddServerLog($"Lỗi copy thư mục: {ex.Message}", LogType.Error);
            }
        }
    }

    private void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), true);

        foreach (var directory in Directory.GetDirectories(sourceDir))
            CopyDirectory(directory, Path.Combine(destinationDir, Path.GetFileName(directory)));
    }

    private void ChooseLocalFolder()
    {
        using FolderBrowserDialog dialog = new() { SelectedPath = localRoot };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        localRoot = dialog.SelectedPath;
        LoadLocalFiles();
        AddClientLog("Đã đổi thư mục tải về/local: " + localRoot, LogType.Success);
    }

    private void ToggleServer()
    {
        serverRunning = !serverRunning;

        if (serverRunning)
        {
            lblServerState.Text = "Đang chạy";
            lblServerState.DotColor = Success;
            lblServerState.Invalidate();
            btnToggleServer.Text = "Tắt server";
            
            try
            {
                int port = 8888;
                _server.Start(port);
                
                string displayName = "Server của tôi";
                if (System.Net.Dns.GetHostName() is string host) displayName = $"Máy {host}";
                
                _serverBroadcaster = new LanServerBroadcaster(displayName, port);
                _serverBroadcaster.Start();
                
                AddServerLog($"Server đã bật trên cổng {port}.", LogType.Success);
            }
            catch (Exception ex)
            {
                serverRunning = false;
                AddServerLog($"Lỗi bật server: {ex.Message}", LogType.Error);
            }
        }
        else
        {
            lblServerState.Text = "Đang dừng";
            lblServerState.DotColor = Danger;
            lblServerState.Invalidate();
            btnToggleServer.Text = "Bật server";
            
            _server.Stop();
            _serverBroadcaster?.StopAsync().GetAwaiter().GetResult();
            
            AddServerLog("Server đã dừng.", LogType.Info);
        }
    }

    private void LoadPermissionsDemo()
    {
        lvPermissions.Items.Clear();
        AddPermission("192.168.1.5", "Chỉ tải về", "Phòng của Tú");
        AddPermission("192.168.1.6", "Tải lên và tải về", "Phòng của Dũng");
        AddPermission("127.0.0.1", "Tải lên và tải về", "Local test");
    }

    private void AddPermission(string ip, string permission, string name)
    {
        ListViewItem item = new(ip);
        item.SubItems.Add(permission);
        item.SubItems.Add(name);
        item.ForeColor = permission.Contains("tải lên", StringComparison.OrdinalIgnoreCase) || permission.Contains("Toàn quyền", StringComparison.OrdinalIgnoreCase) ? Success : Ink;
        item.ToolTipText = $"{ip} - {permission} - {name}";
        lvPermissions.Items.Add(item);

        P2PFileSharingApp.Core.PermissionLevel level = permission switch
        {
            "Chỉ tải về" => P2PFileSharingApp.Core.PermissionLevel.ReadOnly,
            "Chỉ tải lên" => P2PFileSharingApp.Core.PermissionLevel.UploadOnly,
            "Tải lên và tải về" => P2PFileSharingApp.Core.PermissionLevel.ReadWrite,
            "Toàn quyền" => P2PFileSharingApp.Core.PermissionLevel.FullAccess,
            _ => P2PFileSharingApp.Core.PermissionLevel.ReadOnly
        };
        P2PFileSharingApp.Core.PermissionManager.SetPermission(ip, level);
    }

    private void AddOrUpdatePermission()
    {
        string ip = cboPermissionIp.Text.Trim();
        string name = txtPermissionName.Text.Trim();
        string permission = cboPermissionMode.SelectedItem?.ToString() ?? "Tải lên và tải về";

        if (string.IsNullOrWhiteSpace(ip))
        {
            AddServerLog("IP phân quyền không được để trống.", LogType.Error);
            cboPermissionIp.Focus();
            return;
        }

        var existing = lvPermissions.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Text == ip);
        if (existing == null)
        {
            AddPermission(ip, permission, string.IsNullOrWhiteSpace(name) ? ip : name);
            AddServerLog($"Đã thêm quyền '{permission}' cho {ip}.", LogType.Success);
            return;
        }

        existing.SubItems[1].Text = permission;
        existing.SubItems[2].Text = string.IsNullOrWhiteSpace(name) ? existing.SubItems[2].Text : name;
        existing.ForeColor = permission.Contains("tải lên", StringComparison.OrdinalIgnoreCase) || permission.Contains("Toàn quyền", StringComparison.OrdinalIgnoreCase) ? Success : Ink;
        existing.ToolTipText = $"{ip} - {permission} - {existing.SubItems[2].Text}";
        
        P2PFileSharingApp.Core.PermissionLevel level = permission switch
        {
            "Chỉ tải về" => P2PFileSharingApp.Core.PermissionLevel.ReadOnly,
            "Chỉ tải lên" => P2PFileSharingApp.Core.PermissionLevel.UploadOnly,
            "Tải lên và tải về" => P2PFileSharingApp.Core.PermissionLevel.ReadWrite,
            "Toàn quyền" => P2PFileSharingApp.Core.PermissionLevel.FullAccess,
            _ => P2PFileSharingApp.Core.PermissionLevel.ReadOnly
        };
        P2PFileSharingApp.Core.PermissionManager.SetPermission(ip, level);
        
        AddServerLog($"Đã cập nhật quyền '{permission}' cho {ip}.", LogType.Success);
    }

    private void LoadPeers()
    {
        peers.Clear();
        cboPeers.DataSource = peers;
    }

    private void FillSelectedPeer()
    {
        if (cboPeers.SelectedItem is not PeerInfo peer)
            return;

        LoadPeerIntoInputs(peer);
    }

    private void LoadPeerIntoInputs(PeerInfo peer)
    {
        txtDisplayName.Text = peer.DisplayName;
        cboServers.Text = peer.IpAddress;
        numPort.Value = Math.Clamp(peer.Port, 1, 65535);
    }

    private void cboServers_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isBindingDiscoveredServers)
            return;

        if (cboServers.SelectedItem is DiscoveredServer selectedServer)
        {
            txtDisplayName.Text = selectedServer.DisplayName;
            numPort.Value = selectedServer.Port;
        }
    }

    private void SavePeerFromInputs()
    {
        if (!TryReadPeerInputs(out PeerInfo? input) || input == null)
            return;

        PeerInfo? existing = peers.FirstOrDefault(peer => peer.Key == input.Key);
        if (existing == null)
        {
            peers.Add(input);
            cboPeers.Refresh();
        }
        else
        {
            existing.DisplayName = input.DisplayName;
            existing.LastSeen = DateTime.Now;
            cboPeers.Refresh();
        }

        
        AddClientLog($"Đã lưu {input}.", LogType.Success);
    }

    private bool TryReadPeerInputs(out PeerInfo? peer)
    {
        peer = null;
        string ip = cboServers.Text.Trim(); 
        if (cboServers.SelectedItem is DiscoveredServer srv && IsSelectedDiscoveredServerText(ip, srv))
            ip = srv.IpAddress;
        string name = txtDisplayName.Text.Trim();
        int port = (int)numPort.Value;

        if (string.IsNullOrWhiteSpace(ip))
        {
            AddClientLog("IP không được để trống.", LogType.Error);
            cboServers.Focus();
            return false;
        }

        peer = new PeerInfo
        {
            DisplayName = string.IsNullOrWhiteSpace(name) ? ip : name,
            IpAddress = ip,
            Port = port,
            LastSeen = DateTime.Now
        };
        return true;
    }

    private static bool IsSelectedDiscoveredServerText(string text, DiscoveredServer server)
    {
        return string.Equals(text, server.ToString(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, server.DisplayName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, server.Endpoint, StringComparison.OrdinalIgnoreCase)
            || string.Equals(text, server.IpAddress, StringComparison.OrdinalIgnoreCase);
    }

    public void UpsertDiscoveredPeer(PeerInfo discoveredPeer)
    {
        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => UpsertDiscoveredPeer(discoveredPeer)));
            return;
        }

        PeerInfo? existing = peers.FirstOrDefault(peer => peer.Key == discoveredPeer.Key);
        if (existing == null)
        {
            peers.Add(discoveredPeer);
            cboPeers.Refresh();
            AddClientLog($"Phát hiện peer mới: {discoveredPeer}.", LogType.Info);
            return;
        }

        existing.DisplayName = discoveredPeer.DisplayName;
        existing.LastSeen = DateTime.Now;
        cboPeers.Refresh();
    }

    private async void btnConnect_Click(object? sender, EventArgs e)
    {
        if (!TryReadPeerInputs(out PeerInfo? peer) || peer == null)
            return;

        
        btnConnect.Text = "Đang kết nối...";
        progressConnect.Visible = true;
        progressConnect.Style = ProgressBarStyle.Marquee;
        lblConnection.Text = "Đang kết nối...";
        lblConnection.ForeColor = Muted;
        lblConnection.BackColor = WarningSoft;
        connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

        try
        {
            _client?.Disconnect();
            _client = new P2PClient();
            _client.OnLog += msg => AddClientLog(msg, LogType.Info);
            
            var res = await _client.ConnectAsync(peer.IpAddress, peer.Port);
            if (!res.ok) throw new Exception(res.message);

            isConnected = true;
            remotePermission = res.permission;
            lblConnection.Text = $"Đã kết nối ({remotePermission})";
            lblConnection.ForeColor = Success;
            lblConnection.BackColor = SuccessSoft;
            AddClientLog($"Đã kết nối thành công tới {peer.IpAddress}:{peer.Port}", LogType.Success);

            btnConnect.Text = "Ngắt kết nối";
            await LoadRemoteFilesAsync();
        }
        catch (OperationCanceledException)
        {
            isConnected = false;
            lblConnection.Text = "Quá thời gian chờ";
            lblConnection.ForeColor = Danger;
            lblConnection.BackColor = DangerSoft;
            AddClientLog("Kết nối bị hủy hoặc quá thời gian chờ.", LogType.Error);
        }
        catch (Exception ex)
        {
            isConnected = false;
            remotePermission = "ReadOnly";
            lblConnection.Text = "Kết nối thất bại";
            lblConnection.ForeColor = Danger;
            lblConnection.BackColor = DangerSoft;
            AddClientLog("Kết nối thất bại: " + ex.Message, LogType.Error);
        }
        finally
        {
            connectCts?.Dispose();
            connectCts = null;
            progressConnect.Style = ProgressBarStyle.Blocks;
            progressConnect.Visible = false;
            if(!isConnected) btnConnect.Text = "Kết nối";
            
            UpdateActionState();
        }
    }

    private void LoadLocalFiles()
    {
        if (lvLocalFiles == null)
            return;

        lvLocalFiles.Items.Clear();
        AddDirectoryItems(lvLocalFiles, localRoot);
    }

    private void LoadServerFiles()
    {
        if (lvServerFiles == null)
            return;

        lvServerFiles.Items.Clear();

        if (Directory.Exists(P2PSharedRoot))
            AddDirectoryItems(lvServerFiles, P2PSharedRoot);
    }

    private void AddDirectoryItems(ListView target, string rootPath)
    {
        if (!Directory.Exists(rootPath))
            return;

        DirectoryInfo root = new(rootPath);
        foreach (DirectoryInfo directory in root.GetDirectories())
            target.Items.Add(CreateFileItem(directory.Name, "Thư mục", "", directory.LastWriteTime, directory.FullName));

        foreach (FileInfo file in root.GetFiles())
            target.Items.Add(CreateFileItem(file.Name, "Tệp", FormatBytes(file.Length), file.LastWriteTime, file.FullName));
    }

    private async Task LoadRemoteFilesAsync()
    {
        if (_client == null || !isConnected) return;
        
        lvRemoteFiles.Items.Clear();
        string response = await _client.GetListAsync("");
        var parts = response.Split('|');
        if (parts.Length > 0 && parts[0] == P2PFileSharingApp.Network.ProtocolMessages.RES_LIST)
        {
            for (int i = 1; i < parts.Length; i++)
            {
                string item = parts[i];
                if (string.IsNullOrEmpty(item)) continue;
                
                if (item.StartsWith("[DIR]"))
                {
                    string name = item.Substring(5);
                    lvRemoteFiles.Items.Add(CreateFileItem(name, "Thư mục", "", DateTime.Now, name));
                }
                else if (item.StartsWith("[FILE]"))
                {
                    string fileData = item.Substring(6);
                    var fileParts = fileData.Split('*');
                    string name = fileParts[0];
                    long size = fileParts.Length > 1 && long.TryParse(fileParts[1], out long s) ? s : 0;
                    lvRemoteFiles.Items.Add(CreateFileItem(name, "Tệp", FormatBytes(size), DateTime.Now, name));
                }
            }
        }
        else
        {
            AddClientLog("Không thể tải danh sách file từ server.", LogType.Error);
        }
    }

    private static ListViewItem CreateFileItem(string name, string type, string size, DateTime modified, string fullPath)
    {
        ListViewItem item = new(name);
        item.ImageKey = type == "Thư mục" ? "folder" : "file";
        item.SubItems.Add(type);
        item.SubItems.Add(size);
        item.SubItems.Add(modified.ToString("yyyy-MM-dd HH:mm"));
        item.Tag = fullPath;
        return item;
    }

    private async Task UploadSelectedLocalFilesAsync()
    {
        List<string> files = lvLocalFiles.SelectedItems
            .Cast<ListViewItem>()
            .Select(item => item.Tag?.ToString() ?? "")
            .Where(File.Exists)
            .ToList();

        if (files.Count == 0)
        {
            AddClientLog("Hãy chọn ít nhất một file trên máy mình để tải lên.", LogType.Error);
            return;
        }

        await UploadFilesAsync(files);
    }

    private async Task UploadFilesAsync(IEnumerable<string> filePaths)
    {
        if (!isConnected)
        {
            AddClientLog("Chưa kết nối tới server khác.", LogType.Error);
            return;
        }

        SetTransferRunning(true);
        transferCts = new CancellationTokenSource();

        Progress<TransferProgress> progress = new(UpdateTransferProgress);

        try
        {
            foreach (string filePath in filePaths)
            {
                AddClientLog("Bắt đầu tải lên: " + Path.GetFileName(filePath), LogType.Info);
                await _client!.UploadAsync(filePath, "", progress, transferCts.Token);
                AddUploadedFileToRemote(filePath);
                AddClientLog("Tải lên thành công: " + Path.GetFileName(filePath), LogType.Success);
            }
        }
        catch (OperationCanceledException)
        {
            AddClientLog("Đã hủy truyền file.", LogType.Error);
        }
        catch (IOException ex)
        {
            AddClientLog("Không đọc được file: " + ex.Message, LogType.Error);
        }
        catch (Exception ex)
        {
            AddClientLog("Tải lên thất bại: " + ex.Message, LogType.Error);
        }
        finally
        {
            transferCts?.Dispose();
            transferCts = null;
            SetTransferRunning(false);
        }
    }

    private void AddUploadedFileToRemote(string filePath)
    {
        FileInfo fileInfo = new(filePath);
        lvRemoteFiles.Items.Add(CreateFileItem(
            fileInfo.Name,
            "Tệp",
            FormatBytes(fileInfo.Length),
            DateTime.Now,
            "/" + fileInfo.Name));
    }

    private async Task DownloadSelectedRemoteFilesAsync()
    {
        List<ListViewItem> files = lvRemoteFiles.SelectedItems
            .Cast<ListViewItem>()
            .Where(item => item.SubItems[1].Text == "Tệp")
            .ToList();

        if (files.Count == 0)
        {
            AddClientLog("Hãy chọn ít nhất một file trên server khác để tải về.", LogType.Error);
            return;
        }

        SetTransferRunning(true);
        transferCts = new CancellationTokenSource();
        Progress<TransferProgress> progress = new(UpdateTransferProgress);

        try
        {
            Directory.CreateDirectory(localRoot);

            foreach (ListViewItem item in files)
            {
                long bytes = ParseDisplaySize(item.SubItems[2].Text);
                string safeFileName = SanitizeFileName(item.Text);
                string destinationPath = GetUniqueDownloadPath(Path.Combine(localRoot, safeFileName));

                AddClientLog($"Bắt đầu tải về: {item.Text} → {localRoot}", LogType.Info);
                await _client!.DownloadAsync(item.Text, Path.GetDirectoryName(destinationPath)!, progress, transferCts.Token);
                AddClientLog("Tải về thành công: " + destinationPath, LogType.Success);
            }

            LoadLocalFiles();
        }
        catch (OperationCanceledException)
        {
            AddClientLog("Đã hủy tải về.", LogType.Error);
        }
        catch (IOException ex)
        {
            AddClientLog("Không ghi được file tải về: " + ex.Message, LogType.Error);
        }
        finally
        {
            transferCts?.Dispose();
            transferCts = null;
            SetTransferRunning(false);
        }
    }

    private async void DeleteSelectedRemoteFiles()
    {
        if (_client == null || !isConnected)
        {
            AddClientLog("Chưa kết nối tới server.", LogType.Error);
            return;
        }

        if (lvRemoteFiles.SelectedItems.Count == 0)
        {
            AddClientLog("Hãy chọn file trên server khác cần xóa.", LogType.Error);
            return;
        }

        foreach (ListViewItem item in lvRemoteFiles.SelectedItems.Cast<ListViewItem>().ToList())
        {
            string fileName = item.Text;
            var res = await _client.DeleteAsync(fileName);
            if (res.ok)
            {
                lvRemoteFiles.Items.Remove(item);
                AddClientLog("Đã xóa file trên server khác: " + fileName, LogType.Success);
            }
            else
            {
                AddClientLog($"Lỗi xóa '{fileName}': {res.msg}", LogType.Error);
            }
        }
    }

    private void UpdateTransferProgress(TransferProgress progress)
    {
        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => UpdateTransferProgress(progress)));
            return;
        }

        int percent = progress.TotalBytes <= 0
            ? 0
            : (int)Math.Min(100, progress.BytesDone * 100 / progress.TotalBytes);

        progressTransfer.Value = percent;
        lblTransfer.ForeColor = Accent;
        lblTransfer.Text = $"↕ {progress.FileName}: {percent}% - {FormatBytes(progress.BytesDone)} / {FormatBytes(progress.TotalBytes)} - {progress.MegabytesPerSecond:0.00} MB/s";
    }

    private void SetTransferRunning(bool running)
    {
        UpdateActionState();
        btnCancelTransfer.Enabled = true;

        if (!running)
        {
            progressTransfer.Value = 0;
            lblTransfer.ForeColor = Success;
            lblTransfer.Text = "✓ Sẵn sàng truyền file - 0%";
        }
    }

    private void UpdateActionState()
    {
        bool hasSelection = lvRemoteFiles.SelectedItems.Count > 0;
        
        bool canUpload = remotePermission == "UploadOnly" || remotePermission == "ReadWrite" || remotePermission == "FullAccess";
        bool canDownload = remotePermission == "ReadOnly" || remotePermission == "ReadWrite" || remotePermission == "FullAccess";
        bool canEdit = remotePermission == "FullAccess";

        btnUpload.Enabled = isConnected && canUpload;
        btnDownload.Enabled = isConnected && hasSelection && canDownload;
        btnDelete.Enabled = isConnected && hasSelection && canEdit;
    }

    private void lvRemoteFiles_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private async void lvRemoteFiles_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data == null)
            return;

        string[] dropped = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        List<string> files = dropped.Where(File.Exists).ToList();
        if (files.Count == 0)
        {
            AddClientLog("Chỉ hỗ trợ kéo thả file, không tải lên thư mục.", LogType.Error);
            return;
        }

        await UploadFilesAsync(files);
    }

    private void AddServerLog(string message, LogType type)
    {
        AddLog(lvServerLogs, message, type);
    }

    private void AddClientLog(string message, LogType type)
    {
        AddLog(lvClientLogs, message, type);
    }

    private void AddLog(ListView target, string message, LogType type)
    {
        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => AddLog(target, message, type)));
            return;
        }

        ListViewItem item = new(ToLogSymbol(type));
        item.SubItems.Add(DateTime.Now.ToString("HH:mm:ss"));
        item.SubItems.Add(ToVietnameseLogType(type));
        item.SubItems.Add(message);
        item.UseItemStyleForSubItems = false;
        Color textColor = type switch
        {
            LogType.Success => Success,
            LogType.Error => Danger,
            _ => Accent
        };
        item.BackColor = type switch
        {
            LogType.Success => SuccessSoft,
            LogType.Error => DangerSoft,
            _ => Color.FromArgb(246, 248, 250)
        };
        item.SubItems[0].ForeColor = textColor;
        item.SubItems[1].ForeColor = Color.FromArgb(132, 144, 158);
        item.SubItems[2].ForeColor = textColor;
        item.SubItems[3].ForeColor = type == LogType.Error ? Danger : type == LogType.Success ? Success : Ink;

        target.Items.Add(item);
        target.EnsureVisible(target.Items.Count - 1);
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }

    private static string ToVietnameseLogType(LogType type)
    {
        return type switch
        {
            LogType.Success => "Thành công",
            LogType.Error => "Lỗi",
            _ => "Thông tin"
        };
    }

    private static string ToLogSymbol(LogType type)
    {
        return type switch
        {
            LogType.Success => "✓",
            LogType.Error => "✕",
            _ => "ⓘ"
        };
    }

    private static long ParseDisplaySize(string text)
    {
        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !double.TryParse(parts[0], out double value))
            return 10 * 1024 * 1024;

        return parts[1].ToUpperInvariant() switch
        {
            "KB" => (long)(value * 1024),
            "MB" => (long)(value * 1024 * 1024),
            "GB" => (long)(value * 1024 * 1024 * 1024),
            _ => (long)value
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        string name = Path.GetFileName(fileName);
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(name) ? "download.bin" : name;
    }

    private static string GetUniqueDownloadPath(string destinationPath)
    {
        if (!File.Exists(destinationPath))
            return destinationPath;

        string directory = Path.GetDirectoryName(destinationPath) ?? "";
        string fileName = Path.GetFileNameWithoutExtension(destinationPath);
        string extension = Path.GetExtension(destinationPath);

        for (int index = 1; index < 10_000; index++)
        {
            string candidate = Path.Combine(directory, $"{fileName} ({index}){extension}");
            if (!File.Exists(candidate))
                return candidate;
        }

        return Path.Combine(directory, $"{fileName}-{DateTime.Now:yyyyMMddHHmmss}{extension}");
    }

    private void FitPermissionColumns()
    {
        if (lvPermissions.Columns.Count < 3 || lvPermissions.ClientSize.Width <= 0)
            return;

        int width = Math.Max(500, lvPermissions.ClientSize.Width - 8);
        lvPermissions.Columns[0].Width = 128;
        lvPermissions.Columns[1].Width = 210;
        lvPermissions.Columns[2].Width = Math.Max(160, width - 338);
    }

    private static void FitFileColumns(ListView listView)
    {
        if (listView.Columns.Count < 4 || listView.ClientSize.Width <= 0)
            return;

        int width = Math.Max(680, listView.ClientSize.Width - 8);
        int typeWidth = 92;
        int sizeWidth = 92;
        int dateWidth = 150;
        int nameWidth = Math.Max(330, width - typeWidth - sizeWidth - dateWidth);

        listView.Columns[0].Width = nameWidth;
        listView.Columns[1].Width = typeWidth;
        listView.Columns[2].Width = sizeWidth;
        listView.Columns[3].Width = dateWidth;
    }

    private static void FitLogColumns(ListView listView)
    {
        if (listView.Columns.Count < 4 || listView.ClientSize.Width <= 0)
            return;

        int width = Math.Max(780, listView.ClientSize.Width - 8);
        int iconWidth = 34;
        int timeWidth = 84;
        int typeWidth = 96;

        listView.Columns[0].Width = iconWidth;
        listView.Columns[1].Width = timeWidth;
        listView.Columns[2].Width = typeWidth;
        listView.Columns[3].Width = Math.Max(180, width - iconWidth - timeWidth - typeWidth);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
