using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Vampire.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

public sealed partial class VampireUseAreaPowerEvent : InstantActionEvent
{
    [DataField]
    public VampirePowerKey Type;
};
public sealed partial class VampireUseTargetedPowerEvent : EntityTargetActionEvent
{
    [DataField]
    public VampirePowerKey Type;
};

[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class VampireHypnotiseEvent : DoAfterEvent
{
    [DataField]
    public float Duration = 0;

    public VampireHypnotiseEvent(float duration)
    {
        Duration = duration;
    }
    public override DoAfterEvent Clone() => this;
}
[Serializable, NetSerializable]
public sealed partial class VampireAbilityUnlockedEvent : EntityEventArgs
{
    public VampirePowerKey UnlockedAbility = default!;
}
