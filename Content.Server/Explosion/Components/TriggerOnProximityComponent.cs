using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

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
        [DataField("Enabled")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            set
            {

                if (value)
                {
                    Owner.EntityManager.EntitySysManager.GetEntitySystem<ProximityTriggerSystem>().AddProximityFixture(Owner.Uid, this);
                }
                else
                {
                    Owner.EntityManager.EntitySysManager.GetEntitySystem<ProximityTriggerSystem>().RemoveProximityFixture(Owner.Uid, this);
                }
                _enabled = value;

            }
            get => _enabled;
        }

    }
}
