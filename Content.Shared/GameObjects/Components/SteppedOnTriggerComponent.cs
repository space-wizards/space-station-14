using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components
{
    [RegisterComponent]
    public sealed class SteppedOnTriggerComponent : Component
    {
        public override string Name => "SteppedOnTrigger";

        /// <summary>
        /// Radius of the circleshape for the trigger.
        /// </summary>
        [ViewVariables]
        [DataField("triggerScale")]
        private float _triggerRadius = 0.3f;

        public const string SteppedOnFixture = "stepped-on";

        protected override void Startup()
        {
            base.Startup();
            if (!Owner.TryGetComponent(out PhysicsComponent? body))
            {
                Logger.Error($"SteppedOnTrigger added to {Owner} but it doesn't have a physics component!");
                return;
            }

            if (body.GetFixture(SteppedOnFixture) != null)
            {
                // Probably means server replicated it so no action needed?
                // Could potentially have a reserved list of fixture names that yaml can't use to avoid any potential issues.
                return;
            }

            // If they already have a fixture we'll scale that down and use that.
            var fixture = GetFixture(body);
            body.AddFixture(fixture);
        }

        private Fixture GetFixture(PhysicsComponent body)
        {
            return new(body, new PhysShapeCircle {Radius = _triggerRadius})
            {
                Name = SteppedOnFixture,
                Hard = false,
                CollisionLayer = (int) CollisionGroup.MobImpassable
            };
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            if (!Owner.TryGetComponent(out PhysicsComponent? body)) return;

            var fixture = body.GetFixture(SteppedOnFixture);

            if (fixture == null) return;
            body.RemoveFixture(fixture);
        }
    }
}
