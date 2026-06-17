using System.Diagnostics;
using P2PFileSharingHaiUI.Models;

namespace P2PFileSharingHaiUI.Services;

public sealed class FileTransferSimulator
{
    private const int BufferSize = 8192;

    public async Task UploadAsync(
        string filePath,
        IProgress<TransferProgress> progress,
        CancellationToken cancellationToken)
    {
        FileInfo fileInfo = new(filePath);
        long totalBytes = fileInfo.Length;
        long bytesDone = 0;
        byte[] buffer = new byte[BufferSize];
        Stopwatch stopwatch = Stopwatch.StartNew();

        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            useAsync: true);

        while (true)
        {
            int read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            bytesDone += read;

            double seconds = Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
            double mbps = bytesDone / 1024d / 1024d / seconds;

            progress.Report(new TransferProgress(bytesDone, totalBytes, mbps, fileInfo.Name));

            // Temporary network delay so UI progress is visible before wiring the real TCP buffer layer.
            await Task.Delay(8, cancellationToken);
        }
    }

    public async Task DownloadMockAsync(
        string fileName,
        long totalBytes,
        string destinationPath,
        IProgress<TransferProgress> progress,
        CancellationToken cancellationToken)
    {
        long bytesDone = 0;
        byte[] buffer = new byte[BufferSize];
        Stopwatch stopwatch = Stopwatch.StartNew();
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        await using FileStream output = new(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            useAsync: true);

        while (bytesDone < totalBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int writeSize = (int)Math.Min(buffer.Length, totalBytes - bytesDone);
            await output.WriteAsync(buffer.AsMemory(0, writeSize), cancellationToken);
            bytesDone += writeSize;

            double seconds = Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
            double mbps = bytesDone / 1024d / 1024d / seconds;

            progress.Report(new TransferProgress(bytesDone, totalBytes, mbps, fileName));
            await Task.Delay(8, cancellationToken);
        }
    }
}
