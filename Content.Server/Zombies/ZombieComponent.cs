using Content.Shared.Roles;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Zombies
{
    [RegisterComponent]
    public sealed class ZombieComponent : Component
    {
        /// <summary>
        /// The coefficient of the damage reduction applied when a zombie
        /// attacks another zombie. longe name
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float OtherZombieDamageCoefficient = 0.5f;

        /// <summary>
        /// The baseline infection chance you have if you are completely nude
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxZombieInfectionChance = 0.75f;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinZombieInfectionChance = 0.1f;

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
        /// The attack arc of the zombie
        /// </summary>
        [DataField("attackArc", customTypeSerializer: typeof(PrototypeIdSerializer<MeleeWeaponAnimationPrototype>))]
        public string AttackArc = "claw";

        /// <summary>
        /// The role prototype of the zombie antag role
        /// </summary>
        [ViewVariables, DataField("zombieRoldId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
        public readonly string ZombieRoleId = "Zombie";

        [ViewVariables(VVAccess.ReadWrite)]
        public float GroanChance = 0.1f;

        [ViewVariables]
        public float Accumulator = 0f;

        [ViewVariables]
        public float LastDamageGroanAccumulator = 0f;
    }
}
