using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Robust.Shared.Serialization;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains all the events raised by the SharedStunSystem
/// </summary>

/// <summary>
///     Raised directed on an entity when it is stunned.
/// </summary>
[ByRefEvent]
public record struct StunnedEvent;

/// <summary>
///     Raised on a stunned entity when something wants to remove the stunned component.
/// </summary>
[ByRefEvent]
public record struct StunEndAttemptEvent(bool Cancelled);

/// <summary>
///     Raised directed on an entity before it is knocked down to see if it should be cancelled, and to determine
///     knocked down arguments.
/// </summary>
[ByRefEvent]
public record struct KnockDownAttemptEvent(bool AutoStand, bool Drop, TimeSpan? Time)
{
    public bool Cancelled;
}

/// <summary>
///     Raised directed on an entity when it is knocked down.
/// </summary>
[ByRefEvent]
public record struct KnockedDownEvent;

/// <summary>
///     Raised on an entity that needs to refresh its knockdown modifiers
/// </summary>
[ByRefEvent]
public record struct KnockedDownRefreshEvent()
{
    public float SpeedModifier = 1f;
    public float FrictionModifier = 1f;
}

/// <summary>
///     Raised directed on an entity when it tries to stand up
/// </summary>
/// <param name="Autostand">If the attempt was cancelled, passes a recommended value to change autostand to.</param>
[ByRefEvent]
public record struct StandUpAttemptEvent(bool Autostand)
{
    public bool Cancelled = false;

    // Popup data to display to the entity if we so desire...
    public (string, PopupType)? Message = null;
}

/// <summary>
/// Raises the default DoAfterTime for a stand-up attempt for other components to modify it.
/// </summary>
/// <param name="DoAfterTime"></param>
[ByRefEvent]
public record struct GetStandUpTimeEvent(TimeSpan DoAfterTime);

/// <summary>
/// Raised when an entity is forcing itself to stand, allows for the stamina damage it is taking to be modified.
/// This is raised before the stamina damage is taken so it can still fail if the entity does not have enough stamina.
/// </summary>
/// <param name="Stamina">The stamina damage the entity will take when it forces itself to stand.</param>
[ByRefEvent]
public record struct TryForceStandEvent(float Stamina);

/// <summary>
///     Raised when you click on the Knocked Down Alert
/// </summary>
public sealed partial class KnockedDownAlertEvent : BaseAlertEvent;

/// <summary>
/// The DoAfterEvent for trying to stand the slow and boring way.
/// </summary>
[ByRefEvent]
[Serializable, NetSerializable]
public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// An event sent by the client to the server to ask it very nicely to perform a forced stand-up.
/// </summary>
[Serializable, NetSerializable]
public sealed class ForceStandUpEvent : EntityEventArgs;

