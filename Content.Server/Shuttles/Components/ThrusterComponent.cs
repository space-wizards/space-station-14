using System.Collections.Generic;
using Content.Server.Shuttles.EntitySystems;
using Content.Shared.Damage;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    [Friend(typeof(ThrusterSystem))]
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

        [ViewVariables]
        [DataField("impulse")]
        public float Impulse = 450f;

        [ViewVariables]
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
        [ViewVariables] [DataField("damage")] public DamageSpecifier? Damage = new();

        // Used for burns

        public List<EntityUid> Colliding = new();

        public bool Firing = false;
    }

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
