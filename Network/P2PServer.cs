using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using P2PFileSharingApp.Core;

namespace P2PFileSharingApp.Network
{
    /// <summary>
    /// Server lắng nghe kết nối TCP đến từ các Client khác.
    /// - Mỗi Client được xử lý trên Thread riêng (không block giao diện).
    /// - Theo dõi danh sách Client đang kết nối realtime.
    /// - Kiểm tra phân quyền trước mỗi lệnh.
    /// - Phát hiện và cảnh báo xung đột file (File Locking).
    /// </summary>
    public class P2PServer
    {
        private TcpListener? _listener;
        private bool _isRunning;

        public string SharedFolderPath { get; set; } = string.Empty;

        // Danh sách các client đang kết nối: IP → TcpClient
        private readonly ConcurrentDictionary<string, TcpClient> _connectedClients
            = new ConcurrentDictionary<string, TcpClient>();

        // Events để giao diện cập nhật realtime
        public event Action<string>? OnLog;
        public event Action<string>? OnClientConnected;     // ip
        public event Action<string>? OnClientDisconnected;  // ip

        public bool IsRunning => _isRunning;

        // ─────────────────── START / STOP ───────────────────

        public void Start(int port)
        {
            if (_isRunning) return;
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _isRunning = true;
                Log($"✅ Server đang lắng nghe tại cổng {port}");

                var t = new Thread(ListenLoop) { IsBackground = true };
                t.Start();
            }
            catch (Exception ex) { Log("❌ Lỗi khởi động Server: " + ex.Message); }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            // Đóng tất cả kết nối đang mở
            foreach (var kv in _connectedClients)
                kv.Value.Close();
            _connectedClients.Clear();
            Log("🔴 Server đã dừng.");
        }

        public void DisconnectClient(string ip)
        {
            if (_connectedClients.TryGetValue(ip, out var client))
            {
                try { client.Close(); } catch { }
            }
        }

        // ─────────────────── LISTEN LOOP ───────────────────

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var client = _listener!.AcceptTcpClient();
                    var ip = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();

                    // Nếu IP này đã có kết nối cũ, đóng kết nối cũ đi
                    if (_connectedClients.TryRemove(ip, out var old))
                        old.Close();

                    _connectedClients[ip] = client;

                    // Đảm bảo IP mới luôn có quyền (mặc định ReadOnly)
                    var _ = PermissionManager.GetPermission(ip);

                    Log($"🟢 Kết nối mới từ: {ip}");
                    OnClientConnected?.Invoke(ip);

