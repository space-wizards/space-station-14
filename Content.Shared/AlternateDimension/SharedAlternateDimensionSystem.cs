using Content.Shared.Maps;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.AlternateDimension;

public abstract class SharedAlternateDimensionSystem : EntitySystem
{
    /// <summary>
    /// Finds and returns an alternate version of a grid of the specified type.
    /// </summary>
    public EntityUid? GetAlternateRealityGrid(EntityUid originalGrid, ProtoId<AlternateDimensionPrototype> type)
    {
        if (!TryComp<RealDimensionGridComponent>(originalGrid, out var realDimension))
            return null;

        if (!realDimension.AlternativeGrids.TryGetValue(type, out var alternativeGrid))
            return null;

        return alternativeGrid;
    }

    /// <summary>
    /// Tries to find an alternate dimension of the grid the entity is on, and get the same coordinates
    /// in the alternate dimension that the entity is in in the real world at the current moment.
    /// </summary>
    public EntityCoordinates? GetAlternateRealityCoordinates(EntityUid entity,
        ProtoId<AlternateDimensionPrototype> type)
    {
        var xform = Transform(entity);
        if (!TryComp<RealDimensionGridComponent>(xform.GridUid, out var realDimension))
            return null;

        if (!realDimension.AlternativeGrids.TryGetValue(type, out var alternativeGrid))
            return null;

        var alternativeMap = Transform(alternativeGrid).MapUid;
        if (alternativeMap is null)
            return null;

        return new EntityCoordinates(alternativeMap.Value, xform.Coordinates.Position);
    }

    /// <summary>
    /// If the entity is in an alternate grid dimension, returns coordinates in the real world relative to the entity's current position in the alternate dimension.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public EntityCoordinates? GetOriginalRealityCoordinates(EntityUid entity)
    {
        var xform = Transform(entity);

        if (!TryComp<AlternateDimensionGridComponent>(xform.GridUid, out var alternateComp))
            return null;

        if (alternateComp.RealDimensionGrid is null)
            return null;

        return new EntityCoordinates(alternateComp.RealDimensionGrid.Value, xform.Coordinates.Position);
    }
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
