using System;
using Robust.Shared.Maths;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Log;
using Content.Server.Popups;
using Content.Shared.Interaction;

namespace Content.Server.Reflector
{

    public class ReflectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReflectorComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<ReflectorComponent, PreventCollideEvent>(PreventCollision);
        }

        //this needs to be moved to a ui, and changed so that it can update without changing the angle
        private void OnInteractHand(EntityUid uid, ReflectorComponent reflector, InteractHandEvent args)
        {
            reflector.Angle = (reflector.Angle + (MathHelper.Pi/8f)) % Math.Tau;

            if (EntityManager.TryGetComponent<SpriteComponent>(args.Target, out SpriteComponent? sprite))
                sprite.LayerSetRotation(1, reflector.Angle);

            Logger.DebugS("reflection", $"new r: {reflector.Angle.Degrees}");
        }

        private void PreventCollision(EntityUid uid, ReflectorComponent reflector, PreventCollideEvent args) => Reflect(uid, reflector, args);

        private void Reflect(EntityUid uid, ReflectorComponent reflector, PreventCollideEvent args,
                TransformComponent? refXform = default,
                TransformComponent? projXform = default)
        {
            if (!Resolve(uid, ref refXform)
                || !Resolve(args.BodyB.Owner, ref projXform)
                || !reflector.Whitelist.IsValid(args.BodyB.Owner))
                return;

            Vector2 rp = refXform.WorldPosition;
            Vector2 pp = projXform.WorldPosition;
            Vector2 diff = rp - pp;

            Angle r = diff.ToAngle();
            Angle aoi = r - reflector.Angle;
            aoi *= 2f;
            args.Cancel();

            Logger.DebugS("reflection",
                    $"a: {r.Degrees}" +
                    $" aoi: {aoi.Degrees}" +
                    $" R: {reflector.Angle.Degrees} " +
                    $" p: {projXform.LocalRotation.Degrees}");

            projXform.Coordinates = refXform.Coordinates.Offset(r.RotateVec(Vector2.One));

            var bolt = args.BodyB;
            /* args.BodyB.LinearVelocity = r.RotateVec(bolt.LinearVelocity); */
            args.BodyB.LinearVelocity = Vector2.Zero;

            projXform.LocalRotation += r;
        }
    }
}
