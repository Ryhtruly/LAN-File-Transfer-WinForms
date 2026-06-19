using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace P2PFileSharingApp.Network
{
    /// <summary>
    /// Client kết nối tới một Server P2P khác.
    /// Cung cấp toàn bộ các lệnh: List, Download, Upload, Delete, Rename, MkDir.
    /// Kết quả trả về là tuple (success, message/data).
    /// </summary>
    public class P2PClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;

        public event Action<string>? OnLog;
        public bool IsConnected => _client?.Connected ?? false;
        public string MyDisplayName { get; set; } = "Máy khách";

        // ─────────────────── CONNECT / DISCONNECT ───────────────────

        public async Task<(bool ok, string message, string permission)> ConnectAsync(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                await _writer.WriteLineAsync($"HELLO|{MyDisplayName}");

                // Đọc câu chào từ Server để lấy phân quyền
                string? welcome = await _reader.ReadLineAsync();
                string permission = "ReadOnly"; // mặc định
                if (welcome != null)
                {
                    if (welcome.StartsWith(ProtocolMessages.RES_DENIED))
                    {
                        var parts = welcome.Split('|');
                        string rejectMsg = parts.Length > 1 ? parts[1] : "Bị từ chối.";
                        Log($"❌ Kết nối bị từ chối: {rejectMsg}");
                        return (false, rejectMsg, "ReadOnly");
                    }
                    else if (welcome.StartsWith(ProtocolMessages.RES_OK))
                    {
                        var parts = welcome.Split('|');
                        if (parts.Length > 1)
                            permission = parts[1];
                    }
                }

                Log($"✅ Kết nối thành công tới {ip}:{port} (Quyền: {permission})");
                return (true, "Kết nối thành công.", permission);
            }
            catch (Exception ex)
            {
                Log($"❌ Lỗi kết nối: {ex.Message}");
                return (false, ex.Message, "ReadOnly");
            }
        }

        public void Disconnect()
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _client?.Close();
            _client = null;
            Log("🔌 Đã ngắt kết nối.");
        }

        // ─────────────────── GENERIC SEND ───────────────────

        /// <summary>Gửi một lệnh và đợi dòng phản hồi đầu tiên.</summary>
        private async Task<string> SendLineAsync(string command)
        {
            if (!IsConnected) return $"{ProtocolMessages.RES_ERROR}|Chưa kết nối.";
            try
            {
                Log($"→ {command}");
                await _writer!.WriteLineAsync(command);
                string? resp = await _reader!.ReadLineAsync();
                Log($"← {resp}");
                return resp ?? $"{ProtocolMessages.RES_ERROR}|Không nhận được phản hồi.";
            }
            catch (Exception ex) { return $"{ProtocolMessages.RES_ERROR}|{ex.Message}"; }
        }

        // ─────────────────── COMMANDS ───────────────────

        public async Task<string> GetListAsync(string path = "")
            => await SendLineAsync($"{ProtocolMessages.REQ_LIST}{ProtocolMessages.SEPARATOR}{path}");

        public async Task<(bool ok, string msg)> DownloadAsync(string remotePath, string saveFolder, IProgress<P2PFileSharingApp.Models.TransferProgress>? progress = null, System.Threading.CancellationToken ct = default)
        {
            try
            {
                string fileName = Path.GetFileName(remotePath);
                string savePath = Path.Combine(saveFolder, fileName);
                string tmpPath = savePath + ".tmp";

                long offset = 0;
                if (File.Exists(tmpPath))
                {
                    offset = new FileInfo(tmpPath).Length;
                }

                string response = await SendLineAsync(
                    $"{ProtocolMessages.REQ_DOWNLOAD}{ProtocolMessages.SEPARATOR}{remotePath}{ProtocolMessages.SEPARATOR}{offset}");

                var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
                if (parts[0] != ProtocolMessages.RES_OK)
                    return (false, parts.Length > 1 ? parts[1] : "Lỗi không xác định.");

                if (!long.TryParse(parts[1], out long fileSize))
                    return (false, "Kích thước file không hợp lệ.");

                if (offset >= fileSize)
                {
                    offset = 0;
                    try { File.Delete(tmpPath); } catch {}

                    response = await SendLineAsync(
                        $"{ProtocolMessages.REQ_DOWNLOAD}{ProtocolMessages.SEPARATOR}{remotePath}{ProtocolMessages.SEPARATOR}0");
                    parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
                    if (parts[0] != ProtocolMessages.RES_OK)
                        return (false, parts.Length > 1 ? parts[1] : "Lỗi không xác định.");

                    if (!long.TryParse(parts[1], out fileSize))
                        return (false, "Kích thước file không hợp lệ.");
                }

                // Set dynamic timeout
                _stream!.ReadTimeout = 15000;
                _stream!.WriteTimeout = 15000;

                // Stream file in 8KB chunks (Fault Tolerance strategy)
                long totalReceived = offset;
                const int BUFFER_SIZE = 8192;
                byte[] buffer = new byte[BUFFER_SIZE];
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    using (var fs = offset > 0 ? File.Open(tmpPath, FileMode.OpenOrCreate, FileAccess.Write) : File.Create(tmpPath))
                    {
                        if (offset > 0)
                        {
                            fs.Seek(offset, SeekOrigin.Begin);
                        }

                        int bytesRead;
                        while (totalReceived < fileSize)
                        {
                            int toRead = (int)Math.Min(buffer.Length, fileSize - totalReceived);
                            bytesRead = await _stream!.ReadAsync(buffer, 0, toRead, ct);

                            if (bytesRead == 0) break;

                            fs.Write(buffer, 0, bytesRead);
                            totalReceived += bytesRead;

                            // Log every 1MB with progress %
                            if (totalReceived % (1024 * 1024) == 0 || totalReceived == fileSize)
                            {
                                int percent = (int)((totalReceived * 100) / fileSize);
                                double mbps = ((totalReceived - offset) / (1024.0 * 1024.0)) / stopwatch.Elapsed.TotalSeconds;
                                Log($"⬇ {remotePath} | {percent}% | {totalReceived / (1024.0 * 1024.0):F1}MB / {fileSize / (1024.0 * 1024.0):F1}MB | {mbps:F2}MB/s");
                                progress?.Report(new P2PFileSharingApp.Models.TransferProgress(totalReceived, fileSize, mbps, fileName));
                            }
                        }
                    }
                }
                finally
                {
                    // Restore timeout
                    _stream!.ReadTimeout = Timeout.Infinite;
                    _stream!.WriteTimeout = Timeout.Infinite;
                    stopwatch.Stop();
                }

                if (totalReceived == fileSize)
                {
                    if (File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }
                    File.Move(tmpPath, savePath);
                    return (true, $"Đã tải về: {savePath}");
                }
                else
                {
                    return (false, "Tải về bị gián đoạn.");
                }
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool ok, string msg)> UploadAsync(string localFilePath, string remoteDir, IProgress<P2PFileSharingApp.Models.TransferProgress>? progress = null, System.Threading.CancellationToken ct = default)
        {
            try
            {
                var fi = new FileInfo(localFilePath);
                string fileName = fi.Name;
                string remotePath = string.IsNullOrEmpty(remoteDir) ? fileName : $"{remoteDir}/{fileName}";

                string response = await SendLineAsync(
                    $"{ProtocolMessages.REQ_UPLOAD}{ProtocolMessages.SEPARATOR}{remotePath}{ProtocolMessages.SEPARATOR}{fi.Length}");

                // Expecting response: RES_OK|READY|offset
                var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 3);
                if (parts[0] != ProtocolMessages.RES_OK)
                    return (false, parts.Length > 1 ? parts[1] : "Server từ chối.");

                long offset = 0;
                if (parts.Length > 2 && parts[1] == "READY")
                {
                    long.TryParse(parts[2], out offset);
                }
                else if (parts.Length > 1 && parts[1].StartsWith("READY"))
                {
                    var readyParts = parts[1].Split('|');
                    if (readyParts.Length > 1)
                    {
                        long.TryParse(readyParts[1], out offset);
                    }
                }

                if (offset < 0 || offset >= fi.Length)
                {
                    offset = 0;
                }

                // Set dynamic timeout
                _stream!.ReadTimeout = 15000;
                _stream!.WriteTimeout = 15000;

                // Stream file in 8KB chunks (Fault Tolerance strategy)
                const int BUFFER_SIZE = 8192;
                byte[] buffer = new byte[BUFFER_SIZE];
                long totalSent = offset;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    using (var fs = File.OpenRead(localFilePath))
                    {
                        if (offset > 0)
                        {
                            fs.Seek(offset, SeekOrigin.Begin);
                        }

                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            await _stream!.WriteAsync(buffer, 0, bytesRead, ct);
                            totalSent += bytesRead;

                            // Log every 1MB with progress %
                            if (totalSent % (1024 * 1024) == 0 || fs.Position == fs.Length)
                            {
                                int percent = (int)((totalSent * 100) / fi.Length);
                                double mbps = ((totalSent - offset) / (1024.0 * 1024.0)) / stopwatch.Elapsed.TotalSeconds;
                                Log($"⬆ {fileName} | {percent}% | {totalSent / (1024.0 * 1024.0):F1}MB / {fi.Length / (1024.0 * 1024.0):F1}MB | {mbps:F2}MB/s");
                                progress?.Report(new P2PFileSharingApp.Models.TransferProgress(totalSent, fi.Length, mbps, fileName));
                            }
                        }
                    }
                    await _stream!.FlushAsync(ct);
                }
                finally
                {
                    // Restore timeout
                    _stream!.ReadTimeout = Timeout.Infinite;
                    _stream!.WriteTimeout = Timeout.Infinite;
                    stopwatch.Stop();
                }

                // Đợi ACK từ server
                string ack = await _reader!.ReadLineAsync(ct) ?? string.Empty;
                var ackParts = ack.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
                bool ok = ackParts[0] == ProtocolMessages.RES_OK;
                return (ok, ackParts.Length > 1 ? ackParts[1] : (ok ? "Upload thành công." : "Upload thất bại."));
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool ok, string msg)> UploadDirectoryAsync(string localDirPath, string remoteBaseDir)
        {
            if (!Directory.Exists(localDirPath)) return (false, "Thư mục không tồn tại.");
            
            try
            {
                var dirInfo = new DirectoryInfo(localDirPath);
                string remoteDirPath = string.IsNullOrEmpty(remoteBaseDir) ? dirInfo.Name : $"{remoteBaseDir}/{dirInfo.Name}";

                // 1. Create the remote directory
                var mkRes = await MkDirAsync(remoteDirPath);
                if (!mkRes.ok) return (false, $"Lỗi tạo thư mục: {mkRes.msg}");

                // 2. Upload all files in this directory
                foreach (var file in dirInfo.GetFiles())
                {
                    var upRes = await UploadAsync(file.FullName, remoteDirPath);
                    if (!upRes.ok) return (false, $"Lỗi tải file {file.Name}: {upRes.msg}");
                }

                // 3. Recursively upload subdirectories
                foreach (var subDir in dirInfo.GetDirectories())
                {
                    var subRes = await UploadDirectoryAsync(subDir.FullName, remoteDirPath);
                    if (!subRes.ok) return subRes;
                }

                return (true, "Đã upload toàn bộ thư mục thành công!");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool ok, string msg)> DeleteAsync(string remotePath)
        {
            string response = await SendLineAsync(
                $"{ProtocolMessages.REQ_DELETE}{ProtocolMessages.SEPARATOR}{remotePath}");
            var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
            bool ok = parts[0] == ProtocolMessages.RES_OK;
            return (ok, parts.Length > 1 ? parts[1] : (ok ? "Xóa thành công." : "Xóa thất bại."));
        }

        public async Task<(bool ok, string msg)> RenameAsync(string oldPath, string newName)
        {
            string response = await SendLineAsync(
                $"{ProtocolMessages.REQ_RENAME}{ProtocolMessages.SEPARATOR}{oldPath}{ProtocolMessages.SEPARATOR}{newName}");
            var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
            bool ok = parts[0] == ProtocolMessages.RES_OK;
            return (ok, parts.Length > 1 ? parts[1] : (ok ? "Đổi tên thành công." : "Thất bại."));
        }

        public async Task<(bool ok, string msg)> MkDirAsync(string remotePath)
        {
            string response = await SendLineAsync(
                $"{ProtocolMessages.REQ_MKDIR}{ProtocolMessages.SEPARATOR}{remotePath}");
            var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
            bool ok = parts[0] == ProtocolMessages.RES_OK;
            return (ok, parts.Length > 1 ? parts[1] : (ok ? "Tạo thư mục thành công." : "Thất bại."));
        }

        private void Log(string msg) => OnLog?.Invoke(msg);

        public void Dispose() => Disconnect();
    }
}
