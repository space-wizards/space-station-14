using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using static Content.Server.Explosion.ProximityTriggerSystem;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class TriggerOnProximityComponent : Component
    {
        public override string Name => "TriggerOnProximity";

        public string ProximityFixture { get; } = "proximity-fixture";

        [DataField("shape", required: true)]
        public IPhysShape Shape { get; set; } = default!;

        private bool _enabled;
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            set
            {

                if (value)
                {
                    AlterProximityFixtureEvent fixtureEvent = new(false);
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, fixtureEvent);
                }
                else
                {
                    AlterProximityFixtureEvent fixtureEvent = new(true);
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, fixtureEvent);
                }
                _enabled = value;
                
            }
            get { return _enabled; }
        }

    }
}
