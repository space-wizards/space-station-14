using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class TriggerOnProximityComponent : Component
    {
        public override string Name => "TriggerOnProximity";
        public TimeSpan LastTrigger = TimeSpan.Zero;
        public string ProximityFixture { get; } = "proximity-fixture";

        [DataField("shape", required: true)]
        public IPhysShape Shape { get; set; } = default!;

        private bool _enabled;
        [DataField("enabled")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            set
            {
                EntitySystem.Get<TriggerSystem>().SetProximityFixture(Owner.Uid, this, value);
                _enabled = value;
            }
            get => _enabled;
        }

        [DataField("cooldown", required: true)]
        public int Cooldown { get; set; } = 2;

    }
}
