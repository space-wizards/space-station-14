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
        /// Chance that this zombie be permanently killed (rolled once on crit->death transition)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ZombiePermadeathChance = 0.80f;

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
        /// The baseline infection chance you have if you are completely nude
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxZombieInfectionChance = 0.30f;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinZombieInfectionChance = 0.05f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ZombieMovementSpeedDebuff = 0.70f;

        /// <summary>
        /// How long it takes our bite victims to turn in seconds (max).
        ///   Will roll 25% - 100% of this on bite.
        /// </summary>
        [DataField("zombieInfectionTurnTime"), ViewVariables(VVAccess.ReadWrite)]
        public float ZombieInfectionTurnTime = 480.0f;

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

        [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
        public string? EmoteSoundsId = "Zombie";

        public EmoteSoundsPrototype? EmoteSounds;

        [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;

        /// <summary>
        /// Healing each second
        /// </summary>
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
