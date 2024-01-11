using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Vampire.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

public sealed partial class VampireUseAreaPowerEvent : InstantActionEvent
{
    [DataField("type")]
    public VampirePowerKey Type;
};
public sealed partial class VampireUseTargetedPowerEvent : EntityTargetActionEvent
{
    [DataField("type")]
    public VampirePowerKey Type;
};

[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
