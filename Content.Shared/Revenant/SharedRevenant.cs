using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Revenant;

public sealed class SoulSearchDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public SoulSearchDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class SoulSearchDoAfterCancelled : EntityEventArgs { }

public sealed class HarvestDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public HarvestDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs { }
public sealed class RevenantShopActionEvent : InstantActionEvent { }
public sealed class RevenantDefileActionEvent : InstantActionEvent { }
public sealed class RevenantOverloadLightsActionEvent : InstantActionEvent { }
public sealed class RevenantBlightActionEvent : InstantActionEvent { }
public sealed class RevenantMalfunctionActionEvent : InstantActionEvent { }

[NetSerializable, Serializable]
public enum RevenantVisuals : byte
{
    Corporeal,
    Stunned,
    Harvesting,
}
