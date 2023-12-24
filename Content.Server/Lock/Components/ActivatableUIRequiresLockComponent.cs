namespace Content.Server.Lock.Components;

/// <summary>
/// This is used for activatable UIs that require the entity to have a lock in a certain state.
/// </summary>
[RegisterComponent]
public sealed partial class ActivatableUIRequiresLockComponent : Component
{
    /// <summary>
    /// TRUE: the lock must be locked to access the UI.
    /// FALSE: the lock must be unlocked to access the UI.
    /// </summary>
    [DataField("requireLocked"), ViewVariables(VVAccess.ReadWrite)]
    public bool requireLocked = false;
}
