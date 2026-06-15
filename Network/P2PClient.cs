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

        // ─────────────────── CONNECT / DISCONNECT ───────────────────

        public async Task<(bool ok, string message)> ConnectAsync(string ip, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                Log($"✅ Kết nối thành công tới {ip}:{port}");
                return (true, "Kết nối thành công.");
            }
            catch (Exception ex)
            {
                Log($"❌ Lỗi kết nối: {ex.Message}");
                return (false, ex.Message);
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

        public async Task<(bool ok, string msg)> DownloadAsync(string remotePath, string saveFolder)
        {
            try
            {
                string response = await SendLineAsync(
                    $"{ProtocolMessages.REQ_DOWNLOAD}{ProtocolMessages.SEPARATOR}{remotePath}");

                var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
                if (parts[0] != ProtocolMessages.RES_OK)
                    return (false, parts.Length > 1 ? parts[1] : "Lỗi không xác định.");

                if (!long.TryParse(parts[1], out long fileSize))
                    return (false, "Kích thước file không hợp lệ.");

                string savePath = Path.Combine(saveFolder, Path.GetFileName(remotePath));

                // Stream file in 8KB chunks (Fault Tolerance strategy)
                long totalReceived = 0;
                const int BUFFER_SIZE = 8192;
                byte[] buffer = new byte[BUFFER_SIZE];
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                using (var fs = File.Create(savePath))
                {
                    int bytesRead;
                    while (totalReceived < fileSize)
                    {
                        int toRead = (int)Math.Min(buffer.Length, fileSize - totalReceived);
                        bytesRead = await _stream!.ReadAsync(buffer, 0, toRead);

                        if (bytesRead == 0) break;

                        fs.Write(buffer, 0, bytesRead);
                        totalReceived += bytesRead;

                        // Log every 1MB with progress %
                        if (totalReceived % (1024 * 1024) == 0 || totalReceived == fileSize)
                        {
                            int percent = (int)((totalReceived * 100) / fileSize);
                            double mbps = (totalReceived / (1024.0 * 1024.0)) / stopwatch.Elapsed.TotalSeconds;
                            Log($"⬇ {remotePath} | {percent}% | {totalReceived / (1024.0 * 1024.0):F1}MB / {fileSize / (1024.0 * 1024.0):F1}MB | {mbps:F2}MB/s");
                        }
                    }
                }
                stopwatch.Stop();

                return (totalReceived == fileSize, $"Đã tải về: {savePath}");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool ok, string msg)> UploadAsync(string localFilePath, string remoteDir)
        {
            try
            {
                var fi = new FileInfo(localFilePath);
                string fileName = fi.Name;
                string remotePath = string.IsNullOrEmpty(remoteDir) ? fileName : $"{remoteDir}/{fileName}";

                string response = await SendLineAsync(
                    $"{ProtocolMessages.REQ_UPLOAD}{ProtocolMessages.SEPARATOR}{remotePath}{ProtocolMessages.SEPARATOR}{fi.Length}");

                var parts = response.Split(new char[] { ProtocolMessages.SEPARATOR }, 2);
                if (parts[0] != ProtocolMessages.RES_OK)
                    return (false, parts.Length > 1 ? parts[1] : "Server từ chối.");

                // Stream file in 8KB chunks (Fault Tolerance strategy)
                const int BUFFER_SIZE = 8192;
                byte[] buffer = new byte[BUFFER_SIZE];
                long totalSent = 0;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                using (var fs = File.OpenRead(localFilePath))
                {
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        await _stream!.WriteAsync(buffer, 0, bytesRead);
                        totalSent += bytesRead;

                        // Log every 1MB with progress %
                        if (totalSent % (1024 * 1024) == 0 || fs.Position == fs.Length)
                        {
                            int percent = (int)((totalSent * 100) / fi.Length);
                            double mbps = (totalSent / (1024.0 * 1024.0)) / stopwatch.Elapsed.TotalSeconds;
                            Log($"⬆ {fileName} | {percent}% | {totalSent / (1024.0 * 1024.0):F1}MB / {fi.Length / (1024.0 * 1024.0):F1}MB | {mbps:F2}MB/s");
                        }
                    }
                }
                stopwatch.Stop();

                // Đợi ACK từ server
                string ack = await _reader!.ReadLineAsync() ?? string.Empty;
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
