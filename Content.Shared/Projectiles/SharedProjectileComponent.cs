using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Projectiles
{
    [NetworkedComponent, Access(typeof(SharedProjectileSystem))]
    public abstract class SharedProjectileComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("impactEffect", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? ImpactEffect;

        private bool _ignoreShooter = true;
        public EntityUid Shooter { get; set; }

        public bool IgnoreShooter
        {
            get => _ignoreShooter;
            set
            {
                if (_ignoreShooter == value) return;

                _ignoreShooter = value;
                Dirty();
            }
        }
    }
}
