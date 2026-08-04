using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

/// <summary>
/// Action event for opening the changeling transformation radial menu.
/// </summary>
public sealed partial class ChangelingTransformActionEvent : InstantActionEvent;

/// <summary>
/// DoAfterevent used to transform a changeling into one of their stored identities.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingTransformDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Raised on a changeling before they transform into a stored identity.
/// This is raised after the DoAfter finished.
/// </summary>
public readonly record struct BeforeChangelingTransformEvent(EntityUid StoredIdentity)
{
    /// <summary>
    /// The stored identity the changeling will transform into.
    /// </summary>
    public readonly EntityUid StoredIdentity = StoredIdentity;
};

/// <summary>
/// Raised on a changeling after they successfully transformed into a stored identity.
/// </summary>
public readonly record struct AfterChangelingTransformEvent(EntityUid StoredIdentity)
{
    /// <summary>
    /// The stored identity the changeling transformed into.
    /// </summary>
    public readonly EntityUid StoredIdentity = StoredIdentity;
};

/// <summary>
/// Raised on the changeling when it is trying to transform.
/// </summary>
[ByRefEvent]
public record struct ChangelingAttemptTransformEvent(bool Cancelled, string? Reason, EntityUid Identity)
{
    /// <summary>
    /// If the attempt goes through.
    /// </summary>
    public bool Cancelled = Cancelled;
    /// <summary>
    /// If the attempt is cancelled, this reason will be displayed in an alert
    /// </summary>
    public string? Reason = Reason;
    /// <summary>
    /// The identity we are transforming into
    /// </summary>
    public EntityUid Identity = Identity;
};

/// <summary>
/// Raised on the identity a changeling is transforming into.
/// </summary>
[ByRefEvent]
public record struct ChangelingAttemptTransformIntoEvent(bool Cancelled, string? Reason, EntityUid Changeling)
{
    /// <summary>
    /// If the attempt goes through.
    /// </summary>
    public bool Cancelled = Cancelled;
    /// <summary>
    /// If the attempt is cancelled, this reason will be displayed in an alert
    /// </summary>
    public string? Reason = Reason;
    /// <summary>
    /// The changeling which is transforming
    /// </summary>
    public EntityUid Changeling = Changeling;
};
