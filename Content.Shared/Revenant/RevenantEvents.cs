using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Revenant;

[Serializable, NetSerializable]
public sealed partial class SoulEvent : SimpleDoAfterEvent;

public sealed class SoulSearchDoAfterComplete(EntityUid target) : EntityEventArgs
{
    public readonly EntityUid Target = target;
}

public sealed class SoulSearchDoAfterCancelled : EntityEventArgs;

[Serializable, NetSerializable]
public sealed partial class HarvestEvent : SimpleDoAfterEvent;

public sealed class HarvestDoAfterComplete(EntityUid target) : EntityEventArgs
{
    public readonly EntityUid Target = target;
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs;

public sealed partial class RevenantShopActionEvent : InstantActionEvent;

public sealed partial class RevenantDefileActionEvent : InstantActionEvent;

public sealed partial class RevenantOverloadLightsActionEvent : InstantActionEvent;

public sealed partial class RevenantColdSnapActionEvent : InstantActionEvent;

public sealed partial class RevenantEnergyDrainActionEvent : InstantActionEvent;

public sealed partial class RevenantMalfunctionActionEvent : InstantActionEvent;

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
    Digit3,
}
