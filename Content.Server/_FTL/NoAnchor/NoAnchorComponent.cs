namespace Content.Server._FTL.NoAnchor;

/// <summary>
/// This is used for tracking components that wont be able to anchored/unanchored
/// </summary>
[RegisterComponent]
public sealed class NoAnchorComponent : Component
{
    [DataField("stopOnAnchor"), ViewVariables(VVAccess.ReadWrite)]
    public bool StopOnAnchorAttempt = true;
    [DataField("stopOnUnnchor"), ViewVariables(VVAccess.ReadWrite)]
    public bool StopOnUnanchorAttempt = true;
}
