using Content.Shared.CCVar;
using Content.Shared.Movement;
using Content.Shared.Movement.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;


namespace Content.Shared.Friction
{
    public sealed class TileFrictionController : VirtualController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedWeightlessSystem _weightless = default!;

        private float _stopSpeed;
        private float _frictionModifier;

        public override void Initialize()
        {
            var configManager = IoCManager.Resolve<IConfigurationManager>();

            configManager.OnValueChanged(CCVars.TileFrictionModifier, SetFrictionModifier, true);
            configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
        }

        private void SetStopSpeed(float value) => _stopSpeed = value;

        private void SetFrictionModifier(float value) => _frictionModifier = value;

        public override void Shutdown()
        {
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
                    _mover.UseMobMovement(body.Owner)) continue;

                var surfaceFriction = GetTileFriction(body);
                var bodyModifier = EntityManager.GetComponentOrNull<SharedTileFrictionModifier>(body.Owner)?.Modifier ?? 1.0f;
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

        [Pure]
        private float GetTileFriction(PhysicsComponent body)
        {
            if (!TryComp<TransformComponent>(body.Owner, out var xform)) return 0f;

            // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
            if (body.BodyStatus == BodyStatus.InAir ||
                _weightless.IsWeightless(body.Owner, body, xform) ||
                !_mapManager.TryGetGrid(xform.GridID, out var grid))
                return 0.0f;

            var coordinates = xform.Coordinates;

            if (!coordinates.IsValid(EntityManager)) return 0.0f;

            var tile = grid.GetTileRef(coordinates);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
