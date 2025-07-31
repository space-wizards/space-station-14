using Robust.Shared.Player;

namespace Content.Server._NullLink.PlayerData;

public sealed class PlayerData
{
    public required ICommonSession Session { get; init; }
    public HashSet<ulong> Roles { get; set; } = [];
}