                    var t = new Thread(() => HandleClient(client, ip)) { IsBackground = true };
                    t.Start();
                }
                catch { if (!_isRunning) break; }
            }
        }

        // ─────────────────── HANDLE CLIENT ───────────────────

        private void HandleClient(TcpClient client, string clientIP)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                while (client.Connected && _isRunning)
                {
                    string? line = reader.ReadLine();
                    if (line == null) break;

                    Log($"[{clientIP}] ← {line}");
                    ProcessCommand(line, clientIP, stream, reader, writer);
                }
            }
            catch { /* client ngắt đột ngột */ }
            finally
            {
                _connectedClients.TryRemove(clientIP, out _);
                client.Close();
                Log($"🔴 Ngắt kết nối: {clientIP}");
                OnClientDisconnected?.Invoke(clientIP);
            }
        }

        // ─────────────────── PROCESS COMMAND ───────────────────

        private void ProcessCommand(string message, string clientIP,
            NetworkStream stream, StreamReader reader, StreamWriter writer)
        {
            // Tách tối đa 3 phần: COMMAND|ARG1|ARG2
            var parts = message.Split(new char[] { ProtocolMessages.SEPARATOR }, 3);
            string cmd = parts[0];

            // ── Kiểm tra quyền đọc trước tiên ──
            if (!PermissionManager.CanRead(clientIP))
            {
                writer.WriteLine($"{ProtocolMessages.RES_DENIED}|Bạn bị chặn khỏi server này.");
                return;
            }

            switch (cmd)
            {
                // ── LIST ──
                case ProtocolMessages.REQ_LIST:
                    string listRelPath = parts.Length > 1 ? parts[1] : "";
                    string? safeListPath = FileManager.GetSafePath(SharedFolderPath, listRelPath);
                    if (safeListPath == null) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Đường dẫn không hợp lệ."); break; }
                    string structure = FileManager.GetDirectoryStructure(safeListPath);
                    writer.WriteLine($"{ProtocolMessages.RES_LIST}|{structure}");
                    break;

                // ── DOWNLOAD ──
                case ProtocolMessages.REQ_DOWNLOAD:
                    if (parts.Length < 2) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Thiếu tên file."); break; }
                    HandleDownload(parts[1], stream, writer);
                    break;

                // ── UPLOAD (yêu cầu Write) ──
                case ProtocolMessages.REQ_UPLOAD:
                    if (!PermissionManager.CanWrite(clientIP))
                    { writer.WriteLine($"{ProtocolMessages.RES_DENIED}|Bạn chỉ có quyền Read-Only."); break; }
                    if (parts.Length < 3) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Thiếu tham số."); break; }
                    HandleUpload(parts[1], parts[2], stream, writer);
                    break;

                // ── DELETE (yêu cầu Write) ──
                case ProtocolMessages.REQ_DELETE:
                    if (!PermissionManager.CanWrite(clientIP))
                    { writer.WriteLine($"{ProtocolMessages.RES_DENIED}|Bạn chỉ có quyền Read-Only."); break; }
                    if (parts.Length < 2) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Thiếu tên file."); break; }
                    HandleDelete(parts[1], writer);
                    break;

                // ── RENAME (yêu cầu Write) ──
                case ProtocolMessages.REQ_RENAME:
                    if (!PermissionManager.CanWrite(clientIP))
                    { writer.WriteLine($"{ProtocolMessages.RES_DENIED}|Bạn chỉ có quyền Read-Only."); break; }
                    if (parts.Length < 3) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Thiếu tham số."); break; }
                    HandleRename(parts[1], parts[2], writer);
                    break;

                // ── MKDIR (yêu cầu Write) ──
                case ProtocolMessages.REQ_MKDIR:
                    if (!PermissionManager.CanWrite(clientIP))
                    { writer.WriteLine($"{ProtocolMessages.RES_DENIED}|Bạn chỉ có quyền Read-Only."); break; }
                    if (parts.Length < 2) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Thiếu tên thư mục."); break; }
                    HandleMkdir(parts[1], writer);
                    break;

                default:
                    writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Lệnh không hợp lệ: {cmd}");
                    break;
            }
        }

        // ─────────────────── HANDLERS ───────────────────

        private void HandleDownload(string relativePath, NetworkStream stream, StreamWriter writer)
        {
            string? fullPath = FileManager.GetSafePath(SharedFolderPath, relativePath);
            if (fullPath == null) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Đường dẫn không hợp lệ."); return; }

            if (!File.Exists(fullPath))
            {
                writer.WriteLine($"{ProtocolMessages.RES_ERROR}|File không tồn tại.");
                return;
            }

            var fi = new FileInfo(fullPath);
            writer.WriteLine($"{ProtocolMessages.RES_OK}|{fi.Length}");

            try
            {
                // Stream file in 8KB chunks (Fault Tolerance strategy)
                using (var fs = File.OpenRead(fullPath))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    long totalSent = 0;
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                        totalSent += bytesRead;

                        // Log every 1MB with progress %
                        if (totalSent % (1024 * 1024) == 0 || fs.Position == fs.Length)
                        {
                            int percent = (int)((totalSent * 100) / fi.Length);
                            double mbps = (totalSent / (1024.0 * 1024.0)) / sw.Elapsed.TotalSeconds;
                            Log($"📤 {relativePath} | {percent}% | {totalSent / (1024.0 * 1024.0):F1}MB / {fi.Length / (1024.0 * 1024.0):F1}MB | {mbps:F2}MB/s");
                        }
                    }
                    stream.Flush();
                    Log($"✅ Download hoàn tất: {relativePath} ({totalSent} bytes)");
                }
            }
            catch (Exception ex) { Log($"❌ Lỗi download {relativePath}: {ex.Message}"); }
        }

        private void HandleUpload(string relativePath, string sizeStr, NetworkStream stream, StreamWriter writer)
        {
            string? fullPath = FileManager.GetSafePath(SharedFolderPath, relativePath);
            if (fullPath == null) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Đường dẫn không hợp lệ."); return; }

            if (!long.TryParse(sizeStr, out long fileSize))
            {
                writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Kích thước file không hợp lệ.");
                return;
            }
            writer.WriteLine($"{ProtocolMessages.RES_OK}|READY");

            try
            {
                // Tạo directory nếu cần
                string? dir = Path.GetDirectoryName(fullPath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // Ghi file theo dạng Stream
                var result = FileManager.WriteFileStreamAsync(
                    fullPath,
                    () => stream,
                    fileSize,
                    (bytesReceived, total) =>
                    {
                        if (bytesReceived % (1024 * 1024) == 0 || bytesReceived == total)
                        {
                            int percent = (int)((bytesReceived * 100) / total);
                            Log($"📥 {relativePath} | {percent}% | {bytesReceived / (1024.0 * 1024.0):F1}MB / {total / (1024.0 * 1024.0):F1}MB");
                        }
                    }).GetAwaiter().GetResult();

                if (result.WasLocked)
                {
                    writer.WriteLine($"{ProtocolMessages.RES_LOCKED}|File đang được người khác sử dụng, vui lòng thử lại sau.");
                }
                else
                {
                    writer.WriteLine(result.Ok
                        ? $"{ProtocolMessages.RES_OK}|Upload thành công."
                        : $"{ProtocolMessages.RES_ERROR}|Upload không hoàn tất.");
                    Log(result.Ok
                        ? $"✅ Upload hoàn tất: {relativePath}"
                        : $"⚠️ Upload lỗi: {relativePath}");
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"{ProtocolMessages.RES_ERROR}|{ex.Message}");
                Log($"❌ Lỗi upload {relativePath}: {ex.Message}");
            }
        }

        private void HandleDelete(string relativePath, StreamWriter writer)
        {
            string? fullPath = FileManager.GetSafePath(SharedFolderPath, relativePath);
            if (fullPath == null) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Đường dẫn không hợp lệ."); return; }

            bool ok = FileManager.TryDeleteFileSafe(fullPath, out bool wasLocked);
            if (wasLocked)
                writer.WriteLine($"{ProtocolMessages.RES_LOCKED}|Đang được người khác sử dụng, không thể xóa lúc này.");
            else if (ok)
                writer.WriteLine($"{ProtocolMessages.RES_OK}|Đã xóa thành công.");
            else
                writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Không tồn tại hoặc không thể xóa.");
        }

        private void HandleRename(string oldRelPath, string newName, StreamWriter writer)
        {
            string? fullPath = FileManager.GetSafePath(SharedFolderPath, oldRelPath);
            if (fullPath == null) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Đường dẫn không hợp lệ."); return; }

            bool ok = FileManager.TryRenameFileSafe(fullPath, newName, out bool wasLocked);
            if (wasLocked)
                writer.WriteLine($"{ProtocolMessages.RES_LOCKED}|Đang được người khác sử dụng, không thể đổi tên.");
            else if (ok)
                writer.WriteLine($"{ProtocolMessages.RES_OK}|Đổi tên thành công.");
            else
                writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Không thể đổi tên.");
        }

        private void HandleMkdir(string relativePath, StreamWriter writer)
        {
            string? fullPath = FileManager.GetSafePath(SharedFolderPath, relativePath);
            if (fullPath == null) { writer.WriteLine($"{ProtocolMessages.RES_ERROR}|Đường dẫn không hợp lệ."); return; }

            bool ok = FileManager.CreateDirectory(fullPath);
            writer.WriteLine(ok
                ? $"{ProtocolMessages.RES_OK}|Tạo thư mục thành công."
                : $"{ProtocolMessages.RES_ERROR}|Không thể tạo thư mục.");
        }

        private void Log(string msg) => OnLog?.Invoke(msg);
    }
}
