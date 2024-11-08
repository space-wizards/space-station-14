using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Vampire.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Vampire;

//Use power events
public sealed partial class VampireToggleFangsEvent : VampireSelfPowerEvent { }
public sealed partial class VampireOpenMutationsMenu : InstantActionEvent { }
public sealed partial class VampireScreechEvent : VampireSelfPowerEvent { }
public sealed partial class VampirePolymorphEvent : VampireSelfPowerEvent { }
public sealed partial class VampireBloodStealEvent : VampireSelfPowerEvent { }
public sealed partial class VampireUnholyStrengthEvent : VampireSelfPowerEvent { }
public sealed partial class VampireSupernaturalStrengthEvent : VampireSelfPowerEvent { }
public sealed partial class VampireCloakOfDarknessEvent : VampireSelfPowerEvent { }

public sealed partial class VampireGlareEvent : VampireTargetedPowerEvent { }
public sealed partial class VampireHypnotiseEvent : VampireTargetedPowerEvent { }


public abstract partial class VampireSelfPowerEvent : InstantActionEvent
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<VampirePowerProtype>))]
    public string DefinitionName = default!;
};
public abstract partial class VampireTargetedPowerEvent : EntityTargetActionEvent
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<VampirePowerProtype>))]
    public string DefinitionName = default!;
};
public sealed partial class VampirePassiveActionEvent : BaseActionEvent
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<VampirePowerProtype>))]
    public string DefinitionName = default!;
};

//Purchase passive events
[Serializable, NetSerializable]
public sealed partial class VampirePurchaseUnnaturalStrength : EntityEventArgs { }

[Serializable, NetSerializable]
public sealed partial class VampireBloodChangedEvent : EntityEventArgs { }

//Doafter events
[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodDoAfterEvent : DoAfterEvent
{
    [DataField]
    public float Volume = 0;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class VampireHypnotiseDoAfterEvent : DoAfterEvent
{
    [DataField]
    public TimeSpan? Duration = TimeSpan.Zero;

    public override DoAfterEvent Clone() => this;
}
