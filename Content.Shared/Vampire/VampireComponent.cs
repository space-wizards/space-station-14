using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Stealth.Components;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
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
    /// <summary>
    /// How much blood is available for abilities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AvailableBlood = default!;

    /// <summary>
    /// Total blood drank, counter for end of round screen
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalBloodDrank = default!;

    /// <summary>
    /// How long till we apply another tick of space damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public double NextSpaceDamageTick = 0f;

    /// <summary>
    /// Uid of the last coffin the vampire slept in
    /// TODO: UI prompt client side to set this
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? HomeCoffin = default!;

    /// <summary>
    /// Which ability list has the vampire chosen
    /// TODO: Add ability lists
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public VampireAbilityListPrototype ChosenAbilityList = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<VampirePowerKey> ActiveAbilities = new();
    /// <summary>
    /// All unlocked abilities
    /// </summary>
    public Dictionary<VampirePowerKey, EntityUid?> UnlockedPowers = new();

    public SoundSpecifier BloodDrainSound = new SoundPathSpecifier("/Audio/Items/drink.ogg", new AudioParams() { Volume = -3f });
}

[RegisterComponent]
public sealed partial class UnholyComponent : Component
{
}
[RegisterComponent]
public sealed partial class CoffinComponent : Component
{
}
[RegisterComponent]
public sealed partial class VampireHealingComponent : Component
{
    public double NextHealTick = 0;
}

[RegisterComponent]
public sealed partial class VampireSealthComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float NextStealthTick = 0;
}

[Prototype("vampireAbilityList")]
public sealed partial class VampireAbilityListPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public List<VampireAbilityEntry> Abilities = new();

    /// <summary>
    /// For quick reference, populated at system init
    /// </summary>
    public FrozenDictionary<VampirePowerKey, VampireAbilityEntry> AbilitiesByKey = default!;

    [DataField(required: true)]
    public DamageSpecifier SpaceDamage = default!;

    [DataField(required: true)]
    public DamageSpecifier MeleeDamage = default!;

    [DataField(required: true)]
    public DamageSpecifier CoffinHealing = default!;

    [DataField]
    public float BloodDrainVolume = 5;

    [DataField]
    public float BloodDrainFrequency = 1;

    [DataField]
    public float SpaceDamageFrequency = 2;

    [DataField]
    public float StealthBloodCost = 5;

    [DataField]
    public EntityWhitelist AcceptableFoods = new EntityWhitelist() { Tags = new() { "Pill" } };
}

[DataDefinition]
public sealed partial class VampireAbilityEntry
{
    [DataField]
    public string? ActionPrototype = default!;

    [DataField]
    public int BloodUnlockRequirement = 0;
    [DataField]
    public float ActivationCost = 0;
    [DataField]
    public bool UsableWhileCuffed = true;
    [DataField]
    public bool UsableWhileStunned = true;
    [DataField]
    public bool UsableWhileMuffled = true;
    [DataField(required: true)]
    public VampirePowerKey Type = default!;
    [DataField]
    public string ActivationEffect = default!;
    [DataField]
    public DamageSpecifier Damage = default!;
    [DataField]
    public float Duration = 0;
    [DataField]
    public float Delay = 0;
}

[Serializable, NetSerializable]
public enum VampirePowerKey : byte
{
    ToggleFangs,
    Glare,
    DeathsEmbrace,
    Screech,
    Hypnotise,
    BatForm,
    NecroticTouch,
    BloodSteal,
    CloakOfDarkness,
    StellarWeakness
}
