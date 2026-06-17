namespace P2PFileSharingHaiUI.Models;

public sealed record TransferProgress(
    long BytesDone,
    long TotalBytes,
    double MegabytesPerSecond,
    string FileName);
