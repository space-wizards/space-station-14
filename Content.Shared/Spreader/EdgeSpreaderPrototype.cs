using Robust.Shared.Prototypes;

namespace Content.Shared.Spreader;

/// <summary>
/// Adds this node group to <see cref="Content.Server.Spreader.SpreaderSystem"/> for tick updates.
/// </summary>
[Prototype]
public sealed partial class EdgeSpreaderPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;
    
    /// <summary>
    /// How many tiles can this spread to within its spread interval?
    /// </summary>
    [DataField(required:true)] public int UpdatesPerInterval;

    /// <summary>
    /// Time in seconds between each spread.
    /// </summary>
    [DataField(required: true)] public float SpreadInterval = 1f;

    /// <summary>
    /// If true, this spreader can't spread onto spaced tiles like lattice.
    /// </summary>
    [DataField]
    public bool PreventSpreadOnSpaced = true;
}
