using System.Drawing.Drawing2D;
using P2PFileSharingApp.Core;

namespace P2PFileSharingApp.UI;

public class SplashForm : Form
{
    private Label titleLabel;
    private Label subtitleLabel;
    private ProgressBar progressBar;
    private Panel onboardingPanel;
    private TextBox txtName;
    private TextBox txtPath;
    private PictureBox logoPic;

    public SplashForm()
    {
        SettingsManager.Load();

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(680, 420);
        BackColor = Color.FromArgb(252, 253, 254);
        ShowInTaskbar = true;

        logoPic = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(80, 80),
            Location = new Point(170, 120),
            Visible = true
        };
        try
        {
            logoPic.Image = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo_n.png"));
        }
        catch { }
        Controls.Add(logoPic);

        titleLabel = new Label
        {
            Text = "PeerLink",
            Font = new Font("Segoe UI", 42, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 111, 110),
            AutoSize = true,
            Location = new Point(260, 120)
        };
        Controls.Add(titleLabel);

        subtitleLabel = new Label
        {
            Text = "Đang khởi động hệ thống mạng...",
            Font = new Font("Segoe UI", 12),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Location = new Point(220, 210)
        };
        Controls.Add(subtitleLabel);

        progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            Bounds = new Rectangle(140, 360, 400, 4),
            Visible = false
        };
        Controls.Add(progressBar);

        BuildOnboardingPanel();
    }

    private void BuildOnboardingPanel()
    {
        onboardingPanel = new Panel
        {
            Bounds = new Rectangle(0, 200, 680, 220),
            Visible = false
        };

        Label lblWelcome = new Label { Text = "Chào mừng bạn lần đầu sử dụng PeerLink!", Font = new Font("Segoe UI Semibold", 13), ForeColor = Color.FromArgb(31, 41, 55), AutoSize = true, Location = new Point(60, 0) };
        onboardingPanel.Controls.Add(lblWelcome);

        Label lblName = new Label { Text = "Tên hiển thị:", Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(60, 50) };
        onboardingPanel.Controls.Add(lblName);

        txtName = new TextBox { Bounds = new Rectangle(200, 47, 410, 30), Font = new Font("Segoe UI", 11.5F), Text = SettingsManager.Current.DisplayName };
        onboardingPanel.Controls.Add(txtName);

        Label lblPath = new Label { Text = "Thư mục:", Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(100, 116, 139), AutoSize = true, Location = new Point(60, 95) };
        onboardingPanel.Controls.Add(lblPath);

        string defaultPath = SettingsManager.Current.DefaultDownloadPath;
        if (string.IsNullOrWhiteSpace(defaultPath))
        {
            defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "PeerLink");
        }

        txtPath = new TextBox { Bounds = new Rectangle(200, 92, 310, 30), Font = new Font("Segoe UI", 11.5F), Text = defaultPath };
        onboardingPanel.Controls.Add(txtPath);

        SplashRoundedButton btnBrowse = new SplashRoundedButton { Text = "Chọn...", Bounds = new Rectangle(520, 90, 90, 32), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(226, 232, 240), ForeColor = Color.FromArgb(31, 41, 55) };
        btnBrowse.Click += (s, e) =>
        {
            using var fbd = new FolderBrowserDialog { SelectedPath = txtPath.Text };
            if (fbd.ShowDialog() == DialogResult.OK) txtPath.Text = fbd.SelectedPath;
        };
        onboardingPanel.Controls.Add(btnBrowse);

        SplashRoundedButton btnStart = new SplashRoundedButton { Text = "Bắt đầu", Bounds = new Rectangle(270, 150, 140, 40), Font = new Font("Segoe UI Semibold", 11F), BackColor = Color.FromArgb(31, 111, 110), ForeColor = Color.White };
        btnStart.Click += (s, e) => FinishOnboarding();
        onboardingPanel.Controls.Add(btnStart);

        Controls.Add(onboardingPanel);
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        if (SettingsManager.Current.IsFirstRun)
        {
            subtitleLabel.Visible = false;
            onboardingPanel.Visible = true;
            logoPic.Location = new Point(170, 60); // move logo up
            titleLabel.Location = new Point(260, 60); // move title up
        }
        else
        {
            progressBar.Visible = true;
            // Fake loading logic to let user see the splash screen and warm up
            await Task.Delay(1500);
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void FinishOnboarding()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Vui lòng nhập tên hiển thị!");
            return;
        }

        try { Directory.CreateDirectory(txtPath.Text); }
        catch { MessageBox.Show("Thư mục lưu không hợp lệ!"); return; }

        var confirm = MessageBox.Show(
            "Thư mục bạn chọn sẽ là thư mục dùng để chia sẻ file của bạn cho các máy khác trong mạng tải về.\n\n(Lưu ý: Khi bạn tải file từ máy khác về, ứng dụng sẽ hỏi lại thư mục lưu riêng).\n\nBạn có chắc chắn muốn dùng thư mục này không?", 
            "Xác nhận thư mục chia sẻ", 
            MessageBoxButtons.YesNo, 
            MessageBoxIcon.Question);

        if (confirm == DialogResult.No)
        {
            return; // stay on the onboarding screen
        }

        SettingsManager.Current.DisplayName = txtName.Text.Trim();
        SettingsManager.Current.DefaultDownloadPath = txtPath.Text.Trim();
        SettingsManager.Current.IsFirstRun = false;
        SettingsManager.Save();

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Draw a nice border
        using Pen border = new Pen(Color.FromArgb(200, 210, 220), 1);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    private class SplashRoundedButton : Button
    {
        private int radius = 8;
        public SplashRoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);
            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            
            int d = radius * 2;
            using GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            
            using SolidBrush brush = new(BackColor);
            e.Graphics.FillPath(brush, path);
            
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}
