using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Content.Server.Projectiles;

namespace Content.Server.Reflector
{

    public class ReflectorSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();

            Get<SharedPhysicsSystem>().KinematicControllerCollision += HandleCollisionMessage;
            SubscribeLocalEvent<ReflectorComponent, ProjectileAbsorbedEvent>(PreventAbsorption);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Get<SharedPhysicsSystem>().KinematicControllerCollision -= HandleCollisionMessage;
        }

        private void PreventAbsorption(EntityUid uid, ReflectorComponent reflector, ProjectileAbsorbedEvent args)
        {
            if (reflector.Whitelist.IsValid(args.Projectile.Owner))
                args.Cancel();
        }

        private void HandleCollisionMessage(Fixture ourFixture, Fixture otherFixture, float frameTime, Vector2 worldNormal) =>
             DoCollide(ourFixture, otherFixture, frameTime, worldNormal);

        private bool DoCollide(Fixture us, Fixture them, float frameTime, Vector2 worldNormal)
        {
            ReflectorComponent? reflector = default;
            TransformComponent? refXform = default,
                projXform = default;

            var reflectorUid = us.Body.Owner;
            var otherUid = them.Body.Owner;

            if (!Resolve(reflectorUid, ref reflector, ref refXform, logMissing: false)
                || !Resolve(otherUid, ref projXform, logMissing: false))
                return false;

            if (!reflector.Whitelist.IsValid(otherUid))
                return true;

            var (projPos, projRot, projInv) = projXform.GetWorldPositionRotationInvMatrix();
            var reflection = projPos - (worldNormal * (Vector2.Dot(projPos, worldNormal) * 2));

            them.Body.LinearVelocity = projInv.Transform(reflection);
            projXform.LocalRotation = reflection.ToAngle();

            return true;
        }
    }
}
