using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Transform;

/// <summary>
/// Action event for opening the changeling transformation radial menu.
/// </summary>
public sealed partial class ChangelingTransformActionEvent : InstantActionEvent;

/// <summary>
/// DoAfterevent used to transform a changeling into one of their stored identities.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ChangelingTransformDoAfterEvent : SimpleDoAfterEvent;
