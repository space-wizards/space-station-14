using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

[Serializable, NetSerializable]
public sealed class AbsorbDNADoAfterEvent : SimpleDoAfterEvent
{
}

public sealed class AbsorbDNAActionEvent : EntityTargetActionEvent
{
    //public readonly EntityUid Target;

    // public AbsorbDNAActionEvent(EntityUid target)
    // {
    //     Target = target;
    // }
}

public sealed class AbsorbDNADoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public AbsorbDNADoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class AbsorbDNADoAfterCancelled : EntityEventArgs
{
}

public sealed class ChangelingShopActionEvent : InstantActionEvent
{
}

public sealed class ChangelingArmBladeEvent : InstantActionEvent
{
}

public sealed class ChangelingDnaStingEvent : EntityTargetActionEvent
{
}

public sealed class ChangelingFleshmendEvent : InstantActionEvent
{
}

public sealed class ChangelingTransformEvent : InstantActionEvent
{
}
