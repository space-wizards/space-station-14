using Robust.Shared.Prototypes;

namespace Content.Client.Mapping.Snapping;

/// <summary>
/// This is a prototype for configuring a snapping mode.
/// </summary>
[Prototype("snappingMode")]
public sealed class SnappingModePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name", required: true)]
    public string Name { get; } = default!;

    [DataField("config", required: true)]
    public SnappingModeImpl Config { get; } = default!;
}
