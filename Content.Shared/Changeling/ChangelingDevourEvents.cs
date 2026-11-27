using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

/// <summary>
/// Action event for Devour, someone has initiated a devour on someone, begin to windup.
/// </summary>
public sealed partial class ChangelingDevourActionEvent : EntityTargetActionEvent;

/// <summary>
/// A windup has either successfully been completed or has been canceled. If successful start the devouring DoAfter.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingDevourWindupDoAfterEvent : SimpleDoAfterEvent
{
    /// <summary>
    /// Multiplies the doafter's time. If 0, that doafter is skipped and the devour is instantaneous
    /// </summary>
    public float DevourMultiplier = 1f;

    /// <summary>
    /// Wether the devour will break on damage
    /// </summary>
    public bool BreakOnDamage = true;
}

/// <summary>
/// The Consumption DoAfter has either successfully been completed or was canceled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingDevourConsumeDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Used to make devours faster for horror forms
/// </summary>
[ByRefEvent]
public sealed partial class ChangelingDevourAttemptEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// Multiplies the doafter's time. If 0, that doafter is skipped and the devour is instantaneous
    /// </summary>
    public float DevourMultiplier = 1f;

    /// <summary>
    /// The target entity
    /// </summary>
    public EntityUid Target;

    /// <summary>
    /// Wether the devour will break on damage
    /// </summary>
    public bool BreakOnDamage = true;

    /// <summary>
    /// Multiplies the windup's time. If 0, that doafter is skipped and the devour goes straight to the consume. The windup is the part where the changeling reveals its uncanny mouth.
    /// </summary>
    public float WindupMultiplier = 1f;
}

