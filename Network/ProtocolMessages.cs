namespace P2PFileSharingApp.Network
{
    /// <summary>
    /// Định nghĩa toàn bộ giao thức giao tiếp giữa Server và Client.
    /// Dạng lệnh Text, phân cách bởi ký tự '|'.
    /// Ví dụ: "REQ_LIST|/docs" hoặc "REQ_DELETE|test.txt"
    /// </summary>
    public static class ProtocolMessages
    {
        // Ký tự phân cách
        public const char SEPARATOR = '|';

        // ── Lệnh từ CLIENT gửi lên SERVER ──
        public const string REQ_LIST     = "REQ_LIST";      // Yêu cầu danh sách file
        public const string REQ_DOWNLOAD = "REQ_DOWNLOAD";  // Yêu cầu tải file
        public const string REQ_UPLOAD   = "REQ_UPLOAD";    // Yêu cầu gửi file lên
        public const string REQ_DELETE   = "REQ_DELETE";    // Yêu cầu xóa file
        public const string REQ_RENAME   = "REQ_RENAME";    // Yêu cầu đổi tên file
        public const string REQ_MKDIR    = "REQ_MKDIR";     // Yêu cầu tạo thư mục

        // ── Phản hồi từ SERVER trả về CLIENT ──
        public const string RES_OK       = "RES_OK";        // Thành công
        public const string RES_LIST     = "RES_LIST";      // Phản hồi danh sách file
        public const string RES_ERROR    = "RES_ERROR";     // Lỗi chung
        public const string RES_DENIED   = "RES_DENIED";    // Từ chối: Không đủ quyền
        public const string RES_LOCKED   = "RES_LOCKED";    // Từ chối: File đang bị khóa bởi người khác
    }
}
