using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Maps;

public sealed partial class GameMapPrototype
{
    [DataField]
    public SpaceArenaMapData? SpaceArena;

    [DataField]
    public bool SpaceArenaHubProtection;
}

[DataDefinition]
public sealed partial class SpaceArenaMapData
{
    [DataField]
    public string LobbyFormat = string.Empty;

    [DataField]
    public EntProtoId? PreviewWeapon;

    [DataField(required: true)]
    public List<EntProtoId> Modes = new();

    [DataField]
    public List<string> SpawnGroups = new()
    {
        Content.Shared.SpaceArena.SpaceArenaSpawnGroups.Player,
    };

    [DataField]
    public ProtoId<StartingGearPrototype>? Loadout;

    [DataField]
    public List<ProtoId<StartingGearPrototype>> Loadouts = new();

    [DataField]
    public TimeSpan? CountdownDuration;
}
