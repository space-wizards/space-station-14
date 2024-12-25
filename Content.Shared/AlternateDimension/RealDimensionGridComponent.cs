using Robust.Shared.Prototypes;

namespace Content.Shared.AlternateDimension;

/// <summary>
/// Indicates that this grid has alternate dimensions, and stores references to them
/// </summary>
[RegisterComponent]
public sealed partial class RealDimensionGridComponent : Component
{
    /// <summary>
    /// Stores references to all alternate reality grids generated from this grid.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AlternateDimensionPrototype>, EntityUid> AlternativeGrids = new();
}
