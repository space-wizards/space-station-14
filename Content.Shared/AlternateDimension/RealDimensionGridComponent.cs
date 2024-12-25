using Robust.Shared.Prototypes;

namespace Content.Shared.AlternateDimension;

/// <summary>
/// Indicates that this grid has alternate dimensions, and stores references to them
/// </summary>
[RegisterComponent]
public sealed partial class RealDimensionGridComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<AlternateDimensionPrototype>, EntityUid> Alternatives = new();
}
