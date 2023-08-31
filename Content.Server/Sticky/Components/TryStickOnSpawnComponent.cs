namespace Content.Server.Sticky.Components;

/// <summary>
///     Attempts to stick entity on spawn to other valid entities nearby
/// </summary>
[RegisterComponent]
public sealed partial class TryStickOnSpawnComponent : Component
{
    /// <summary>
    ///     Range where it will search entity to stick on
    /// </summary>
    [DataField("range")]
    public float Range = 0.25f;

    /// <summary>
    ///     Has it already stuck entity once
    /// </summary>
    [DataField("shot")]
    public bool Shot = false;
}
