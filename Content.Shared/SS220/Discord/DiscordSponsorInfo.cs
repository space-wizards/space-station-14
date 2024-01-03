// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Discord;

[Serializable, NetSerializable]
public sealed class DiscordSponsorInfo
{
    public SponsorTier[] Tiers { get; set; } = Array.Empty<SponsorTier>();
}
