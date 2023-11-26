// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DiscordLink;

[Serializable, NetSerializable]
public sealed class DiscordLinkEuiState : EuiStateBase
{
    public string? LinkKey { get; }

    public DiscordLinkEuiState(string? linkKey)
    {
        LinkKey = linkKey;
    }
}

