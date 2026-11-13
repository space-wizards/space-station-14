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
public sealed partial class ChangelingDevourWindupDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// The Consumption DoAfter has either successfully been completed or was canceled.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingDevourConsumeDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Used to skip doafters by horror forms
/// </summary>
[ByRefEvent]
public sealed partial class ChangelingDevourTryWindupEvent : EntityEventArgs
{
    /// <summary>
    /// Multiplies the windup's time. If 0, that doafter is skipped and the devour goes straight to the consume
    /// </summary>
    public float Multiplier = 1f;

    /// <summary>
    /// The target entity
    /// </summary>
    public EntityUid Target;
}

/// <summary>
/// Used to make devours faster for horror forms
/// </summary>
[ByRefEvent]
public sealed partial class ChangelingTryDevourEvent : EntityEventArgs
{
    /// <summary>
    /// Multiplies the doafter's time. If 0, that doafter is skipped and the devour is instantaneous
    /// </summary>
    public float Multiplier = 1f;

    /// <summary>
    /// The target entity
    /// </summary>
    public EntityUid Target;

    /// <summary>
    /// Wether the devour will break on damage
    /// </summary>
    public bool BreakOnDamage = true;
}

/// <summary>
/// Raised when a devour is finished
/// </summary>
public sealed partial class ChangelingEntityDevouredEvent : EntityEventArgs
{
    /// <summary>
    /// The entity that was just devoured
    /// </summary>
    public EntityUid Entity;
}
