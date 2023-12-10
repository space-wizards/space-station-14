using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Zombies;

[RegisterComponent, NetworkedComponent]
public sealed partial class ZombieComponent : Component
{
    /// <summary>
    /// The baseline infection chance you have if you are completely nude
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxZombieInfectionChance = 0.80f; //SS220-zomb_reb

    /// <summary>
    /// The minimum infection chance possible. This is simply to prevent
    /// being invincible by bundling up.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MinZombieInfectionChance = 0.05f; //SS220-zomb_reb

    [ViewVariables(VVAccess.ReadWrite)]
    public float ZombieMovementSpeedDebuff = 0.85f; //SS220-zomb_reb

    /// <summary>
    /// The skin color of the zombie
    /// </summary>
    [DataField("skinColor")]
    public Color SkinColor = new(0.45f, 0.51f, 0.29f);

    /// <summary>
    /// The eye color of the zombie
    /// </summary>
    [DataField("eyeColor")]
    public Color EyeColor = new(0.96f, 0.13f, 0.24f);

    /// <summary>
    /// The base layer to apply to any 'external' humanoid layers upon zombification.
    /// </summary>
    [DataField("baseLayerExternal")]
    public string BaseLayerExternal = "MobHumanoidMarkingMatchSkin";

    /// <summary>
    /// The attack arc of the zombie
    /// </summary>
    [DataField("attackArc", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string AttackAnimation = "WeaponArcBite";

    /// <summary>
    /// The role prototype of the zombie antag role
    /// </summary>
    [DataField("zombieRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string ZombieRoleId = "Zombie";

    /// <summary>
    /// The EntityName of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeZombifiedEntityName = string.Empty;

    /// <summary>
    /// The CustomBaseLayers of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedCustomBaseLayers")]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> BeforeZombifiedCustomBaseLayers = new ();

    /// <summary>
    /// The skin color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedSkinColor")]
    public Color BeforeZombifiedSkinColor;

    /// <summary>
    /// The eye color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedEyeColor")]
    public Color BeforeZombifiedEyeColor;

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Zombie";

    public EmoteSoundsPrototype? EmoteSounds;

    [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    [DataField("zombieStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string ZombieStatusIcon = "ZombieFaction";

    /// <summary>
    /// Healing each second
    /// </summary>
    [DataField("passiveHealing")]
    public DamageSpecifier PassiveHealing = new()
    {
        DamageDict = new ()
        //SS220-zomb_reb
        {
            { "Blunt", -1 },
            { "Slash", -1 },
            { "Piercing", -1 },
            { "Heat", -0.5 },
            { "Shock", -0.5 },
            { "Cold", -0.5 }
        }
        //SS220-zomb_reb
    };

    /// <summary>
    /// A multiplier applied to <see cref="PassiveHealing"/> when the entity is in critical condition.
    /// </summary>
    [DataField("passiveHealingCritMultiplier")]
    public float PassiveHealingCritMultiplier = 2f;

    /// <summary>
    /// Healing given when a zombie bites a living being.
    /// </summary>
    [DataField("healingOnBite")]
    public DamageSpecifier HealingOnBite = new()
    {
        DamageDict = new()
        //SS220-zomb_reb
        {
            { "Blunt", -10 },
            { "Slash", -10 },
            { "Piercing", -10 },
            { "Heat", -10 },
            { "Shock", -10 },
            { "Cold", -10 },
            { "Stamina", -25 }
        }
        //SS220-zomb_reb
    };

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    /// <summary>
    /// The blood reagent of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedBloodReagent")]
    public string BeforeZombifiedBloodReagent = string.Empty;

    /// <summary>
    /// The blood reagent to give the zombie. In case you want zombies that bleed milk, or something.
    /// </summary>
    [DataField("newBloodReagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string NewBloodReagent = "ZombieBlood";
}
