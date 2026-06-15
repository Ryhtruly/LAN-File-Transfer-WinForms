using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace P2PFileSharingApp.Network
{
    /// <summary>
    /// Theo lý thuyết Distributed Systems:
    /// - Fault Tolerance: 8KB buffer ngăn chặn Crash Failure (Out of Memory)
    /// - Stream-Oriented Communication: Chia nhỏ dữ liệu thay vì gửi 1 lần
    /// - Flow Control: Vòng lặp tự điều chỉnh tốc độ theo mạng
    /// </summary>
    public class StreamTransferProgress
    {
        public long TotalBytes { get; set; }
        public long BytesTransferred { get; set; }
        public double PercentComplete => TotalBytes > 0
            ? (BytesTransferred * 100.0) / TotalBytes
            : 0;
        public double MBPerSecond { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public Action<StreamTransferProgress>? OnProgressUpdate { get; set; }

        public void UpdateProgress(long bytesTransferred, double mbps, TimeSpan elapsed)
        {
            BytesTransferred = bytesTransferred;
            MBPerSecond = mbps;
            ElapsedTime = elapsed;
            OnProgressUpdate?.Invoke(this);
        }

        public override string ToString()
            => $"{PercentComplete:F1}% | {BytesTransferred / (1024.0 * 1024.0):F2}MB / {TotalBytes / (1024.0 * 1024.0):F2}MB | {MBPerSecond:F2}MB/s";
    }

    public static class StreamTransferHelper
    {
        const int BUFFER_SIZE = 8192;  // 8KB chunks - Fault Tolerance strategy

        /// <summary>
        /// Server-side: Send file to client in 8KB chunks.
        /// Áp dụng: Stream-oriented communication + Fault Tolerance
        /// </summary>
        public static async Task SendFileAsync(
            FileStream fileStream,
            NetworkStream networkStream,
            StreamTransferProgress? progress = null)
        {
            if (fileStream == null || networkStream == null)
                throw new ArgumentNullException("Stream không thể null");

            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;
            long totalSent = 0;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    await networkStream.WriteAsync(buffer, 0, bytesRead);
                    totalSent += bytesRead;

                    // Update progress mỗi MB
                    if (totalSent % (1024 * 1024) == 0 || fileStream.Position == fileStream.Length)
                    {
                        double mbps = CalculateMBPerSecond(totalSent, stopwatch.Elapsed);
                        progress?.UpdateProgress(totalSent, mbps, stopwatch.Elapsed);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Client-side: Receive file from server in 8KB chunks.
        /// Áp dụng: Stream-oriented communication + Fault Tolerance
        /// </summary>
        public static async Task ReceiveFileAsync(
            NetworkStream networkStream,
            FileStream fileStream,
            long fileSize,
            StreamTransferProgress? progress = null)
        {
            if (networkStream == null || fileStream == null)
                throw new ArgumentNullException("Stream không thể null");

            byte[] buffer = new byte[BUFFER_SIZE];
            int bytesRead;
            long totalReceived = 0;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                while (totalReceived < fileSize)
                {
                    int toRead = (int)Math.Min(buffer.Length, fileSize - totalReceived);
                    bytesRead = await networkStream.ReadAsync(buffer, 0, toRead);

                    if (bytesRead == 0) break;

                    fileStream.Write(buffer, 0, bytesRead);
                    totalReceived += bytesRead;

                    // Update progress mỗi MB
                    if (totalReceived % (1024 * 1024) == 0 || totalReceived == fileSize)
                    {
                        double mbps = CalculateMBPerSecond(totalReceived, stopwatch.Elapsed);
                        progress?.UpdateProgress(totalReceived, mbps, stopwatch.Elapsed);
                    }
                }
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Tính tốc độ truyền (MB/s).
        /// Áp dụng: Transport Layer Flow Control
        /// </summary>
        private static double CalculateMBPerSecond(long bytes, TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds == 0) return 0;
            return (bytes / (1024.0 * 1024.0)) / elapsed.TotalSeconds;
        }
    }
}
