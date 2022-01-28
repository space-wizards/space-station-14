using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Movement.EntitySystems
{
    public sealed class SharedMobMoverSystem : EntitySystem
    {
        private bool _pushingEnabled;

        public override void Initialize()
        {
            base.Initialize();
            Get<SharedPhysicsSystem>().KinematicControllerCollision += HandleCollisionMessage;
            IoCManager.Resolve<IConfigurationManager>().OnValueChanged(CCVars.MobPushing, SetPushing, true);
        }

        private void SetPushing(bool value)
        {
            _pushingEnabled = value;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            IoCManager.Resolve<IConfigurationManager>().UnsubValueChanged(CCVars.MobPushing, SetPushing);
            Get<SharedPhysicsSystem>().KinematicControllerCollision -= HandleCollisionMessage;
        }

        /// <summary>
        ///     Fake pushing for player collisions.
        /// </summary>
        private void HandleCollisionMessage(Fixture ourFixture, Fixture otherFixture, float frameTime, Vector2 worldNormal)
        {
            if (!_pushingEnabled) return;

            var otherBody = otherFixture.Body;

            if (otherBody.BodyType != BodyType.Dynamic || !otherFixture.Hard) return;

            if (!EntityManager.TryGetComponent(ourFixture.Body.Owner, out IMobMoverComponent? mobMover) || worldNormal == Vector2.Zero) return;

            otherBody.ApplyLinearImpulse(-worldNormal * mobMover.PushStrength * frameTime);
        }
    }
}
