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
