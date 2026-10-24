#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Nodes;

namespace DotNetCqrsEventSourcing.Application.Upcasting;

/// <summary>
/// Upcasts <c>AccountCreated</c> payloads from schema version 1 to version 2.
/// <para>
/// Version 1 wrote the account holder under the <c>AccountHolder</c> key. Version 2 renames the wire
/// field to <c>OwnerName</c>; the CLR property keeps its name. Payloads that already use the
/// version 2 key are returned unchanged.
/// </para>
/// </summary>
public sealed class AccountCreatedV1ToV2Upcaster : IEventUpcaster
{
    private const string LegacyHolderKey = "AccountHolder";
    private const string OwnerNameKey = "OwnerName";

    /// <summary>
    /// Gets the event name handled by this upcaster, <c>AccountCreated</c>.
    /// </summary>
    public string EventType => "AccountCreated";

    /// <summary>
    /// Gets the schema version read by this upcaster, <c>1</c>.
    /// </summary>
    public int FromVersion => 1;

    /// <summary>
    /// Renames the <c>AccountHolder</c> key to <c>OwnerName</c>, preserving its value.
    /// </summary>
    /// <param name="payload">The version 1 payload.</param>
    /// <returns>The payload with the holder stored under <c>OwnerName</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="payload"/> is <see langword="null"/>.</exception>
    public JsonObject Upcast(JsonObject payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!payload.Remove(LegacyHolderKey, out var holder))
        {
            return payload;
        }

        payload[OwnerNameKey] = holder;
        return payload;
    }
}
