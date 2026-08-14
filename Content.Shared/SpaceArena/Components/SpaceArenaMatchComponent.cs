using Content.Shared.Maps;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.SpaceArena.Components;

[RegisterComponent]
public sealed partial class SpaceArenaMatchComponent : Component
{
    [DataField]
    public LocId Name = "space-arena-mode-unknown";

    [DataField]
    public int MinPlayers = 2;

    [DataField]
    public int MaxPlayers = 16;

    [DataField]
    public TimeSpan PreparationDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan CountdownDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan? TimeLimit = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan EndingDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ResultsDuration = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan? RespawnDelay;

    [DataField]
    public TimeSpan? DisconnectGracePeriod;

    [DataField]
    public bool AllowVoluntaryLeave;

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    public ProtoId<GameMapPrototype>? Arena;

    public SpaceArenaMatchState State = SpaceArenaMatchState.Waiting;

    public int PlayerCount;

    public TimeSpan? StateEndsAt;
}
