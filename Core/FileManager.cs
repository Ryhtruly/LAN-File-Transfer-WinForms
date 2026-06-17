using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace P2PFileSharingApp.Core
{
    /// <summary>
    /// Quản lý tất cả các thao tác file I/O với cơ chế khóa đa luồng (ReaderWriterLockSlim).
    /// - Nhiều client có thể đọc (Download) cùng lúc.
    /// - Chỉ 1 client được ghi (Upload/Delete/Rename) tại một thời điểm.
    /// - Nếu file đang được Đọc/Ghi, lệnh ghi khác sẽ bị từ chối ngay (RES_LOCKED).
    ///
    /// Tối ưu hóa:
    /// - Sử dụng TryEnterWriteLock(0) thay vì kiểm tra thủ công _activeReaders (Fix B).
    /// - Dọn dẹp khóa rác khi file bị xóa để tránh rò rỉ bộ nhớ (Fix C).
    /// - Khóa cả đường dẫn nguồn và đích khi đổi tên để tránh xung đột (Fix D).
    /// </summary>
    public static class FileManager
    {
        // Mỗi file có một khóa riêng để tránh chặn toàn bộ hệ thống
        private static readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _fileLocks
            = new ConcurrentDictionary<string, ReaderWriterLockSlim>();

        // [Fix B] Đã xóa _activeReaders vì ReaderWriterLockSlim đã tự đếm qua CurrentReadCount.

        private static string NormalizeKey(string filePath)
            => filePath.ToLowerInvariant();

        private static ReaderWriterLockSlim GetLock(string filePath)
            => _fileLocks.GetOrAdd(NormalizeKey(filePath), _ => new ReaderWriterLockSlim());

        /// <summary>Ghép nối và xác thực đường dẫn, ngăn chặn Directory Traversal (../).</summary>
        public static string? GetSafePath(string sharedRoot, string relativePath)
        {
            if (string.IsNullOrEmpty(sharedRoot)) return null;
            
            // Xóa ký tự gạch chéo đầu tiên nếu có để Path.Combine không tưởng là đường dẫn tuyệt đối
            relativePath = relativePath.TrimStart('/', '\\');
            
            string fullPath = Path.GetFullPath(Path.Combine(sharedRoot, relativePath));
            string rootFullPath = Path.GetFullPath(sharedRoot);

            // Kiểm tra xem fullPath có nằm trong rootFullPath không (chống ../)
            if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
                return null;

            return fullPath;
        }

        // ─────────────────── DIRECTORY LISTING ───────────────────

        /// <summary>Quét cấu trúc thư mục, trả về chuỗi dạng [DIR]name|[FILE]name*size|...</summary>
        public static string GetDirectoryStructure(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return string.Empty;

            var sb = new StringBuilder();
            try
            {
                foreach (string d in Directory.GetDirectories(rootPath))
                    sb.Append($"[DIR]{Path.GetFileName(d)}|");

                foreach (string f in Directory.GetFiles(rootPath))
                {
                    var fi = new FileInfo(f);
                    sb.Append($"[FILE]{fi.Name}*{fi.Length}|");
                }

                if (sb.Length > 0) sb.Length--;
            }
            catch { /* Bỏ qua thư mục bị từ chối quyền hệ thống */ }

            return sb.ToString();
        }

        // ─────────────────── READ (DOWNLOAD) ───────────────────

        /// <summary>
        /// Đọc toàn bộ byte của file với ReadLock (nhiều người có thể đọc cùng lúc).
        /// [Fix B] Không cần đếm _activeReaders thủ công nữa – ReaderWriterLockSlim
        /// tự động theo dõi số ReadLock đang active qua thuộc tính CurrentReadCount.
        /// </summary>
        public static byte[]? ReadFileSafe(string filePath)
        {
            var lk = GetLock(filePath);
            lk.EnterReadLock();
            try
            {
                // thêm dòng này nếu muốn test, do truyền qua mạng LAN rất nhanh
                // System.Threading.Thread.Sleep(8000);
                return File.Exists(filePath) ? File.ReadAllBytes(filePath) : null;
            }
            finally
            {
                lk.ExitReadLock();
            }
        }

        // ─────────────────── WRITE (UPLOAD) ───────────────────

        /// <summary>
        /// Ghi byte vào file với WriteLock.
        /// [Fix B] Sử dụng TryEnterWriteLock(0): nếu file đang bị khóa (đọc hoặc ghi),
        /// trả về false + wasLocked = true ngay lập tức thay vì đứng chờ vô hạn.
        /// </summary>
        public static bool WriteFileSafe(string filePath, byte[] data, out bool wasLocked)
        {
            wasLocked = false;
            var lk = GetLock(filePath);

            // TryEnterWriteLock(0): thử lấy WriteLock ngay, nếu đang có ai đọc/ghi → trả false
            if (!lk.TryEnterWriteLock(0))
            {
                wasLocked = true;
                return false;
            }

            try
            {
                File.WriteAllBytes(filePath, data);
                return true;
            }
            catch { return false; }
            finally { lk.ExitWriteLock(); }
        }

        // ─────────────────── DELETE ───────────────────

        /// <summary>
        /// Xóa file HOẶC thư mục với WriteLock.
        /// [Fix B] Sử dụng TryEnterWriteLock(0) thay vì kiểm tra thủ công IsFileBeingRead().
        /// [Fix C] Sau khi xóa thành công, dọn dẹp khóa rác khỏi _fileLocks.
        /// </summary>
        public static bool TryDeleteFileSafe(string path, out bool wasLocked)
        {
            wasLocked = false;
            var key = NormalizeKey(path);
            var lk = GetLock(path);

            // [Fix B] TryEnterWriteLock(0): thử lấy WriteLock ngay lập tức.
            // Nếu đang có ReadLock (ai đó đang tải) hoặc WriteLock (ai đó đang ghi) → trả false.
            // Không cần pre-check hay double-check thủ công nữa!
            if (!lk.TryEnterWriteLock(0))
            {
                wasLocked = true;
                return false;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    // [Fix C] Dọn dẹp khóa rác: file đã xóa → khóa không còn cần thiết
                    _fileLocks.TryRemove(key, out _);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                    // [Fix C] Dọn dẹp khóa rác cho thư mục
                    _fileLocks.TryRemove(key, out _);
                    return true;
                }
                return false;
            }
            catch { return false; }
            finally { lk.ExitWriteLock(); }
        }

        // ─────────────────── RENAME ───────────────────

        /// <summary>
        /// Đổi tên file HOẶC thư mục với WriteLock cho CẢ đường dẫn nguồn VÀ đích.
        /// [Fix D] Khóa cả 2 đường dẫn theo thứ tự alphabet để tránh Deadlock.
        /// [Fix C] Sau khi đổi tên, dọn dẹp khóa cũ (nguồn) khỏi _fileLocks.
        /// </summary>
        public static bool TryRenameFileSafe(string path, string newName, out bool wasLocked)
        {
            wasLocked = false;

            // Tính đường dẫn đích trước khi lấy khóa
            string? parentDir = Path.GetDirectoryName(path);
            if (parentDir == null) return false;
            string newPath = Path.Combine(parentDir, newName);

            var sourceKey = NormalizeKey(path);
            var destKey = NormalizeKey(newPath);

            // Nếu tên không thay đổi → không cần làm gì
            if (sourceKey == destKey) return true;

            // [Fix D] Luôn khóa theo thứ tự alphabet để tránh Deadlock khi 2 thread
            // đổi tên chéo nhau (A→B và B→A cùng lúc).
            bool sourceFirst = string.Compare(sourceKey, destKey, StringComparison.Ordinal) < 0;
            var lk1 = sourceFirst ? GetLock(path) : GetLock(newPath);     // Khóa thứ 1 (alphabet nhỏ hơn)
            var lk2 = sourceFirst ? GetLock(newPath) : GetLock(path);     // Khóa thứ 2 (alphabet lớn hơn)

            // Lấy WriteLock cho khóa thứ nhất
            if (!lk1.TryEnterWriteLock(0))
            {
                wasLocked = true;
                return false;
            }

            try
            {
                // Lấy WriteLock cho khóa thứ hai
                if (!lk2.TryEnterWriteLock(0))
                {
                    wasLocked = true;
                    return false;
                }

                try
                {
                    if (File.Exists(path))
                    {
                        File.Move(path, newPath);
                        // [Fix C] Dọn dẹp khóa cũ (khóa mới sẽ được tạo lazy khi cần)
                        _fileLocks.TryRemove(sourceKey, out _);
                        return true;
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Move(path, newPath);
                        _fileLocks.TryRemove(sourceKey, out _);
                        return true;
                    }
                    return false;
                }
                catch { return false; }
                finally { lk2.ExitWriteLock(); }
            }
            finally { lk1.ExitWriteLock(); }
        }

        // ─────────────────── CREATE FOLDER ───────────────────

        /// <summary>Tạo thư mục mới.</summary>
        public static bool CreateDirectory(string dirPath)
        {
            try
            {
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                return true;
            }
            catch { return false; }
        }

        // ─────────────────── STREAM-BASED I/O (BUFFER 8KB) ───────────────────

        /// <summary>
        /// Đọc file theo streaming (8KB chunks) thay vì ReadAllBytes.
        /// Áp dụng: Fault Tolerance + Stream-oriented communication
        /// RAM = 8KB dù file bao lớn.
        /// </summary>
        public static async Task<bool> ReadFileStreamAsync(
            string filePath,
            Func<Stream> getNetworkStream,  // Lấy NetworkStream để ghi dữ liệu
            Action<long, long>? onProgress = null)  // (bytesSent, totalBytes)
        {
            var lk = GetLock(filePath);
            lk.EnterReadLock();
            try
            {
                if (!File.Exists(filePath)) return false;

                var fi = new FileInfo(filePath);
                long totalSize = fi.Length;
                long bytesSent = 0;
                const int BUFFER_SIZE = 8192;
                byte[] buffer = new byte[BUFFER_SIZE];

                using (var fs = File.OpenRead(filePath))
                using (var networkStream = getNetworkStream())
                {
                    int bytesRead;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        await networkStream.WriteAsync(buffer, 0, bytesRead);
                        bytesSent += bytesRead;
                        onProgress?.Invoke(bytesSent, totalSize);
                    }
                }
                return true;
            }
            catch { return false; }
            finally
            {
                lk.ExitReadLock();
            }
        }

        /// <summary>
        /// Ghi file theo streaming (8KB chunks) thay vì WriteAllBytes.
        /// Áp dụng: Fault Tolerance + Stream-oriented communication
        /// RAM = 8KB dù file bao lớn.
        /// </summary>
        public static async Task<bool> WriteFileStreamAsync(
            string filePath,
            Func<Stream> getNetworkStream,  // Lấy NetworkStream để đọc dữ liệu
            long fileSize,
            out bool wasLocked,
            Action<long, long>? onProgress = null)  // (bytesReceived, totalBytes)
        {
            wasLocked = false;
            var lk = GetLock(filePath);

            if (!lk.TryEnterWriteLock(0))
            {
                wasLocked = true;
                return false;
            }

            try
            {
                // Tạo directory nếu chưa có
                string? dir = Path.GetDirectoryName(filePath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                long bytesReceived = 0;
                const int BUFFER_SIZE = 8192;
                byte[] buffer = new byte[BUFFER_SIZE];

                using (var fs = File.Create(filePath))
                using (var networkStream = getNetworkStream())
                {
                    while (bytesReceived < fileSize)
                    {
                        int toRead = (int)Math.Min(buffer.Length, fileSize - bytesReceived);
                        int bytesRead = await networkStream.ReadAsync(buffer, 0, toRead);

                        if (bytesRead == 0) break;

                        fs.Write(buffer, 0, bytesRead);
                        bytesReceived += bytesRead;
                        onProgress?.Invoke(bytesReceived, fileSize);
                    }
                }

                return bytesReceived == fileSize;
            }
            catch { return false; }
            finally { lk.ExitWriteLock(); }
        }
    }
}
