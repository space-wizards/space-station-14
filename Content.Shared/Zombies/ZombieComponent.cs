using Content.Shared.Chat.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using static Content.Shared.Humanoid.HumanoidAppearanceState;
using Content.Shared.Damage;

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
        public float MaxZombieInfectionChance = 0.50f;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinZombieInfectionChance = 0.05f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float ZombieMovementSpeedDebuff = 0.75f;

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

        /// <summary>
        /// The accent of the humanoid to restore in case of cloning
        /// </summary>
        [DataField("beforeZombifiedAccent")]
        public string BeforeZombifiedAccent = "none";

        /// <summary>
        /// The melee attack settings of the humanoid to restore in case of cloning
        /// </summary>
        [DataField("beforeZombifiedClickAnimation")]
        public string? BeforeZombifiedClickAnimation;

        [DataField("beforeZombifiedWideAnimation")]
        public string? BeforeZombifiedWideAnimation;

        [DataField("beforeZombifiedRange")]
        public float? BeforeZombifiedRange;

        [DataField("beforeZombifiedDamage")]
        public DamageSpecifier? BeforeZombifiedDamage;

        [DataField("beforeZombifiedModifierSetId")]
        public string? BeforeZombifiedModifierSetId;

        /// <summary>
        /// The bloodloss threshold of the entity to restore in case of cloning
        /// </summary>
        [DataField("beforeZombifiedBloodLossThreshold")]
        public float? BeforeZombifiedBloodLossThreshold;

        /// <summary>
        /// The cold damage threshold of the entity to restore in case of cloning
        /// </summary>
        [DataField("beforeZombifiedColdTempThreshold")]
        public DamageSpecifier? BeforeZombifiedColdTempThreshold;

        [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
        public string? EmoteSoundsId = "Zombie";

        public EmoteSoundsPrototype? EmoteSounds;
    }
}
