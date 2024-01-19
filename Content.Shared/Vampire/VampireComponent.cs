using Content.Shared.Body.Prototypes;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire.Components;

[RegisterComponent]
public sealed partial class VampireComponent : Component
{
    //Static prototype references
    [ValidatePrototypeId<StatusEffectPrototype>]
    public static readonly string SleepStatusEffectProto = "ForcedSleep";
    [ValidatePrototypeId<EmotePrototype>]
    public static readonly string ScreamEmoteProto = "Scream";
    [ValidatePrototypeId<EntityPrototype>]
    public static readonly string HeirloomProto = "HeirloomVampire";
    [ValidatePrototypeId<CurrencyPrototype>]
    public static readonly string CurrencyProto = "BloodEssence";

    public static readonly EntityWhitelist AcceptableFoods = new()
    {
        Tags = new() { "Pill" }
    };
    [ValidatePrototypeId<MetabolizerTypePrototype>]
    public static readonly string MetabolizerVampire = "vampire";
    [ValidatePrototypeId<MetabolizerTypePrototype>]
    public static readonly string MetabolizerBloodsucker = "bloodsucker";

    public static readonly DamageSpecifier MeleeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Slash", 10 } }
    };
    public static readonly DamageSpecifier HolyDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Burn", 10 } }
    };
    public static readonly DamageSpecifier SpaceDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Burn", 2.5 } }
    };

    [ValidatePrototypeId<EntityPrototype>]
    public static readonly string SummonActionPrototype = "ActionVampireSummonHeirloom";

    /// <summary>
    /// Total blood drank, counter for end of round screen
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TotalBloodDrank = 0;

    /// <summary>
    /// How much blood per mouthful
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MouthVolume = 5;

    /// <summary>
    /// Uid of the last coffin the vampire slept in
    /// TODO: UI prompt client side to set this
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? HomeCoffin = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<VampirePowerKey> AbilityStates = new();
    /// <summary>
    /// All unlocked abilities
    /// </summary>
    public Dictionary<VampirePowerKey, EntityUid?> UnlockedPowers = new();

    /// <summary>
    /// Link to the vampires heirloom
    /// </summary>
    public EntityUid? Heirloom = default!;

    /// <summary>
    /// Current available balance, used to sync currency across heirlooms and add essence as we feed
    /// </summary>
    public Dictionary<string, FixedPoint2> Balance = default!;

    public readonly SoundSpecifier BloodDrainSound = new SoundPathSpecifier("/Audio/Items/drink.ogg", new AudioParams() { Volume = -3f, MaxDistance = 3f });
    public readonly SoundSpecifier AbilityPurchaseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}


/// <summary>
/// Contains all details about the ability and its effects or restrictions
/// </summary>
[DataDefinition
public sealed partial class VampirePowerProtype
{
    [DataField(required: true)]
    public VampirePowerKey Type;
    [DataField]
    public float ActivationCost = 0;
    [DataField]
    public bool UsableWhileCuffed = true;
    [DataField]
    public bool UsableWhileStunned = true;
    [DataField]
    public bool UsableWhileMuffled = true;
    [DataField]
    public DamageSpecifier? Damage = default!;
    [DataField]
    public TimeSpan? Duration = TimeSpan.Zero;
    [DataField]
    public TimeSpan? DoAfterDelay = TimeSpan.Zero;
    [DataField]
    public string? PolymorphTarget = default!;
    [DataField]
    public float Upkeep = 0;
}

/// <summary>
/// Marks an entity as taking damage when hit by a bible, rather than being healed
/// </summary>
[RegisterComponent]
public sealed partial class UnholyComponent : Component
{
}

/// <summary>
/// Marks a container as a coffin, for the purposes of vampire healing
/// </summary>
[RegisterComponent]
public sealed partial class CoffinComponent : Component
{
}
[RegisterComponent]
public sealed partial class VampireHeirloomComponent : Component
{
    //Use of the heirloom is limited to this entity
    public EntityUid? VampireOwner = default!;
}
[RegisterComponent]
public sealed partial class VampireHealingComponent : Component
{
    public double NextHealTick = 0;

    public DamageSpecifier Healing = new DamageSpecifier()
    {
        DamageDict = new Dictionary<string, FixedPoint2>()
        {
            { "Blunt", 2 },
            { "Slash", 2 },
            { "Pierce", 2 },
            { "Heat", 1 },
            { "Cold", 2 },
            { "Shock", 2 },
            { "Caustic", 2 },
            { "Airloss", 2 },
            { "Bloodloss", 2 },
            { "Genetic", 2 }
        }
    };
}

[RegisterComponent]
public sealed partial class VampireSealthComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float NextStealthTick = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Upkeep = 0;
}

[Serializable, NetSerializable]
public enum VampirePowerKey : byte
{
    ToggleFangs,
    Glare,
    DeathsEmbrace,
    Screech,
    Hypnotise,
    Polymorph,
    NecroticTouch,
    BloodSteal,
    CloakOfDarkness,
    StellarWeakness,
    SummonHeirloom,

    //Passives
    UnnaturalStrength,
    SupernaturalStrength
}
