using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace P2PFileSharingApp.Core
{
    /// <summary>
    /// Quản lý tất cả các thao tác file I/O với cơ chế khóa đa luồng (ReaderWriterLockSlim).
    /// - Nhiều client có thể đọc (Download) cùng lúc.
    /// - Chỉ 1 client được ghi (Upload/Delete/Rename) tại một thời điểm.
    /// - Nếu file đang được Đọc, lệnh Xóa/Sửa sẽ bị chặn và trả về RES_LOCKED.
    /// </summary>
    public static class FileManager
    {
        // Mỗi file có một khóa riêng để tránh chặn toàn bộ hệ thống
        private static readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _fileLocks
            = new ConcurrentDictionary<string, ReaderWriterLockSlim>();

        // Đếm số lượng ReadLock đang active để phát hiện xung đột
        private static readonly ConcurrentDictionary<string, int> _activeReaders
            = new ConcurrentDictionary<string, int>();

        private static string NormalizeKey(string filePath)
            => filePath.ToLowerInvariant();

        private static ReaderWriterLockSlim GetLock(string filePath)
            => _fileLocks.GetOrAdd(NormalizeKey(filePath), _ => new ReaderWriterLockSlim());

        /// <summary>Kiểm tra file đang có ai đọc không (để phát hiện xung đột trước khi xóa/sửa).</summary>
        public static bool IsFileBeingRead(string filePath)
            => _activeReaders.GetOrAdd(NormalizeKey(filePath), 0) > 0;

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

        /// <summary>Đọc toàn bộ byte của file với ReadLock (nhiều người có thể đọc cùng lúc).</summary>
        public static byte[]? ReadFileSafe(string filePath)
        {
            var key = NormalizeKey(filePath);
            var lk = GetLock(filePath);

            lk.EnterReadLock();
            _activeReaders.AddOrUpdate(key, 1, (_, v) => v + 1);
            try
            {
                return File.Exists(filePath) ? File.ReadAllBytes(filePath) : null;
            }
            finally
            {
                _activeReaders.AddOrUpdate(key, 0, (_, v) => Math.Max(0, v - 1));
                lk.ExitReadLock();
            }
        }

        // ─────────────────── WRITE (UPLOAD) ───────────────────

        /// <summary>Ghi byte vào file với WriteLock. Sẽ bị chặn nếu có ReadLock đang giữ.</summary>
        public static bool WriteFileSafe(string filePath, byte[] data)
        {
            var lk = GetLock(filePath);
            lk.EnterWriteLock();
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
        /// Xóa file HOẶC thư mục với WriteLock. Nếu đang bị ReadLock (ai đó đang tải),
        /// trả về false và caller nên trả về RES_LOCKED cho client.
        /// </summary>
        public static bool TryDeleteFileSafe(string path, out bool wasLocked)
        {
            wasLocked = false;
            if (IsFileBeingRead(path))
            {
                wasLocked = true;
                return false;
            }

            var lk = GetLock(path);
            lk.EnterWriteLock();
            try
            {
                // Double-check sau khi lấy WriteLock
                if (IsFileBeingRead(path)) { wasLocked = true; return false; }

                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                    return true;
                }
                return false;
            }
            catch { return false; }
            finally { lk.ExitWriteLock(); }
        }

        // ─────────────────── RENAME ───────────────────

        /// <summary>Đổi tên file HOẶC thư mục với WriteLock.</summary>
        public static bool TryRenameFileSafe(string path, string newName, out bool wasLocked)
        {
            wasLocked = false;
            if (IsFileBeingRead(path)) { wasLocked = true; return false; }

            var lk = GetLock(path);
            lk.EnterWriteLock();
            try
            {
                if (IsFileBeingRead(path)) { wasLocked = true; return false; }

                if (File.Exists(path))
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(path)!, newName);
                    File.Move(path, newPath);
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    string newPath = Path.Combine(Directory.GetParent(path)!.FullName, newName);
                    Directory.Move(path, newPath);
                    return true;
                }
                return false;
            }
            catch { return false; }
            finally { lk.ExitWriteLock(); }
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
    }
}
