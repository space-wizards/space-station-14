using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.DungeonGenerators;

/// <summary>
/// Fills unreserved tiles with the specified entity prototype.
/// </summary>
/// <remarks>
/// DungeonData keys are:
/// - Fill
/// </remarks>
public sealed partial class FillGridDunGen : IDunGenLayer
{
    /// <summary>
    /// Tiles the fill can occur on.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ContentTileDefinition>>? AllowedTiles;
}
