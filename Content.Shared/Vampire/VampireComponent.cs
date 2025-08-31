using Content.Shared.Body.Prototypes;
using Content.Shared.StatusIcon;
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
using Robust.Shared.GameStates;

namespace Content.Shared.Vampire.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VampireComponent : Component
{
    //Static prototype references
    [ValidatePrototypeId<EmotePrototype>]
    public static readonly string ScreamEmoteProto = "Scream";
    [ValidatePrototypeId<CurrencyPrototype>]
    public static readonly string CurrencyProto = "BloodEssence";

    [ViewVariables(VVAccess.ReadOnly), DataField("defaultMutation")]
    public VampireMutationsType DefaultMutation = VampireMutationsType.None;
    [ViewVariables(VVAccess.ReadOnly), DataField("currentMutation")]
    public VampireMutationsType CurrentMutation = VampireMutationsType.None;

    public readonly HashSet<VampireMutationsType> VampireMutations = new()
    {
        VampireMutationsType.None,
        VampireMutationsType.Hemomancer,
        VampireMutationsType.Umbrae,
        VampireMutationsType.Gargantua,
        //VampireMutationsType.Dantalion,
        VampireMutationsType.Bestia
    };

    public static readonly EntityWhitelist AcceptableFoods = new()
    {
        Tags = new() { "Pill" }
    };
    [ValidatePrototypeId<MetabolizerTypePrototype>]
    public static readonly string MetabolizerVampire = "Vampire";
    [ValidatePrototypeId<MetabolizerTypePrototype>]
    public static readonly string MetabolizerBloodsucker = "Bloodsucker";

    public static readonly DamageSpecifier MeleeDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Slash", 10 } }
    };
    public static readonly DamageSpecifier HolyDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Burn", 10 } }
    };

    [ValidatePrototypeId<EntityPrototype>]
    public static readonly string MutationsActionPrototype = "ActionVampireOpenMutationsMenu";

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? MutationsAction;

    public readonly List<ProtoId<EntityPrototype>> BaseVampireActions = new()
    {
        "ActionVampireToggleFangs",
        "ActionVampireHypnotise"
    };

    [ValidatePrototypeId<VampirePowerProtype>]
    public static readonly string DrinkBloodPrototype = "DrinkBlood";

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
    /// All unlocked abilities
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<string, NetEntity?> UnlockedPowers = new();

    /// <summary>
    /// All abilities
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public Dictionary<string, AbilityInfo> actionEntities = new();

    /// <summary>
    /// Current available balance, used to sync currency across heirlooms and add essence as we feed
    /// </summary>
    public Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> Balance = default!;

    public readonly SoundSpecifier BloodDrainSound = new SoundPathSpecifier("/Audio/Items/drink.ogg", new AudioParams() { Volume = -3f, MaxDistance = 3f });
    public readonly SoundSpecifier AbilityPurchaseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}

/// <summary>
/// Struct contains Action and Owner
/// </summary>
[Serializable, NetSerializable]
public struct AbilityInfo
{
    public NetEntity Owner;
    public NetEntity Action;

    public AbilityInfo(NetEntity owner, NetEntity action)
    {
        Owner = owner;
        Action = action;
    }
}


/// <summary>
/// Contains all details about the ability and its effects or restrictions
/// </summary>
[DataDefinition]
[Prototype("vampirePower")]
public sealed partial class VampirePowerProtype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; }

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

[DataDefinition]
[Prototype("vampirePassive")]
public sealed partial class VampirePassiveProtype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; }

    [DataField(required: true)]
    public string CatalogEntry = string.Empty;

    [DataField]
    public ComponentRegistry CompsToAdd = new();

    [DataField]
    public ComponentRegistry CompsToRemove = new();
}

/// <summary>
/// Marks an entity as taking damage when hit by a bible, rather than being healed
/// </summary>
[RegisterComponent]
public sealed partial class UnholyComponent : Component { }

/// <summary>
/// Marks a container as a coffin, for the purposes of vampire healing
/// </summary>
[RegisterComponent]
public sealed partial class CoffinComponent : Component { }

[RegisterComponent]
public sealed partial class VampireFangsExtendedComponent : Component { }

/// <summary>
/// When added, damage the entity if its on the space
/// </summary>
[RegisterComponent]
public sealed partial class VampireSpaceDamageComponent : Component
{
    public double NextSpaceDamageTick = 0;
}

/// <summary>
/// When added, heals the entity by the specified amount
/// </summary>
[RegisterComponent]
public sealed partial class VampireHealingComponent : Component
{
    public double NextHealTick = 0;

    public DamageSpecifier? Healing = default!;
}

[RegisterComponent]
public sealed partial class VampireDeathsEmbraceComponent : Component
{
    [ViewVariables()]
    public EntityUid? HomeCoffin = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Cost = 0;

    [DataField]
    public DamageSpecifier CoffinHealing = default!;
}
[RegisterComponent]
public sealed partial class VampireSealthComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float NextStealthTick = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Upkeep = 0;
}
[RegisterComponent]
public sealed partial class VampireStrengthComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float NextTick = 0;

    [ViewVariables(VVAccess.ReadOnly)]
    public string Power = "";

    [ViewVariables(VVAccess.ReadWrite)]
    public float Upkeep = 0;
}

[Serializable, NetSerializable]
public enum VampireMutationsType : byte
{
    None,
    Hemomancer,
    Umbrae,
    Gargantua,
    Dantalion,
    Bestia
}

[Serializable, NetSerializable]
public sealed class VampireMutationComponentState : ComponentState
{
    public VampireMutationsType SelectedMutation;
}

[Serializable, NetSerializable]
public sealed class VampireMutationBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly HashSet<VampireMutationsType> MutationList;
    public readonly VampireMutationsType SelectedMutation;

    public VampireMutationBoundUserInterfaceState(HashSet<VampireMutationsType> mutationList, VampireMutationsType selectedId)
    {
        MutationList = mutationList;
        SelectedMutation = selectedId;
    }
}

[Serializable, NetSerializable]
public sealed class VampireMutationPrototypeSelectedMessage : BoundUserInterfaceMessage
{
    public readonly VampireMutationsType SelectedId;

    public VampireMutationPrototypeSelectedMessage(VampireMutationsType selectedId)
    {
        SelectedId = selectedId;
    }
}

[Serializable, NetSerializable]
public enum VampireMutationUiKey : byte
{
    Key
}

[NetSerializable, Serializable]
public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3
}

/*[Serializable, NetSerializable]
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
}*/
