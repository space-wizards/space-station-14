namespace Content.Shared.ActionBlocker;

/// <summary>
/// A cache component for stuff that needs to check CanX a lot.
/// </summary>
[RegisterComponent]
public sealed class ActionBlockerComponent : Component
{
    // Suss on making it networked because ideally all the shared stuff leads to the same result.
    [ViewVariables]
    public bool? CanMove = null;
}
