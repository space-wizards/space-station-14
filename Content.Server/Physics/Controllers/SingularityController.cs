#nullable enable
using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.Components.Singularity;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Random;

namespace Content.Server.Physics.Controllers
{
    internal sealed class SingularityController : VirtualController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private float _pullAccumulator;
        private float _moveAccumulator;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            _moveAccumulator += frameTime;
            _pullAccumulator += frameTime;

            while (_pullAccumulator > 0.5f)
            {
                _pullAccumulator -= 0.5f;

                foreach (var singularity in ComponentManager.EntityQuery<ServerSingularityComponent>())
                {
                    // TODO: Use colliders instead probably yada yada
                    PullEntities(singularity);
                    // Yeah look the collision with station wasn't working and I'm 15k lines in and not debugging this shit
                    DestroyTiles(singularity);
                }
            }

            while (_moveAccumulator > 1.0f)
            {
                _moveAccumulator -= 1.0f;

                foreach (var (singularity, physics) in ComponentManager.EntityQuery<ServerSingularityComponent, PhysicsComponent>())
                {
                    if (singularity.Owner.HasComponent<BasicActorComponent>()) continue;

                    // TODO: Need to essentially use a push vector in a random direction for us PLUS
                    // Any entity colliding with our larger circlebox needs to have an impulse applied to itself.
                    physics.BodyStatus = BodyStatus.InAir;
                    MoveSingulo(singularity, physics);
                }
            }
        }

        private void MoveSingulo(ServerSingularityComponent singularity, PhysicsComponent physics)
        {
            if (singularity.Level <= 1) return;
            // TODO: Could try gradual changes instead but for now just try to replicate

            var pushVector = new Vector2(_robustRandom.Next(-10, 10), _robustRandom.Next(-10, 10));

            if (pushVector == Vector2.Zero) return;

            physics.LinearVelocity = Vector2.Zero;
            physics.LinearVelocity = pushVector.Normalized * 2;
        }

        private void PullEntities(ServerSingularityComponent component)
        {
            var singularityCoords = component.Owner.Transform.Coordinates;
            // TODO: Maybe if we have named fixtures needs to pull out the outer circle collider (inner will be for deleting).
            var entitiesToPull = EntityManager.GetEntitiesInRange(singularityCoords, component.Level * 10);
            foreach (var entity in entitiesToPull)
            {
                if (!entity.TryGetComponent<PhysicsComponent>(out var collidableComponent) || collidableComponent.BodyType == BodyType.Static) continue;
                if (entity.HasComponent<GhostComponent>()) continue;
                if (singularityCoords.EntityId != entity.Transform.Coordinates.EntityId) continue;
                var vec = (singularityCoords - entity.Transform.Coordinates).Position;
                if (vec == Vector2.Zero) continue;

                var speed = 10 / vec.Length * component.Level;

                collidableComponent.ApplyLinearImpulse(vec.Normalized * speed);
            }
        }

        private void DestroyTiles(ServerSingularityComponent component)
        {
            if (!component.Owner.TryGetComponent(out PhysicsComponent? physicsComponent)) return;
            var worldBox = physicsComponent.GetWorldAABB();

            foreach (var grid in _mapManager.FindGridsIntersecting(component.Owner.Transform.MapID, worldBox))
            {
                foreach (var tile in grid.GetTilesIntersecting(worldBox))
                {
                    grid.SetTile(tile.GridIndices, Tile.Empty);
                }
            }
        }
    }
}
