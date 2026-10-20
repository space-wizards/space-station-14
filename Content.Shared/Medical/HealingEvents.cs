using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

[Serializable, NetSerializable]
public sealed partial class HealingEvents : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class HealingAnimationEvent(NetEntity target) : EntityEventArgs
{
    public readonly NetEntity Target = target;
}
