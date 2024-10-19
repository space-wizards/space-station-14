using Robust.Shared.Prototypes;

namespace Content.Shared._DeltaV.Shipyard.Prototypes;

/// <summary>
/// Like <c>TagPrototype</c> but for vessel categories.
/// Prevents making typos being silently ignored by the linter.
/// </summary>
[Prototype("vesselCategory")]
public sealed class VesselCategoryPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; } = default!;
}
