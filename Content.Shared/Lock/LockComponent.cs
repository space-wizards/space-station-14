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
    [ViewVariables(VVAccess.ReadWrite)] [DataField("locked")]
    public bool Locked  = true;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("lockOnClick")]
    public bool LockOnClick;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("unlockingSound")]
    public SoundSpecifier UnlockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg");

    [ViewVariables(VVAccess.ReadWrite)] [DataField("lockingSound")]
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
public struct LockToggleAttemptEvent
{
    public bool Silent = false;
    public bool Cancelled = false;
    public EntityUid User;

    public LockToggleAttemptEvent(EntityUid user, bool silent = false)
    {
        User = user;
        Silent = silent;
    }
}

[ByRefEvent]
public readonly record struct LockToggledEvent(bool Locked);
