using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool FangsExtended = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AvailableBlood = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TotalBloodDrank = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SpaceDamageInterval = TimeSpan.FromSeconds(1);

    public double NextSpaceDamageTick = 0f;

    public EntityUid? HomeCoffin = default!;

    //Abilities
    public Dictionary<VampirePowerKey, EntityUid> UnlockedPowers = new();

    //Blood Drinking
    public TimeSpan BloodDrainDelay = TimeSpan.FromSeconds(1);
}

[Prototype("vampirePower")]
public sealed partial class VampirePowerPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    public VampirePowerKey Key => Enum.Parse<VampirePowerKey>(ID);

    [DataField]
    public readonly string? ActionPrototype;

    [DataField]
    public readonly string? ActivationEffect;

    [DataField]
    public readonly SoundSpecifier? ActivationSound;

    [DataField]
    public readonly int ActivationCost = 0;

    [DataField]
    public readonly int UnlockRequirement = 0;

    [DataField]
    public readonly bool UsableWhileCuffed = true;

    [DataField]
    public readonly bool UsableWhileStunned = true;

    [DataField]
    public readonly bool UsableWhileMuffled = true;

    public EntityUid? Action;

    /*public VampirePowerPrototype(string? actionPrototype, string? activationEffect, SoundSpecifier? activationSound, int unlockCost, int activationCost, bool usableWhileCuffed, bool usableWhileStunned, bool usableWhileMuffled)
    {
        ActionPrototype = actionPrototype;
        ActivationEffect = activationEffect;
        ActivationSound = activationSound;
        UnlockCost = unlockCost;
        ActivationCost = activationCost;
        UsableWhileCuffed = usableWhileCuffed;
        UsableWhileStunned = usableWhileStunned;
        UsableWhileMuffled = usableWhileMuffled;
    }*/
}

[Serializable, NetSerializable]
public enum VampirePowerKey : byte
{
    ToggleFangs,
    Glare,
    DeathsEmbrace,
    DrinkBlood,
    Screech
}
