using Robust.Shared.Prototypes;

namespace Content.Shared.AlternateDimension;

/// <summary>
/// Indicates that this grid is an alternate dimension, and keeps references to the original grid.
/// </summary>
[RegisterComponent]
public sealed partial class AlternateDimensionGridComponent : Component
{
    [DataField]
    public ProtoId<AlternateDimensionPrototype> DimensionType = default!;

    [DataField]
    public EntityUid? RealDimensionGrid;
}
