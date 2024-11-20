using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

/// <summary>
///     Event raised broadcast before a player is spawned by the GameTicker.
///     You can use this event to spawn a player off-station on late-join but also at round start.
///     When this event is handled, the GameTicker will not perform its own player-spawning logic.
/// </summary>
[PublicAPI]
public sealed class PlayerBeforeSpawnEvent : HandledEntityEventArgs
{
    public ICommonSession Player { get; }
    public HumanoidCharacterProfile Profile { get; }
    public string? JobId { get; }
    public bool LateJoin { get; }
    public EntityUid Station { get; }

    public PlayerBeforeSpawnEvent(ICommonSession player,
        HumanoidCharacterProfile profile,
        string? jobId,
        bool lateJoin,
        EntityUid station)
    {
        Player = player;
        Profile = profile;
        JobId = jobId;
        LateJoin = lateJoin;
        Station = station;
    }
}
