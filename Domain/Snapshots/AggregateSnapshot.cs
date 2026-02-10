// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Snapshots;

/// <summary>
/// Represents a snapshot of an aggregate's state at a specific version.
/// </summary>
public class AggregateSnapshot
{
    public string Id { get; set; }
    public string AggregateId { get; set; }
    public string AggregateType { get; set; }
    public long Version { get; set; }
    public string AggregateData { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Checksum { get; set; }
    public int CompressedSize { get; set; }
    public int UncompressedSize { get; set; }
    public bool IsCompressed { get; set; }

    public AggregateSnapshot()
    {
        Id = Guid.NewGuid().ToString();
        AggregateId = string.Empty;
        AggregateType = string.Empty;
        AggregateData = string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    public AggregateSnapshot(string aggregateId, string aggregateType, long version, string aggregateData)
        : this()
    {
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        Version = version;
        AggregateData = aggregateData;
        UncompressedSize = aggregateData.Length;
        CompressedSize = aggregateData.Length;
    }

    /// <summary>
    /// Calculate checksum for snapshot integrity verification.
    /// </summary>
    public void ComputeChecksum()
    {
        var checksumData = $"{AggregateId}:{Version}:{AggregateType}:{AggregateData}";
        Checksum = ComputeSha256Hash(checksumData);
    }

    /// <summary>
    /// Verify snapshot integrity using checksum.
    /// </summary>
    public bool VerifyChecksum()
    {
        if (string.IsNullOrEmpty(Checksum))
            return false;

        var expectedChecksum = Checksum;
        ComputeChecksum();
        return Checksum == expectedChecksum;
    }

    /// <summary>
    /// Mark snapshot as compressed.
    /// </summary>
    public void MarkCompressed(int compressedSize)
    {
        IsCompressed = true;
        CompressedSize = compressedSize;
    }

    /// <summary>
    /// Get compression ratio as percentage.
    /// </summary>
    public double GetCompressionRatio()
    {
        if (UncompressedSize == 0)
            return 0;

        return 100.0 * (1 - (double)CompressedSize / UncompressedSize);
    }

    /// <summary>
    /// Get approximate size in kilobytes.
    /// </summary>
    public int GetSizeInKilobytes()
    {
        var sizeBytes = IsCompressed ? CompressedSize : UncompressedSize;
        return sizeBytes / 1024;
    }

    private static string ComputeSha256Hash(string input)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public override string ToString()
        => $"AggregateSnapshot {{ AggregateId={AggregateId}, Version={Version}, Size={GetSizeInKilobytes()}KB, CreatedAt={CreatedAt:yyyy-MM-dd HH:mm:ss} }}";
}
