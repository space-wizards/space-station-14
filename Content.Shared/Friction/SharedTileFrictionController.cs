using Content.Shared.CCVar;
using Content.Shared.Gravity.EntitySystems;
using Content.Shared.Movement;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Friction
{
    public abstract class SharedTileFrictionController : VirtualController
    {
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedWeightlessSystem _weightSystem = default!;

        protected SharedMoverController Mover = default!;

        private float _stopSpeed;
        private float _frictionModifier;

        public override void Initialize()
        {
            base.Initialize();

            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.OnValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier, true);
            configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
        }

        private void SetStopSpeed(float value) => _stopSpeed = value;

        private void SetFrictionModifier(float value) => _frictionModifier = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.UnsubValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier);
            configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        }

        public override void UpdateBeforeMapSolve(bool prediction, SharedPhysicsMapComponent mapComponent, float frameTime)
        {
            base.UpdateBeforeMapSolve(prediction, mapComponent, frameTime);

            foreach (var body in mapComponent.AwakeBodies)
            {
                // Only apply friction when it's not a mob (or the mob doesn't have control)
                if (body.Deleted ||
                    prediction && !body.Predict ||
                    body.BodyStatus == BodyStatus.InAir ||
                    Mover.UseMobMovement(body.Owner)) continue;

                var surfaceFriction = GetTileFriction(body);
                var bodyModifier = IoCManager.Resolve<IEntityManager>().GetComponentOrNull<TileFrictionModifierComponent>(body.Owner)?.Modifier ?? 1.0f;
                var friction = _frictionModifier * surfaceFriction * bodyModifier;

                ReduceLinearVelocity(prediction, body, friction, frameTime);
                ReduceAngularVelocity(prediction, body, friction, frameTime);
            }
        }

        private void ReduceLinearVelocity(bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = body.LinearVelocity.Length;

            if (speed <= 0.0f) return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            body.LinearVelocity *= newSpeed;
        }

        private void ReduceAngularVelocity(bool prediction, PhysicsComponent body, float friction, float frameTime)
        {
            var speed = MathF.Abs(body.AngularVelocity);

            if (speed <= 0.0f) return;

            // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
            var drop = 0.0f;
            float control;

            if (friction > 0.0f)
            {
                // TBH I can't really tell if this makes a difference.
                if (!prediction)
                {
                    control = speed < _stopSpeed ? _stopSpeed : speed;
                }
                else
                {
                    control = speed;
                }

                drop += control * friction * frameTime;
            }

            var newSpeed = MathF.Max(0.0f, speed - drop);

            newSpeed /= speed;
            body.AngularVelocity *= newSpeed;
        }

        private float GetTileFriction(PhysicsComponent body)
        {
            if (body.BodyStatus == BodyStatus.InAir ||
                _weightSystem.IsWeightless(body.Owner, out var tileRef, body) ||
                tileRef == null)
                return 0.0f;

            return _tileDefinitionManager[tileRef.Value.Tile.TypeId].Friction;
        }
    }
}
