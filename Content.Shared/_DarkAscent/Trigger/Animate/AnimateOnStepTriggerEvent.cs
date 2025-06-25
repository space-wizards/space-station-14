using Robust.Shared.Serialization;

namespace Content.Shared._DarkAscent.Trigger.Animate;

[Serializable, NetSerializable]
public sealed class AnimateOnStepTriggerEvent(NetEntity uid) : EntityEventArgs
{
    public NetEntity Uid = uid;
}
