using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Necromant;

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

public sealed partial class NecromantShopActionEvent : InstantActionEvent
{
}

public sealed partial class NecromantRaiseTwitcherActionEvent : InstantActionEvent
{
}

public sealed partial class NecromantRaiseInfectorActionEvent : InstantActionEvent
{
}

public sealed partial class NecromantRaiseDivaderActionEvent : InstantActionEvent
{
}

public sealed partial class NecromantRaisePregnantActionEvent : InstantActionEvent
{
}

public sealed partial class NecromantRaiseArmyActionEvent : InstantActionEvent
{
}

public sealed partial class NecromantRaiseBruteActionEvent : InstantActionEvent
{
}


[NetSerializable, Serializable]
public enum NecromantVisuals : byte
{
    Stunned,
    Harvesting,
}
