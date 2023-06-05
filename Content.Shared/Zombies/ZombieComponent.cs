using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using static Content.Shared.Humanoid.HumanoidAppearanceState;

namespace Content.Shared.Zombies
{
    [DataDefinition]
    public sealed class ZombieSettings
    {
        /// <summary>
        /// Fraction of damage subtracted when hitting another zombie
        /// </summary>
        [DataField("otherZombieDamageCoefficient"), ViewVariables]
        public float OtherZombieDamageCoefficient = 1.0f;

        /// <summary>
        /// Chance that this zombie be permanently killed (rolled once on crit->death transition)
        /// </summary>
        [DataField("zombiePermadeathChance"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombiePermadeathChance = 0.80f;

        /// <summary>
        /// Chance that this zombie be permanently killed (rolled once on alive->crit transition)
        /// </summary>
        [DataField("zombieCritDeathChance"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombieCritDeathChance = 0.40f;

        /// <summary>
        /// How many seconds it takes for a zombie to revive (min)
        /// </summary>
        [DataField("zombieReviveTime"), ViewVariables(VVAccess.ReadWrite)]
        public int ZombieReviveTime = 10;

        /// <summary>
        /// How many seconds it takes for a zombie to revive (max)
        /// </summary>
        [DataField("zombieReviveTimeMax"), ViewVariables(VVAccess.ReadWrite)]
        public int ZombieReviveTimeMax = 60;

        /// <summary>
        /// The baseline infection chance you have if you are completely nude
        /// </summary>
        [DataField("maxZombieInfectionChance"), ViewVariables(VVAccess.ReadWrite)]
        public float MaxZombieInfectionChance = 0.30f;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        [DataField("minZombieInfectionChance"), ViewVariables(VVAccess.ReadWrite)]
        public float MinZombieInfectionChance = 0.05f;

        [DataField("zombieMovementSpeedDebuff"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombieMovementSpeedDebuff = 0.70f;

        /// <summary>
        /// How long it takes our bite victims to turn in seconds (max).
        ///   Will roll 25% - 100% of this on bite.
        /// </summary>
        [DataField("zombieInfectionTurnTime"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombieInfectionTurnTime = 480.0f;
        /// <summary>
        /// Minimum time a zombie victim will lie dead before rising as a zombie.
        /// </summary>
        [DataField("zombieDeadMinTurnTime"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombieDeadMinTurnTime = 10.0f;

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
        /// The attack range of the zombie
        /// </summary>
        public float MeleeRange = 1.5f;

        /// <summary>
        /// The role prototype of the zombie antag role
        /// </summary>
        [DataField("zombieRoleId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
        public readonly string ZombieRoleId = "Zombie";

        [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
        public string? EmoteSoundsId = "Zombie";

        public EmoteSoundsPrototype? EmoteSounds;

        /// <summary>
        /// Healing each second
        /// </summary>
        [DataField("healing")] public DamageSpecifier Healing = new()
        {
            DamageDict = new ()
            {
                { "Blunt", -0.4 },
                { "Slash", -0.2 },
                { "Piercing", -0.2 },
                { "Heat", -0.2 },
                { "Cold", -0.2 },
                { "Shock", -0.2 },
            }
        };

        /// <summary>
        /// How much the virus hurts you (base, scales rapidly)
        /// </summary>
        [DataField("virusDamage")] public DamageSpecifier VirusDamage = new()
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
        [DataField("attackDamage")] public DamageSpecifier AttackDamage = new()
        {
            DamageDict = new ()
            {
                { "Slash", 13 },
                { "Piercing", 7 },
                { "Structural", 10 },
            }
        };

        /// <summary>
        /// Damage inflicted on the zombie whenever they attack something that is not organic.
        /// </summary>
        [DataField("biteMetalDamage")] public DamageSpecifier BiteMetalDamage = new()
        {
            DamageDict = new ()
            {
                { "Blunt", 5 },
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
        /// Number of seconds that a typical infection will last before the player is totally overwhelmed with damage and
        ///   dies.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("maxInfectionLength")]
        public float MaxInfectionLength = 120f;

    }

    [DataDefinition]
    public sealed class ZombieFamily
    {
        /// <summary>
        /// Generation of this zombie (patient zero is 0, their victims are 1, etc)
        /// </summary>
        [DataField("generation"), ViewVariables(VVAccess.ReadOnly)]
        public int Generation = default!;

        /// <summary>
        /// If this zombie is not patient 0, this is the player who infected this zombie.
        /// </summary>
        [DataField("infector"), ViewVariables(VVAccess.ReadOnly)]
        public EntityUid Infector = EntityUid.Invalid;

        /// <summary>
        /// When created by a ZombieRuleComponent, this points to the entity which unleashed this zombie horde.
        /// </summary>
        [DataField("rules"), ViewVariables(VVAccess.ReadOnly)]
        public EntityUid Rules = EntityUid.Invalid;

    }

    [RegisterComponent, NetworkedComponent]
    public sealed class ZombieComponent : Component
    {
        /// <summary>
        /// Our settings (describes what the zombie can do)
        /// </summary>
        [DataField("settings"), ViewVariables(VVAccess.ReadOnly)]
        public ZombieSettings Settings = default!;

        /// <summary>
        /// Settings for any victims we might have (if they are not the same as our settings)
        /// </summary>
        [DataField("victimSettings"), ViewVariables(VVAccess.ReadOnly)]
        public ZombieSettings? VictimSettings;

        /// <summary>
        /// Our family (describes how we became a zombie and where the rules are)
        /// </summary>
        [DataField("family"), ViewVariables(VVAccess.ReadOnly)]
        public ZombieFamily Family = default!;

        public float OtherZombieDamageCoefficient => Settings.OtherZombieDamageCoefficient;


        /// <summary>
        /// Chance that this zombie be permanently killed (rolled once on crit->death transition)
        /// </summary>
        public float ZombieCritDeathChance => Settings.ZombieCritDeathChance;

        /// <summary>
        /// Chance that this zombie be permanently killed (rolled once on crit->death transition)
        /// </summary>
        public float ZombiePermadeathChance => Settings.ZombiePermadeathChance;

        /// <summary>
        /// Has this zombie stopped healing now that it's died for real?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Permadeath;

        /// <summary>
        /// Seconds until this zombie revives
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int ZombieRevivalSeconds;

        /// <summary>
        /// The baseline infection chance you have if you are completely nude
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float MaxZombieInfectionChance => Settings.MaxZombieInfectionChance;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float MinZombieInfectionChance => Settings.MinZombieInfectionChance;

        [ViewVariables(VVAccess.ReadOnly)]
        public float ZombieMovementSpeedDebuff => Settings.ZombieMovementSpeedDebuff;

        /// <summary>
        /// How long it takes our bite victims to turn in seconds (max).
        ///   Will roll 25% - 100% of this on bite.
        /// </summary>

        [ViewVariables(VVAccess.ReadOnly)]
        public float ZombieInfectionTurnTime => Settings.ZombieInfectionTurnTime;

        /// <summary>
        /// Healing each second
        /// </summary>
        public DamageSpecifier Healing => Settings.Healing;

        /// <summary>
        /// The EntityName of the humanoid to restore in case of cloning
        /// </summary>
        [DataField("beforeZombifiedEntityName"), ViewVariables(VVAccess.ReadOnly)]
        public string BeforeZombifiedEntityName = String.Empty;

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

        [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;


    }
}
