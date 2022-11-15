using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.Sponsors;

[Serializable, NetSerializable]
public sealed class SponsorInfo
{
    [JsonPropertyName("tier")]
    public int? Tier { get; set; }

    [JsonPropertyName("oocColor")]
    public string? OOCColor { get; set; }

    [JsonPropertyName("priorityJoin")]
    public bool HavePriorityJoin { get; set; }

    [JsonPropertyName("allowedNeko")]
    public bool AllowedNeko { get; set; }
}