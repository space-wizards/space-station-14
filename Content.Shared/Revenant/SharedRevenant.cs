using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Revenant;

[Serializable, NetSerializable]
public sealed partial class SoulEvent : SimpleDoAfterEvent
{
}

public sealed class SoulSearchDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public SoulSearchDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class SoulSearchDoAfterCancelled : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class HarvestEvent : SimpleDoAfterEvent
{
}

public sealed class HarvestDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public HarvestDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs
{
}

public sealed partial class RevenantShopActionEvent : InstantActionEvent
{
}

public sealed partial class RevenantHauntActionEvent : InstantActionEvent
{
}

public sealed partial class RevenantDefileActionEvent : InstantActionEvent
{
}

public sealed partial class RevenantOverloadLightsActionEvent : InstantActionEvent
{
}

public sealed partial class RevenantBlightActionEvent : InstantActionEvent
{
}

public sealed partial class RevenantMalfunctionActionEvent : InstantActionEvent
{
}

public sealed partial class RevenantBloodWritingEvent : InstantActionEvent
{
}

public sealed partial class RevenantAnimateEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RevenantHauntWitnessEvent : EntityEventArgs
{
    public HashSet<NetEntity> Witnesses = new();

    public RevenantHauntWitnessEvent(HashSet<NetEntity> witnesses)
    {
        Witnesses = witnesses;
    }

    public RevenantHauntWitnessEvent() : this(new())
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class ExorciseRevenantDoAfterEvent : SimpleDoAfterEvent
{
}


[NetSerializable, Serializable]
public enum RevenantVisuals : byte
{
    Corporeal,
    Stunned,
    Harvesting,
}

[NetSerializable, Serializable]
public enum RevenantVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3
}
