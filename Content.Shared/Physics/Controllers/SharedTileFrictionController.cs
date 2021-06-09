using System;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using JetBrains.Annotations;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

#nullable enable

namespace Content.Shared.Physics.Controllers
{
    public sealed class SharedTileFrictionController : VirtualController
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        private float _stopSpeed;

        private float _frictionModifier;

        public override void Initialize()
        {
            base.Initialize();
            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();

            _frictionModifier = _configManager.GetCVar(CCVars.TileFrictionModifier);
            _configManager.OnValueChanged(CCVars.TileFrictionModifier, value => _frictionModifier = value);

            _stopSpeed = _configManager.GetCVar(CCVars.StopSpeed);
            _configManager.OnValueChanged(CCVars.StopSpeed, value => _stopSpeed = value);
        }

        public override void UpdateBeforeMapSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeMapSolve(prediction, map, frameTime);

            foreach (var body in map.AwakeBodies)
            {
                // Only apply friction when it's not a mob (or the mob doesn't have control)
                if (prediction && !body.Predict ||
                    body.BodyStatus == BodyStatus.InAir ||
                    SharedMoverController.UseMobMovement(_broadPhaseSystem, body, _mapManager)) continue;

                var surfaceFriction = GetTileFriction(body);
                var bodyModifier = body.Owner.GetComponentOrNull<SharedTileFrictionModifier>()?.Modifier ?? 1.0f;
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
            var transform = body.Owner.Transform;
            var coords = transform.Coordinates;

            // TODO: Make IsWeightless event-based; we already have grid traversals tracked so just raise events
            if (body.BodyStatus == BodyStatus.InAir ||
                body.Owner.IsWeightless(body, coords, _mapManager) ||
                !_mapManager.TryGetGrid(transform.GridID, out var grid))
                return 0.0f;

            if (!coords.IsValid(EntityManager)) return 0.0f;

            var tile = grid.GetTileRef(coords);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
