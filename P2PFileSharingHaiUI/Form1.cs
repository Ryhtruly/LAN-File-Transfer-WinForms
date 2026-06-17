using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using P2PFileSharingHaiUI.Models;
using P2PFileSharingHaiUI.Services;

namespace P2PFileSharingHaiUI;

public partial class Form1 : Form
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
    private readonly PeerStore peerStore = new();
    private readonly FileTransferSimulator transferService = new();
    private readonly BindingList<string> sharedFolders = [];
    private readonly ToolTip toolTips = new();
    private readonly ImageList fileIcons = new();

    private CancellationTokenSource? connectCts;
    private CancellationTokenSource? transferCts;
    private string localRoot = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    private bool isConnected;
    private bool serverRunning;

    private TabControl mainTabs = null!;
    private Panel pageHost = null!;
    private Button btnServerTab = null!;
    private Button btnClientTab = null!;
    private Control serverView = null!;
    private Control clientView = null!;
    private ServerStatusIndicator lblServerState = null!;
    private Button btnToggleServer = null!;
    private Button btnAddSharedFolder = null!;
    private Button btnRemoveSharedFolder = null!;
    private Button btnRefreshServerFiles = null!;
    private ListView lvSharedFolders = null!;
    private ListView lvServerFiles = null!;
    private ListView lvPermissions = null!;
    private ListView lvServerLogs = null!;
    private TextBox txtPermissionIp = null!;
    private TextBox txtPermissionName = null!;
    private ComboBox cboPermissionMode = null!;
    private Button btnAddPermission = null!;

    private ComboBox cboPeers = null!;
    private TextBox txtDisplayName = null!;
    private TextBox txtIp = null!;
    private NumericUpDown numPort = null!;
    private CheckBox chkDemoMode = null!;
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

    public Form1()
    {
        InitializeComponent();
        Font = new Font("Segoe UI", 9.25F);
        toolTips.ShowAlways = true;
        InitializeFileIcons();
        sharedFolders.Add(localRoot);
        BuildUi();
        LoadPeers();
        LoadSharedFolders();
        LoadLocalFiles();
        LoadServerFiles();
        LoadRemoteDemoFiles();
        LoadPermissionsDemo();
        UpdateActionState();
        AddServerLog("Server đang dừng. Thêm thư mục chia sẻ rồi bấm Bật server.", LogType.Info);
        AddClientLog("Bật Chế độ demo để thử kết nối khi chưa có server thật.", LogType.Info);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        peerStore.Save(peers);
        connectCts?.Cancel();
        transferCts?.Cancel();
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

    private void MainTabs_DrawItem(object? sender, DrawItemEventArgs e)
    {
        TabPage page = mainTabs.TabPages[e.Index];
        bool selected = e.Index == mainTabs.SelectedIndex;
        Rectangle bounds = e.Bounds;
        bounds.Inflate(-4, -5);

        using GraphicsPath path = RoundedRect(bounds, 12);
        using SolidBrush fill = new(selected ? Accent : Color.Transparent);
        using Pen border = new(selected ? Accent : Color.FromArgb(216, 222, 229));
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        TextRenderer.DrawText(
            e.Graphics,
            page.Text,
            new Font("Segoe UI Semibold", 9.5F),
            bounds,
            selected ? Color.White : Ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
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

        Control sharedPanel = BuildSharedFoldersPanel();
        Control filesPanel = BuildServerFilesPanel();
        Control permissionsPanel = BuildPermissionsPanel();
        sharedPanel.Margin = new Padding(0, 0, 10, 12);
        filesPanel.Margin = new Padding(6, 0, 10, 12);
        permissionsPanel.Margin = new Padding(6, 0, 0, 12);
        page.Controls.Add(sharedPanel, 0, 1);
        page.Controls.Add(filesPanel, 1, 1);
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
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
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
            Text = "Chia sẻ nhiều thư mục cùng lúc. Mỗi IP có thể được cấp quyền tải lên, tải về hoặc cả hai.",
            AutoEllipsis = true,
            ForeColor = Muted,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(summary, 0, 1);

        btnAddSharedFolder = PrimaryButton("Thêm thư mục");
        btnAddSharedFolder.Click += (_, _) => AddSharedFolder();
        panel.Controls.Add(btnAddSharedFolder, 1, 1);

        btnRemoveSharedFolder = SecondaryButton("Gỡ thư mục");
        btnRemoveSharedFolder.Click += (_, _) => RemoveSelectedSharedFolder();
        panel.Controls.Add(btnRemoveSharedFolder, 2, 1);

        btnRefreshServerFiles = SecondaryButton("Làm mới");
        btnRefreshServerFiles.Click += (_, _) => LoadServerFiles();
        panel.Controls.Add(btnRefreshServerFiles, 3, 1);

        btnToggleServer = PrimaryButton("Bật server");
        btnToggleServer.Click += (_, _) => ToggleServer();
        panel.Controls.Add(btnToggleServer, 4, 1);

        return panel;
    }

    private Control BuildSharedFoldersPanel()
    {
        TableLayoutPanel panel = CreatePanel(1, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(SectionTitle("Thư mục chia sẻ"), 0, 0);
        lvSharedFolders = new ListView
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
        ApplyListViewChrome(lvSharedFolders);
        lvSharedFolders.Columns.Add("Tên thư mục", 150);
        lvSharedFolders.Columns.Add("Đường dẫn", 260);
        lvSharedFolders.Resize += (_, _) => FitSharedFolderColumns();
        lvSharedFolders.HandleCreated += (_, _) => FitSharedFolderColumns();
        panel.Controls.Add(lvSharedFolders, 0, 1);

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
        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = PanelBack
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

        txtPermissionIp = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "IP máy khách" };
        editor.Controls.Add(txtPermissionIp, 0, 0);
        txtPermissionName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Tên hiển thị" };
        editor.Controls.Add(txtPermissionName, 1, 0);

        cboPermissionMode = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cboPermissionMode.Items.AddRange(["Chỉ tải về", "Chỉ tải lên", "Tải lên và tải về"]);
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
        panel.Controls.Add(FieldLabel("Tùy chọn"), 5, 1);
        panel.Controls.Add(FieldLabel("Kết nối"), 6, 1);
        panel.Controls.Add(FieldLabel("Trạng thái"), 7, 1);

        cboPeers = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        cboPeers.SelectedIndexChanged += (_, _) => FillSelectedPeer();
        panel.Controls.Add(cboPeers, 0, 2);

        txtDisplayName = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "Phòng của Hải" };
        panel.Controls.Add(txtDisplayName, 1, 2);

        txtIp = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "192.168.1.10" };
        panel.Controls.Add(txtIp, 2, 2);

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

        chkDemoMode = new CheckBox
        {
            Dock = DockStyle.Fill,
            Text = "demo",
            Checked = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Ink
        };
        toolTips.SetToolTip(chkDemoMode, "Bật để mô phỏng kết nối khi chưa có server thật.");
        panel.Controls.Add(chkDemoMode, 5, 2);

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
        TableLayoutPanel panel = CreatePanel(2, 2);
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));

        panel.Controls.Add(SectionTitle("Thư mục để tải về"), 0, 0);
        btnChooseFolder = SecondaryButton("Chọn thư mục");
        btnChooseFolder.Click += (_, _) => ChooseLocalFolder();
        panel.Controls.Add(btnChooseFolder, 1, 0);

        lvLocalFiles = CreateFileListView();
        panel.SetColumnSpan(lvLocalFiles, 2);
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

    private void LoadSharedFolders()
    {
        lvSharedFolders.Items.Clear();

        foreach (string folder in sharedFolders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            DirectoryInfo info = new(folder);
            ListViewItem item = new(info.Name);
            item.SubItems.Add(ShortenPath(info.FullName, 42));
            item.Tag = info.FullName;
            item.ToolTipText = info.FullName;
            lvSharedFolders.Items.Add(item);
        }
    }

    private void AddSharedFolder()
    {
        using FolderBrowserDialog dialog = new() { SelectedPath = localRoot };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        if (sharedFolders.Any(path => string.Equals(path, dialog.SelectedPath, StringComparison.OrdinalIgnoreCase)))
        {
            AddServerLog("Thư mục này đã nằm trong danh sách chia sẻ.", LogType.Info);
            return;
        }

        sharedFolders.Add(dialog.SelectedPath);
        LoadSharedFolders();
        LoadServerFiles();
        AddServerLog("Đã thêm thư mục chia sẻ: " + dialog.SelectedPath, LogType.Success);
    }

    private void RemoveSelectedSharedFolder()
    {
        if (lvSharedFolders.SelectedItems.Count == 0)
        {
            AddServerLog("Hãy chọn thư mục cần gỡ khỏi danh sách chia sẻ.", LogType.Error);
            return;
        }

        foreach (ListViewItem item in lvSharedFolders.SelectedItems.Cast<ListViewItem>().ToList())
        {
            string path = item.Tag?.ToString() ?? "";
            string? existing = sharedFolders.FirstOrDefault(folder => string.Equals(folder, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                sharedFolders.Remove(existing);
        }

        LoadSharedFolders();
        LoadServerFiles();
        AddServerLog("Đã cập nhật danh sách thư mục chia sẻ.", LogType.Success);
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
            AddServerLog("Server đã bật trên cổng 8888.", LogType.Success);
        }
        else
        {
            lblServerState.Text = "Đang dừng";
            lblServerState.DotColor = Danger;
            lblServerState.Invalidate();
            btnToggleServer.Text = "Bật server";
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
        item.ForeColor = permission.Contains("tải lên", StringComparison.OrdinalIgnoreCase) ? Success : Ink;
        item.ToolTipText = $"{ip} - {permission} - {name}";
        lvPermissions.Items.Add(item);
    }

    private void AddOrUpdatePermission()
    {
        string ip = txtPermissionIp.Text.Trim();
        string name = txtPermissionName.Text.Trim();
        string permission = cboPermissionMode.SelectedItem?.ToString() ?? "Tải lên và tải về";

        if (string.IsNullOrWhiteSpace(ip))
        {
            AddServerLog("IP phân quyền không được để trống.", LogType.Error);
            txtPermissionIp.Focus();
            return;
        }

        ListViewItem? existing = lvPermissions.Items
            .Cast<ListViewItem>()
            .FirstOrDefault(item => string.Equals(item.Text, ip, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            AddPermission(ip, permission, string.IsNullOrWhiteSpace(name) ? ip : name);
            AddServerLog($"Đã thêm quyền '{permission}' cho {ip}.", LogType.Success);
            return;
        }

        existing.SubItems[1].Text = permission;
        existing.SubItems[2].Text = string.IsNullOrWhiteSpace(name) ? existing.SubItems[2].Text : name;
        existing.ForeColor = permission.Contains("tải lên", StringComparison.OrdinalIgnoreCase) ? Success : Ink;
        existing.ToolTipText = $"{ip} - {permission} - {existing.SubItems[2].Text}";
        AddServerLog($"Đã cập nhật quyền '{permission}' cho {ip}.", LogType.Success);
    }

    private void LoadPeers()
    {
        peers.Clear();
        foreach (PeerInfo peer in peerStore.Load())
            peers.Add(peer);

        cboPeers.DataSource = peers;
        if (peers.Count > 0)
            cboPeers.SelectedIndex = 0;
    }

    private void FillSelectedPeer()
    {
        if (cboPeers.SelectedItem is not PeerInfo peer)
            return;

        txtDisplayName.Text = peer.DisplayName;
        txtIp.Text = peer.IpAddress;
        numPort.Value = Math.Clamp(peer.Port, 1, 65535);
    }

    private void SavePeerFromInputs()
    {
        if (!TryReadPeerInputs(out PeerInfo? input) || input == null)
            return;

        PeerInfo? existing = peers.FirstOrDefault(peer => peer.Key == input.Key);
        if (existing == null)
        {
            peers.Add(input);
            cboPeers.SelectedItem = input;
        }
        else
        {
            existing.DisplayName = input.DisplayName;
            existing.LastSeen = DateTime.Now;
            cboPeers.Refresh();
        }

        peerStore.Save(peers);
        AddClientLog($"Đã lưu {input}.", LogType.Success);
    }

    private bool TryReadPeerInputs(out PeerInfo? peer)
    {
        peer = null;
        string ip = txtIp.Text.Trim();
        string name = txtDisplayName.Text.Trim();
        int port = (int)numPort.Value;

        if (string.IsNullOrWhiteSpace(ip))
        {
            AddClientLog("IP không được để trống.", LogType.Error);
            txtIp.Focus();
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

        btnConnect.Enabled = false;
        btnConnect.Text = "Đang kết nối...";
        progressConnect.Visible = true;
        progressConnect.Style = ProgressBarStyle.Marquee;
        lblConnection.Text = "Đang kết nối...";
        lblConnection.ForeColor = Muted;
        lblConnection.BackColor = WarningSoft;
        connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

        try
        {
            if (chkDemoMode.Checked)
            {
                await Task.Delay(900, connectCts.Token);
            }
            else
            {
                using TcpClient client = new();
                await client.ConnectAsync(peer.IpAddress, peer.Port, connectCts.Token);
            }

            isConnected = true;
            lblConnection.Text = "Đã kết nối";
            lblConnection.ForeColor = Success;
            lblConnection.BackColor = SuccessSoft;
            AddClientLog($"Kết nối thành công tới {peer}.", LogType.Success);
            SavePeerFromInputs();
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
            btnConnect.Text = "Kết nối";
            btnConnect.Enabled = true;
            UpdateActionState();
        }
    }

    private void LoadLocalFiles()
    {
        if (lvLocalFiles == null)
            return;

        lvLocalFiles.Items.Clear();
        AddDirectoryItems(lvLocalFiles);
    }

    private void LoadServerFiles()
    {
        if (lvServerFiles == null)
            return;

        lvServerFiles.Items.Clear();

        foreach (string folder in sharedFolders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
            AddSharedDirectoryItems(lvServerFiles, folder);
    }

    private void AddDirectoryItems(ListView target)
    {
        if (!Directory.Exists(localRoot))
            return;

        DirectoryInfo root = new(localRoot);
        foreach (DirectoryInfo directory in root.GetDirectories())
            target.Items.Add(CreateFileItem(directory.Name, "Thư mục", "", directory.LastWriteTime, directory.FullName));

        foreach (FileInfo file in root.GetFiles())
            target.Items.Add(CreateFileItem(file.Name, "Tệp", FormatBytes(file.Length), file.LastWriteTime, file.FullName));
    }

    private void AddSharedDirectoryItems(ListView target, string sharedFolder)
    {
        DirectoryInfo root = new(sharedFolder);
        string prefix = root.Name;

        foreach (DirectoryInfo directory in root.GetDirectories())
            target.Items.Add(CreateFileItem($"{prefix}\\{directory.Name}", "Thư mục", "", directory.LastWriteTime, directory.FullName));

        foreach (FileInfo file in root.GetFiles())
            target.Items.Add(CreateFileItem($"{prefix}\\{file.Name}", "Tệp", FormatBytes(file.Length), file.LastWriteTime, file.FullName));
    }

    private void LoadRemoteDemoFiles()
    {
        lvRemoteFiles.Items.Clear();
        lvRemoteFiles.Items.Add(CreateFileItem("Tài liệu", "Thư mục", "", DateTime.Now.AddDays(-2), "/Documents"));
        lvRemoteFiles.Items.Add(CreateFileItem("demo-report.pdf", "Tệp", "2.4 MB", DateTime.Now.AddHours(-5), "/Documents/demo-report.pdf"));
        lvRemoteFiles.Items.Add(CreateFileItem("video-demo.mp4", "Tệp", "118 MB", DateTime.Now.AddDays(-1), "/video-demo.mp4"));
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
        if (!isConnected && !chkDemoMode.Checked)
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
                await transferService.UploadAsync(filePath, progress, transferCts.Token);
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
                await transferService.DownloadMockAsync(item.Text, bytes, destinationPath, progress, transferCts.Token);
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

    private void DeleteSelectedRemoteFiles()
    {
        if (lvRemoteFiles.SelectedItems.Count == 0)
        {
            AddClientLog("Hãy chọn file trên server khác cần xóa.", LogType.Error);
            return;
        }

        foreach (ListViewItem item in lvRemoteFiles.SelectedItems.Cast<ListViewItem>().ToList())
        {
            if (item.Text.Contains("demo", StringComparison.OrdinalIgnoreCase))
            {
                AddClientLog($"Server từ chối xóa '{item.Text}': file đang được sử dụng.", LogType.Error);
                continue;
            }

            lvRemoteFiles.Items.Remove(item);
            AddClientLog("Đã xóa mục trên server khác: " + item.Text, LogType.Success);
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
        btnUpload.Enabled = !running;
        btnDownload.Enabled = !running;
        btnDelete.Enabled = !running;
        btnChooseFolder.Enabled = !running;
        btnCancelTransfer.Enabled = true;

        if (!running)
        {
            progressTransfer.Value = 0;
            lblTransfer.ForeColor = Success;
            lblTransfer.Text = "✓ Sẵn sàng truyền file - 0%";
            UpdateActionState();
        }
    }

    private void UpdateActionState()
    {
        bool allowRemoteActions = isConnected || chkDemoMode.Checked;
        btnUpload.Enabled = allowRemoteActions;
        btnDownload.Enabled = allowRemoteActions;
        btnDelete.Enabled = allowRemoteActions;
        btnCancelTransfer.Enabled = true;
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

    private static string ShortenPath(string path, int maxLength)
    {
        if (path.Length <= maxLength)
            return path;

        string root = Path.GetPathRoot(path) ?? "";
        string name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string prefix = string.IsNullOrWhiteSpace(root) ? "" : root.TrimEnd(Path.DirectorySeparatorChar);

        string compact = $"{prefix}\\...\\{name}";
        if (compact.Length <= maxLength)
            return compact;

        int keep = Math.Max(8, maxLength - 4);
        return path[..keep] + "...";
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

    private void FitSharedFolderColumns()
    {
        if (lvSharedFolders.Columns.Count < 2 || lvSharedFolders.ClientSize.Width <= 0)
            return;

        int width = Math.Max(430, lvSharedFolders.ClientSize.Width - 8);
        lvSharedFolders.Columns[0].Width = 150;
        lvSharedFolders.Columns[1].Width = Math.Max(280, width - 150);
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
