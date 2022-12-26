using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Projectiles
{
    [RegisterComponent, NetworkedComponent]
    public sealed class ProjectileComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("impactEffect", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? ImpactEffect;

        public EntityUid Shooter { get; set; }

        public bool IgnoreShooter = true;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("deleteOnCollide")]
        public bool DeleteOnCollide { get; } = true;

        [DataField("ignoreResistances")]
        public bool IgnoreResistances { get; } = false;

        // Get that juicy FPS hit sound
        [DataField("soundHit")] public SoundSpecifier? SoundHit;

        [DataField("soundForce")]
        public bool ForceSound = false;

        public bool DamagedEntity;
    }
}
