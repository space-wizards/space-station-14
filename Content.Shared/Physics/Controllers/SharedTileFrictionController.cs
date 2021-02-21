using System;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

#nullable enable

namespace Content.Shared.Physics.Controllers
{
    public sealed class SharedTileFrictionController : AetherController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        private const float StopSpeed = 0.01f;

        public override void Initialize()
        {
            base.Initialize();
            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
        }

        public override void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, map, frameTime);

            foreach (var body in map.AwakeBodies)
            {
                var speed = body.LinearVelocity.Length;

                if (speed <= 0.0f || body.Status == BodyStatus.InAir) continue;

                // This is the *actual* amount that speed will drop by, we just do some multiplication around it to be easier.
                var drop = 0.0f;
                float control;

                // Only apply friction when the player has no control
                var useMobMovement = body.Owner.HasComponent<IMobStateComponent>() &&
                                     ActionBlockerSystem.CanMove(body.Owner) &&
                                     (!body.Owner.IsWeightless() ||
                                      body.Owner.TryGetComponent(out IMoverComponent? mover) &&
                                      SharedMobMoverController.IsAroundCollider(_broadPhaseSystem, body.Owner.Transform, mover, body));

                if (useMobMovement) continue;

                var surfaceFriction = GetTileFriction(body);
                // TODO: Make cvar
                var frictionModifier = 10.0f;
                var friction = frictionModifier * surfaceFriction;

                if (friction > 0.0f)
                {
                    // TBH I can't really tell if this makes a difference, player movement is fucking hard.
                    if (!prediction)
                    {
                        control = speed < StopSpeed ? StopSpeed : speed;
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
        }

        [Pure]
        private float GetTileFriction(IPhysicsComponent body)
        {
            if (!body.OnGround)
                return 0.0f;

            var transform = body.Owner.Transform;
            var coords = transform.Coordinates;

            var grid = _mapManager.GetGrid(coords.GetGridId(body.Owner.EntityManager));
            var tile = grid.GetTileRef(coords);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
