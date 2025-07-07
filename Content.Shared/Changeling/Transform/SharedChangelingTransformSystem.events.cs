using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Transform;

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ChangelingTransformWindupDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity TargetIdentity;

    public ChangelingTransformWindupDoAfterEvent(NetEntity targetIdentity)
    {
        TargetIdentity = targetIdentity;
    }
}
