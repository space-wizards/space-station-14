using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

public sealed partial class LingAbsorbActionEvent : EntityTargetActionEvent
{
}

public sealed partial class LingStingExtractActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AbsorbDoAfterEvent : SimpleDoAfterEvent
{
}
public sealed partial class ChangelingEvolutionMenuActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingCycleDNAActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{
}

public sealed partial class LingRegenerateActionEvent : InstantActionEvent
{
}

public sealed partial class ArmBladeActionEvent : InstantActionEvent
{
}

public sealed partial class LingArmorActionEvent : InstantActionEvent
{
}

public sealed partial class LingInvisibleActionEvent : InstantActionEvent
{
}

public sealed partial class LingEMPActionEvent : InstantActionEvent
{
}