using Content.Server.Shuttles.Systems;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    [Access(typeof(ThrusterSystem))]
    public sealed class ThrusterComponent : Component
    {
        /// <summary>
        /// Whether the thruster has been force to be enabled / disabled (e.g. VV, interaction, etc.)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;

                var system = EntitySystem.Get<ThrusterSystem>();

                if (!_enabled)
                {
                    system.DisableThruster(Owner, this);
                }
                else if (system.CanEnable(Owner, this))
                {
                    system.EnableThruster(Owner, this);
                }
            }
        }

        private bool _enabled = true;

        /// <summary>
        /// This determines whether the thruster is actually enabled for the purposes of thrust
        /// </summary>
        public bool IsOn;

        [ViewVariables(VVAccess.ReadWrite)]
        public float Thrust;

        [DataField("baseThrust"), ViewVariables(VVAccess.ReadWrite)]
        public float BaseThrust = 750f;

        [DataField("thrusterType")]
        public ThrusterType Type = ThrusterType.Linear;

        [DataField("burnShape")] public List<Vector2> BurnPoly = new()
        {
            new Vector2(-0.4f, 0.5f),
            new Vector2(-0.1f, 1.2f),
            new Vector2(0.1f, 1.2f),
            new Vector2(0.4f, 0.5f)
        };

        /// <summary>
        /// How much damage is done per second to anything colliding with our thrust.
        /// </summary>
        [DataField("damage")] public DamageSpecifier? Damage = new();

        [DataField("requireSpace")]
        public bool RequireSpace = true;

        // Used for burns

        public List<EntityUid> Colliding = new();

        public bool Firing = false;

        [DataField("machinePartThrust", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartThrust = "Laser";

        [DataField("partRatingThrustMultiplier")]
        public float PartRatingThrustMultiplier = 1.5f;
    }

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
