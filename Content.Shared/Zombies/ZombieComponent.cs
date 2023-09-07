using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Zombies;

/// <summary>
///   Applied to any mob that is carrying a zombie infection in any state.
///   That mob will also have one of these additional components depending on the stage of infection:
///   - InitialInfectedComponent - Human Patient0 players who are not being hurt by the infection yet.
///   - PendingZombieComponent - The painful stage of the infection.
///   - LivingZombieComponent - An active zombie either alive or in crit (and about to heal back to life)
///   - [none of these] - This zombie has turned from undead to actually dead.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedZombieSystem))]
public sealed partial class ZombieComponent : Component
{
    /// The baseline infection chance you have if you are completely nude
    /// </summary>
    [DataField("maxInfectionChance"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxInfectionChance = 0.50f;

    /// <summary>
    /// The minimum infection chance possible. This is simply to prevent
    /// being invincible by bundling up.
    /// </summary>
    [DataField("minInfectionChance"), ViewVariables(VVAccess.ReadWrite)]
    public float MinInfectionChance = 0.10f;

    [DataField("movementSpeedDebuff"), ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedDebuff = 0.70f;

    /// <summary>
    /// How long you have until you begin to turn to a zombie
    /// </summary>
    [DataField("infectionTurnTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan InfectionTurnTime = TimeSpan.FromSeconds(30.0f);

    /// <summary>
    /// Minimum time a zombie victim will lie dead before rising as a zombie.
    /// </summary>
    [DataField("deadMinTurnTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DeadMinTurnTime = TimeSpan.FromSeconds(10.0f);

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
    /// The attack range of the zombie
    /// </summary>
    [DataField("meleeRange"), ViewVariables(VVAccess.ReadWrite)]
    public float MeleeRange = 1.5f;

    /// <summary>
    /// The role prototype of the zombie antag role
    /// </summary>
    [DataField("zombieRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string ZombieRoleId = "Zombie";


    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Zombie";

    /// <summary>
    /// The blood reagent to give the zombie. In case you want zombies that bleed milk, or something.
    /// </summary>
    [DataField("newBloodReagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string NewBloodReagent = "ZombieBlood";

    /// <summary>
    /// Healing each second
    /// </summary>
    [DataField("passiveHealing")]
    public DamageSpecifier PassiveHealing = new()
    {
        DamageDict = new ()
        {
            { "Blunt", -0.4 },
            { "Slash", -0.2 },
            { "Piercing", -0.2 }
        }
    };

    /// <summary>
    /// How much the virus hurts you (base, scales rapidly)
    /// </summary>
    [DataField("virusDamage"), ViewVariables(VVAccess.ReadWrite)] public DamageSpecifier VirusDamage = new()
    {
        DamageDict = new ()
        {
            { "Blunt", 0.8 },
            { "Toxin", 0.2 },
        }
    };

    /// <summary>
    /// How much damage is inflicted per bite.
    /// </summary>
    [DataField("attackDamage"), ViewVariables(VVAccess.ReadWrite)] public DamageSpecifier AttackDamage = new()
    {
        DamageDict = new ()
        {
            { "Slash", 13 },
            { "Piercing", 7 },
            { "Structural", 10 },
        }
    };

    /// <summary>
    /// Healing given when a zombie bites a living being.
    /// </summary>
    [DataField("healingOnBite")]
    public DamageSpecifier HealingOnBite = new()
    {
        DamageDict = new()
        {
            { "Blunt", -2 },
            { "Slash", -2 },
            { "Piercing", -2 }
        }
    };

    /// <summary>
    /// Infection warnings are shown as popups, times are in seconds.
    ///   -ve times shown to initial zombies (once timer counts from -ve to 0 the infection starts)
    ///   +ve warnings are in seconds after being bitten
    /// </summary>
    [DataField("infectionWarnings")]
    public Dictionary<int, string> InfectionWarnings = new()
    {
        {-45, "zombie-infection-warning"},
        {-30, "zombie-infection-warning"},
        {10, "zombie-infection-underway"},
        {25, "zombie-infection-underway"},
    };

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/zombie_start.ogg");

    [DataField("zombieStatusIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    public string ZombieStatusIcon = "ZombieFaction";
}
