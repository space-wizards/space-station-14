using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Transform;

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent;

/// <summary>
/// DoAfterevent used to transform a changeling into one of their stored identities.
/// </summary>
/// <remarks>
/// DoAfter events should not store data. See the code comment in the parent class.
/// And NetEntites are not serializable, so this will cause errors once the analyzer PR for this is merged.
/// Fix before merging! - Slarti
/// </remarks>
[Serializable, NetSerializable]
public sealed partial class ChangelingTransformDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity TargetIdentity;

    public ChangelingTransformDoAfterEvent(NetEntity targetIdentity)
    {
        TargetIdentity = targetIdentity;
    }
}
