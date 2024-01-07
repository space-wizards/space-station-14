using Robust.Shared.Prototypes;

namespace Content.Shared.Spreader;

/// <summary>
/// Adds this node group to <see cref="Content.Server.Spreader.SpreaderSystem"/> for tick updates.
/// </summary>
[Prototype("edgeSpreader")]
public sealed partial class EdgeSpreaderPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;
    [DataField(required:true)] public int UpdatesPerSecond;
}
