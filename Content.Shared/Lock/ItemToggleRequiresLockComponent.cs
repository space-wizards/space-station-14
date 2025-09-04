using Robust.Shared.GameStates;

namespace Content.Shared.Lock;

/// <summary>
/// This is used for toggleable items that require the entity to have a lock in a certain state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(LockSystem))]
public sealed partial class ItemToggleRequiresLockComponent : Component
{
    /// <summary>
    /// TRUE: the lock must be locked to toggle the item.
    /// FALSE: the lock must be unlocked to toggle the item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireLocked;

    /// <summary>
    /// Popup text for when someone tries to toggle the item, but it's locked. If null, no popup will be shown.
    /// </summary>
    [DataField]
    public LocId? LockedPopup = "lock-comp-generic-fail";
}
