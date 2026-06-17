using System.Collections.Concurrent;

namespace P2PFileSharingApp.Core
{
    /// <summary>
    /// Ba cấp độ quyền truy cập tài nguyên:
    /// - Denied:    Chặn hoàn toàn mọi thao tác.
    /// - ReadOnly:  Chỉ được xem danh sách + tải file (Download).
    /// - ReadWrite: Toàn quyền: Download, Upload, Delete, Rename, New Folder.
    /// </summary>
    public enum PermissionLevel
    {
        Denied,
        ReadOnly,
        UploadOnly,
        ReadWrite,
        FullAccess
    }

    public static class PermissionManager
    {
        // IP mới kết nối mặc định vào ReadOnly (an toàn nhất)
        private static readonly ConcurrentDictionary<string, PermissionLevel> _permissions
            = new ConcurrentDictionary<string, PermissionLevel>();

        /// <summary>Lấy cấp độ quyền của một IP. Mặc định ReadOnly nếu chưa được cấu hình.</summary>
        public static PermissionLevel GetPermission(string ip)
        {
            return _permissions.GetOrAdd(ip, PermissionLevel.ReadOnly);
        }

        /// <summary>Thiết lập cấp độ quyền cho một IP.</summary>
        public static void SetPermission(string ip, PermissionLevel level)
        {
            _permissions[ip] = level;
        }

        /// <summary>Kiểm tra IP có quyền Upload không (UploadOnly, ReadWrite, FullAccess).</summary>
        public static bool CanWrite(string ip)
        {
            var level = GetPermission(ip);
            return level == PermissionLevel.UploadOnly || level == PermissionLevel.ReadWrite || level == PermissionLevel.FullAccess;
        }

        /// <summary>Kiểm tra IP có quyền xóa, sửa, tạo thư mục không (FullAccess).</summary>
        public static bool CanDeleteOrEdit(string ip)
        {
            return GetPermission(ip) == PermissionLevel.FullAccess;
        }

        /// <summary>Kiểm tra IP có quyền tải về không (ReadOnly, ReadWrite, FullAccess). UploadOnly KHÔNG ĐƯỢC tải về.</summary>
        public static bool CanDownload(string ip)
        {
            var level = GetPermission(ip);
            return level == PermissionLevel.ReadOnly || level == PermissionLevel.ReadWrite || level == PermissionLevel.FullAccess;
        }

        /// <summary>Kiểm tra IP có quyền xem danh sách file không. Bị chặn (Denied) thì không được xem.</summary>
        public static bool CanRead(string ip)
            => GetPermission(ip) != PermissionLevel.Denied;

        /// <summary>Trả về danh sách tất cả các IP đang có mục quyền.</summary>
        public static IEnumerable<KeyValuePair<string, PermissionLevel>> GetAll()
            => _permissions;

        /// <summary>Xóa cấu hình quyền của một IP (dùng khi IP đó ngắt kết nối).</summary>
        public static void Remove(string ip)
            => _permissions.TryRemove(ip, out _);
    }
}
