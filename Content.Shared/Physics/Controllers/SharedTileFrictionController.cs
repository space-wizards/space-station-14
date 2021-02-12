using System;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

#nullable enable

namespace Content.Shared.Physics.Controllers
{
    public sealed class SharedTileFrictionController : AetherController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private const float StopSpeed = 0.5f;

        public override void UpdateBeforeSolve(PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(map, frameTime);

            foreach (var body in map.AwakeBodies)
            {
                var vel = body.LinearVelocity;
                var speed = vel.Length;

                var drop = 0.0f;
                float control;

                var surfaceFriction = GetTileFriction(body);

                // TODO: Make const

                var frictionModifier = body.Owner.HasComponent<IBody>() && ActionBlockerSystem.CanMove(body.Owner) ? 60.0f : 10.0f;

                var friction = frictionModifier * surfaceFriction;

                if (friction > 0.0f)
                {
                    control = speed < StopSpeed ? StopSpeed : speed;
                    drop += control * friction * frameTime;
                }

                var newSpeed = MathF.Max(0.0f, speed - drop);

                if (MathHelper.CloseTo(newSpeed, speed)) continue;

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
