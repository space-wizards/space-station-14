using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Lock;

/// <summary>
/// Allows locking/unlocking, with access determined by AccessReader
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(LockSystem))]
[AutoGenerateComponentState]
public sealed partial class LockComponent : Component
{
    /// <summary>
    /// Whether or not the lock is locked.
    /// </summary>
    [DataField("locked"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Locked  = true;

    /// <summary>
    /// Whether or not the lock is toggled by simply clicking.
    /// </summary>
    [DataField("lockOnClick"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
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

    /// <summary>
    /// Whether or not an emag disables it.
    /// </summary>
    [DataField("breakOnEmag")]
    [AutoNetworkedField]
    public bool BreakOnEmag = true;
}

/// <summary>
/// Event raised on the lock when a toggle is attempted.
/// Can be cancelled to prevent it.
/// </summary>
[ByRefEvent]
public record struct LockToggleAttemptEvent(EntityUid User, bool Silent = false, bool Cancelled = false);

/// <summary>
/// Event raised on a lock after it has been toggled.
/// </summary>
[ByRefEvent]
public readonly record struct LockToggledEvent(bool Locked);
