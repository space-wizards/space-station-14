using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.PostGeneration;

/// <summary>
/// Places the specified entities at junction areas.
/// </summary>
public sealed partial class JunctionDunGen : IDunGenLayer
{
    /// <summary>
    /// Width to check for junctions.
    /// </summary>
    [DataField]
    public int Width = 3;

    [DataField(required: true)]
    public ProtoId<ContentTileDefinition> Tile;

    [DataField(required: true)]
    public EntityTableSelector Contents = default!;
}
