using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Lock;

/// <summary>
/// Allows locking/unlocking, with access determined by AccessReader
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LockSystem))]
public sealed class LockComponent : Component
{
    /// <summary>
    /// Whether or not the lock is locked.
    /// </summary>
    [DataField("locked"), ViewVariables(VVAccess.ReadWrite)]
    public bool Locked  = true;

    /// <summary>
    /// Whether or not the lock is toggled by simply clicking.
    /// </summary>
    [DataField("lockOnClick"), ViewVariables(VVAccess.ReadWrite)]
    public bool LockOnClick;

    /// <summary>
    /// The sound played when unlocked.
    /// </summary>
    [DataField("unlockingSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier UnlockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");

    /// <summary>
    /// The sound played when locked.
    /// </summary>
    [DataField("lockingSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_on.ogg");
}

[Serializable, NetSerializable]
public sealed class LockComponentState : ComponentState
{
    public bool Locked;

    public bool LockOnClick;

    public LockComponentState(bool locked, bool lockOnClick)
    {
        Locked = locked;
        LockOnClick = lockOnClick;
    }
}

[ByRefEvent]
public record struct LockToggleAttemptEvent(EntityUid User, bool Silent = false, bool Cancelled = false);

[ByRefEvent]
public readonly record struct LockToggledEvent(bool Locked);
