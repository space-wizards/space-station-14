using Content.Shared.Maps;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Places rooms in pre-selected pack layouts. Chooses rooms from the specified whitelist.
/// </summary>
/// <remarks>
public sealed partial class PrefabDunGen : IDunGenLayer
{
    /// <summary>
    /// Room pack presets we can use for this prefab.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<DungeonPresetPrototype>> Presets = new();

    [DataField]
    public EntityWhitelist? RoomWhitelist;

    [DataField]
    public ProtoId<ContentTileDefinition>? FallbackTile;
}
