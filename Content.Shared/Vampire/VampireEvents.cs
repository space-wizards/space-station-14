using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Store.Events;
using Content.Shared.Vampire.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

//Use power events
public sealed partial class VampireSelfPowerEvent : InstantActionEvent
{
    [DataField]
    public VampirePowerDetails Details = new();
};
public sealed partial class VampireTargetedPowerEvent : EntityTargetActionEvent
{
    [DataField]
    public VampirePowerDetails Details = new();
};
public sealed partial class VampirePassiveActionEvent : BaseActionEvent
{
    [DataField]
    public VampirePowerDetails Details = new();
};

//Doafter events
[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodEvent : DoAfterEvent
{
    [DataField]
    public float Volume = 0;
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class VampireHypnotiseEvent : DoAfterEvent
{
    [DataField]
    public TimeSpan? Duration = TimeSpan.Zero;

    public override DoAfterEvent Clone() => this;
}
