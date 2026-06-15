# LAN Auto Discovery cho phần của Dũng

Thư mục này chứa một bộ mã C# độc lập để làm phần:

- Server tự phát hiện và phát thông tin trong mạng LAN.
- Client tự lắng nghe server trong LAN.
- UI có thể thay ô nhập IP bằng dropdown danh sách server tìm được.

## Cách hoạt động

1. Server mở `LanServerBroadcaster`.
2. Cứ mỗi 3 giây, server gửi một gói UDP broadcast chứa:
   - `ServerId`
   - `DisplayName`
   - `IpAddress`
   - `Port`
3. Client mở `LanClientDiscoveryService` để nghe trên cùng `discoveryPort`.
4. Khi nhận được broadcast, client cập nhật danh sách server khả dụng.
5. Nếu server im lặng quá 10 giây, client tự xóa khỏi danh sách.

## File chính

- `LanServerBroadcaster.cs`: chạy ở phía server.
- `LanClientDiscoveryService.cs`: chạy ở phía client.
- `DiscoveredServer.cs`: model dữ liệu cho dropdown/UI.
- `WinFormsIntegrationExample.cs`: ví dụ ghép vào WinForms `ComboBox`.

## Cách ghép vào app server

Ví dụ khi server bắt đầu listen ở cổng `5000`:

```csharp
private LanServerBroadcaster? _broadcaster;

private void StartServer()
{
    // Tên này sẽ hiện trên dropdown client, ví dụ "Phòng của Dũng"
    _broadcaster = new LanServerBroadcaster("Phòng của Dũng", 5000);
    _broadcaster.Start();
}

private async Task StopServerAsync()
{
    if (_broadcaster is not null)
    {
        await _broadcaster.StopAsync();
        _broadcaster = null;
    }
}
```

## Cách ghép vào app client

Ví dụ thay ô nhập IP thủ công bằng `ComboBox`:

```csharp
private WinFormsIntegrationExample? _discoveryUi;

private void MainForm_Load(object sender, EventArgs e)
{
    _discoveryUi = new WinFormsIntegrationExample(cboServers);
    _discoveryUi.StartDiscovery();
}

private void btnConnect_Click(object sender, EventArgs e)
{
    var selected = _discoveryUi?.GetSelectedServer();
    if (selected is null)
    {
        MessageBox.Show("Chưa có server nào trong LAN.");
        return;
    }

    ConnectToServer(selected.IpAddress, selected.Port);
}
```

## Gợi ý ghép UI theo phân công

- Đổi textbox nhập IP thành `ComboBox`.
- Cho phép server nhập tên phòng, ví dụ:
  - `Phòng của Dũng`
  - `Room của Hải`
- Bind `ComboBox` theo `DisplayName`, nhưng khi connect thì dùng `IpAddress` và `Port`.

## Lưu ý kỹ thuật

- Cơ chế này dùng UDP broadcast nên phù hợp mạng LAN nội bộ.
- Một số mạng/router có thể chặn broadcast giữa VLAN khác nhau.
- Nếu app dùng WPF thay vì WinForms, giữ nguyên service, chỉ đổi phần bind UI.
