using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.SpaceArena.Components;

[RegisterComponent, Access(typeof(SpaceArenaDeathMatchSystem))]
public sealed partial class SpaceArenaDeathMatchComponent : Component
{
    [DataField]
    public ProtoId<StartingGearPrototype> Gear = "DeathMatchGear";

    public readonly Dictionary<NetUserId, ProtoId<StartingGearPrototype>> PlayerLoadouts = [];

    public readonly Dictionary<string, List<ProtoId<StartingGearPrototype>>> GroupLoadouts = [];

    public readonly Dictionary<string, int> NextGroupLoadout = [];

    public string? WinningGroup;

    public bool ResultAnnounced;
}
