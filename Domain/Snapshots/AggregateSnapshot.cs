#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetCqrsEventSourcing.Domain.Snapshots;

/// <summary>
/// Represents a snapshot of an aggregate's state at a specific version.
/// </summary>
public sealed class AggregateSnapshot
{
    /// <summary>
    /// Schema version of the snapshot. Used to detect shape changes in the aggregate.
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// Current schema version of the snapshot format. Bump this when making breaking changes to the snapshot structure.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Gets or sets the unique identifier of the snapshot.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the aggregate the snapshot belongs to.
    /// </summary>
    public string AggregateId { get; set; }

    /// <summary>
    /// Gets or sets the type of the aggregate the snapshot belongs to.
    /// </summary>
    public string AggregateType { get; set; }

    /// <summary>
    /// Gets or sets the aggregate version captured by the snapshot.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the serialized state of the aggregate.
    /// </summary>
    public string AggregateData { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time at which the snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the integrity checksum of the snapshot, or <see langword="null"/> if none has been computed.
    /// </summary>
    public string? Checksum { get; set; }

    /// <summary>
    /// Gets or sets the size of the snapshot data in bytes after compression.
    /// </summary>
    public int CompressedSize { get; set; }

    /// <summary>
    /// Gets or sets the size of the snapshot data in bytes before compression.
    /// </summary>
    public int UncompressedSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the snapshot data is compressed.
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="AggregateSnapshot"/> class for deserialization.
    /// </summary>
    public AggregateSnapshot()
    {
        Id = Guid.NewGuid().ToString();
        AggregateId = string.Empty;
        AggregateType = string.Empty;
        AggregateData = string.Empty;
        CreatedAt = DateTime.UtcNow;
        SchemaVersion = CurrentSchemaVersion;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateSnapshot"/> class with the specified aggregate state.
    /// </summary>
    /// <param name="aggregateId">The identifier of the aggregate.</param>
    /// <param name="aggregateType">The type of the aggregate.</param>
    /// <param name="version">The aggregate version captured by the snapshot.</param>
    /// <param name="aggregateData">The serialized state of the aggregate.</param>
    public AggregateSnapshot(string aggregateId, string aggregateType, long version, string aggregateData)
        : this()
    {
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        Version = version;
        AggregateData = aggregateData;
        UncompressedSize = aggregateData.Length;
        CompressedSize = aggregateData.Length;
        SchemaVersion = CurrentSchemaVersion;
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
        {
            return false;
        }

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
        {
            return 0;
        }

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

    /// <summary>
    /// Returns a string representation of the snapshot including its aggregate, version, size, creation time and schema version.
    /// </summary>
    /// <returns>A formatted string describing the snapshot.</returns>
    public override string ToString()
        => $"AggregateSnapshot {{ AggregateId={AggregateId}, Version={Version}, Size={GetSizeInKilobytes()}KB, CreatedAt={CreatedAt:yyyy-MM-dd HH:mm:ss}, SchemaVersion={SchemaVersion} }}";
}
