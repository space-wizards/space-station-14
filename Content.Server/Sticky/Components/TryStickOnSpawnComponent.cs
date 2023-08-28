namespace Content.Server.Sticky.Components;

/// <summary>
///     Attempts to stick entity on spawn to other valid entities nearby
/// </summary>
[RegisterComponent]
public sealed partial class TryStickOnSpawnComponent : Component
{
    /// <summary>
    ///     Range where it will search object to stick onto
    /// </summary>
    [DataField("range")]
    public float Range = 0.25f;
}
