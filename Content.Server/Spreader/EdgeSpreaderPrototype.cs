using Robust.Shared.Prototypes;

namespace Content.Server.Spreader;

/// <summary>
/// Adds this node group to <see cref="SpreaderSystem"/> for tick updates.
/// </summary>
[Prototype("edgeSpreader")]
public sealed class EdgeSpreaderPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;
}
