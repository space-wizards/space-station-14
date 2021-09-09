using Content.Server.Construction.Components;
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
        public const string FixtureID  = "trigger-on-proximity-fixture";

        [DataField("shape", required: true)]
        public IPhysShape Shape { get; set; } = default!;

        [DataField("enabled")]
        public bool Enabled = true;
        
        [ViewVariables(VVAccess.ReadWrite)]
        public bool EnabledVV
        {
            set
            {
                EntitySystem.Get<TriggerSystem>().SetProximityFixture(Owner.Uid, this, value && Owner.Transform.Anchored);
                Enabled = value;
            }
            get => Enabled;
        }

        [DataField("cooldown")]
        public int Cooldown { get; set; } = 2;

        [DataField("repeating")]
        internal bool Repeating = true;

    }
}
