namespace Content.Server.Spreader;

[RegisterComponent]
public sealed partial class SpreaderGridComponent : Component
{
    [DataField]
    public float UpdateAccumulator = SpreaderSystem.SpreadCooldownSeconds;

    /// <summary>
    /// Remaining updates per prototype for this grid. Index corresponds to prototype index.
    /// Null if not initialized for current prototype set.
    /// </summary>
    public int[]? RemainingUpdates;
}
