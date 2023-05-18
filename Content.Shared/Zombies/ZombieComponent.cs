using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using static Content.Shared.Humanoid.HumanoidAppearanceState;
using Content.Shared.FixedPoint;

namespace Content.Shared.Zombies
{
    [RegisterComponent, NetworkedComponent]
    public sealed class ZombieComponent : Component
    {
        /// <summary>
        /// The coefficient of the damage reduction applied when a zombie
        /// attacks another zombie. longe name
        /// </summary>
        [ViewVariables]
        public float OtherZombieDamageCoefficient = 0.25f;

        /// <summary>
        /// The baseline infection chance you have if you are completely nude
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxZombieInfectionChance = 0.40f;

        /// <summary>
        /// Chance that this zombie be permanently killed (rolled once on crit->death transition)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ZombiePermadeathChance = 0.70f;

        /// <summary>
        /// Chance that this zombie will be healed (rolled each second when in crit or dead)
        ///   3% means you have a 60% chance after 30 secs and a 84% chance after 60.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ZombieReviveChance = 0.03f;

        /// <summary>
        /// Has this zombie stopped healing now that it's died for real?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Permadeath = false;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinZombieInfectionChance = 0.10f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ZombieMovementSpeedDebuff = 0.75f;

        /// <summary>
        /// How long it takes our bite victims to turn in seconds (max).
        ///   Will roll 25% - 100% of this on bite.
        /// </summary>
        [DataField("zombieInfectionTurnTime"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombieInfectionTurnTime = 240.0f;

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
        public readonly string ZombieRoleId = "Zombie";

        /// <summary>
        /// The suffocation threshold of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedSuffocationThreshold"), ViewVariables(VVAccess.ReadOnly)]
        public float? BeforeZombifiedSuffocationThreshold;

        /// <summary>
        /// The barotrauma immunity of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedBarotraumaImmunity"), ViewVariables(VVAccess.ReadOnly)]
        public bool? BeforeZombifiedBarotraumaImmunity;

        /// <summary>
        /// The hunger decay rate of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedHungerDecayRate"), ViewVariables(VVAccess.ReadOnly)]
        public float? BeforeZombifiedHungerDecayRate;

        /// <summary>
        /// The thirst decay rate of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedThirstDecayRate"), ViewVariables(VVAccess.ReadOnly)]
        public float? BeforeZombifiedThirstDecayRate;

        /// <summary>
        /// The accent of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedAccent"), ViewVariables(VVAccess.ReadOnly)]
        public string? BeforeZombifiedAccent;

        /// <summary>
        /// The click attack animation of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedClickAnimation"), ViewVariables(VVAccess.ReadOnly)]
        public string? BeforeZombifiedClickAnimation;

        /// <summary>
        /// The wide attack animation of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedWideAnimation"), ViewVariables(VVAccess.ReadOnly)]
        public string? BeforeZombifiedWideAnimation;

        /// <summary>
        /// The melee range of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedRange"), ViewVariables(VVAccess.ReadOnly)]
        public float? BeforeZombifiedRange;

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

        /// <summary>
        /// The melee damage specifier of the humanoid to restore in case of cloning
        /// </summary>
        [DataField("beforeZombifiedMeleeDamageSpecifier")]
        public DamageSpecifier? BeforeZombifiedMeleeDamageSpecifier;

        /// <summary>
        /// The DamageModifierSetId of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedDamageModifierSetId")]
        public string? BeforeZombifiedDamageModifierSetId;

        /// <summary>
        /// The bloodloss threshold of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedBloodlossThreshold")]
        public float? BeforeZombifiedBloodlossThreshold;

        /// <summary>
        /// The cold damage specifier of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedColdDamage")]
        public DamageSpecifier? BeforeZombifiedColdDamage;

        /// <summary>
        /// The cold damage specifier of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedPacifist")]
        public bool BeforeZombifiedPacifist = false;

        /// <summary>
        /// The amount of hands of the zombie to restore in case of unzombification
        /// </summary>
        [DataField("beforeZombifiedHandCount")]
        public int BeforeZombifiedHandCount = 0;

        /// <summary>
        /// The dictionary with humanoid zombie damage values
        /// </summary>
        [DataField("humanoidZombieDamageValues")]
        public Dictionary<string, FixedPoint2> HumanoidZombieDamageValues = new Dictionary<string, FixedPoint2>()
        {
            {"Slash", 13},
            {"Piercing", 7},
            {"Structural", 10}
        };

        [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
        public string? EmoteSoundsId = "Zombie";

        public EmoteSoundsPrototype? EmoteSounds;

        // Heal on tick
        [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;

        [DataField("damage")] public DamageSpecifier Damage = new()
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
    }
}
