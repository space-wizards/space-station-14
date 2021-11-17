using System.Collections.Generic;
using Content.Server.Shuttles.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    [Friend(typeof(ThrusterSystem))]
    public sealed class ThrusterComponent : Component
    {
        public override string Name => "Thruster";

        /// <summary>
        /// Whether the thruster has been force to be enabled / disable (e.g. VV, interaction, etc.)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool EnabledVV
        {
            get => _enabledVv;
            set
            {
                if (_enabledVv == value) return;
                _enabledVv = value;

                var system = EntitySystem.Get<ThrusterSystem>();

                if (!_enabledVv)
                {
                    system.DisableThruster(OwnerUid, this);
                }
                else if (system.CanEnable(OwnerUid, this))
                {
                    system.EnableThruster(OwnerUid, this);
                }
            }
        }

        private bool _enabledVv = true;

        /// <summary>
        /// This determines whether the thruster is actually enabled for the purposes of thrust
        /// </summary>
        public bool Enabled;

        [ViewVariables]
        [DataField("impulse")]
        public float Impulse = 3f;

        [ViewVariables]
        [DataField("thrusterType")]
        public ThrusterType Type = ThrusterType.Linear;

        // Used for burns

        public List<EntityUid> Colliding = new();

        public bool Firing = false;
    }

    public enum ThrusterType
    {
        Invalid = 0,
        Linear = 1 << 0,
        // Angular meaning rotational.
        Angular = 1 << 1,
    }
}
