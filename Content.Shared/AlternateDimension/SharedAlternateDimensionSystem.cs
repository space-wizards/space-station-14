using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AlternateDimension;

public abstract class SharedAlternateDimensionSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed record AlternateDimensionParams
{
    public int Seed;
    public ProtoId<AlternateDimensionPrototype> Dimension = new();
}

[Prototype("alternateDimension")]
public sealed partial class AlternateDimensionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    /// The floor of the alternate reality will be made up of this tile
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> DefaultTile = string.Empty;

    /// <summary>
    /// These components will be added to the alternate reality map
    /// </summary>
    [DataField]
    public ComponentRegistry? MapComponents;

    /// <summary>
    /// These components will be added to the alternate reality grid
    /// </summary>
    [DataField]
    public ComponentRegistry? GridComponents;

    /// <summary>
    /// All entities with specified tags on station map will create other specified entities in the alternate reality.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<TagPrototype>, EntProtoId> Replacements = new();
}
