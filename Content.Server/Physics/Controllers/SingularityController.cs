using Content.Server.Ghost.Components;
using Content.Server.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
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
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        private float _pullAccumulator;
        private float _moveAccumulator;

        private const float GravityCooldown = 0.5f;
        private const float MoveCooldown = 3.0f;

        /// <summary>
        /// How much energy the singulo gains from destroying a tile.
        /// </summary>
        private const int TileEnergyGain = 1;

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            _moveAccumulator += frameTime;
            _pullAccumulator += frameTime;

            while (_pullAccumulator > GravityCooldown)
            {
                _pullAccumulator -= GravityCooldown;

                foreach (var singularity in ComponentManager.EntityQuery<ServerSingularityComponent>())
                {
                    var worldPos = singularity.Owner.Transform.WorldPosition;
                    PullEntities(singularity, worldPos);
                    DestroyTiles(singularity, worldPos);
                }
            }

            while (_moveAccumulator > MoveCooldown)
            {
                _moveAccumulator -= MoveCooldown;

                foreach (var (singularity, physics) in ComponentManager.EntityQuery<ServerSingularityComponent, PhysicsComponent>())
                {
                    if (singularity.Owner.HasComponent<ActorComponent>()) continue;

                    MoveSingulo(singularity, physics);
                }
            }
        }

        private float PullRange(ServerSingularityComponent component)
        {
            // Level 6 is normally 15 range but that's yuge.
            return 2 + component.Level * 2;
        }

        private float DestroyTileRange(ServerSingularityComponent component)
        {
            return component.Level - 0.5f;
        }

        private void MoveSingulo(ServerSingularityComponent singularity, PhysicsComponent physics)
        {
            // To prevent getting stuck, ServerSingularityComponent will zero the velocity of a singularity when it goes to a level <= 1 (see here).
            if (singularity.Level <= 1) return;
            // TODO: Could try gradual changes instead but for now just try to replicate

            var pushVector = new Vector2(_robustRandom.Next(-10, 10), _robustRandom.Next(-10, 10));

            if (pushVector == Vector2.Zero) return;

            // Need to reset its velocity entirely. Probably look better with like a slerped version but future problem.
            physics.LinearVelocity = pushVector.Normalized * singularity.Level;
        }

        private void PullEntities(ServerSingularityComponent component, Vector2 worldPos)
        {
            // TODO: When we split up dynamic and static trees we might be able to make items always on the broadphase
            // in which case we can just query dynamictree directly for brrt
            var pullRange = PullRange(component);
            var destroyRange = DestroyTileRange(component);

            foreach (var entity in _lookup.GetEntitiesInRange(component.Owner.Transform.MapID, worldPos, pullRange))
            {
                if (entity == component.Owner ||
                    !entity.TryGetComponent<PhysicsComponent>(out var collidableComponent) ||
                    collidableComponent.BodyType == BodyType.Static ||
                    entity.HasComponent<GhostComponent>() ||
                    entity.HasComponent<IMapGridComponent>() ||
                    entity.HasComponent<MapComponent>() ||
                    entity.IsInContainer()) continue;

                var vec = (worldPos - entity.Transform.WorldPosition);

                if (vec.Length < destroyRange - 0.01f) continue;

                var speed = vec.Length * component.Level * 10;

                // Because tile friction is so high we'll just multiply by mass so stuff like closets can even move.
                collidableComponent.ApplyLinearImpulse(vec.Normalized * speed);
            }
        }

        /// <summary>
        /// Destroy any grid tiles within the relevant Level range.
        /// </summary>
        private void DestroyTiles(ServerSingularityComponent component, Vector2 worldPos)
        {
            var radius = DestroyTileRange(component);

            var circle = new Circle(worldPos, radius);
            var box = new Box2(worldPos - radius, worldPos + radius);

            foreach (var grid in _mapManager.FindGridsIntersecting(component.Owner.Transform.MapID, box))
            {
                foreach (var tile in grid.GetTilesIntersecting(circle))
                {
                    if (tile.Tile.IsEmpty) continue;
                    grid.SetTile(tile.GridIndices, Tile.Empty);
                    component.Energy += TileEnergyGain;
                }
            }
        }
    }
}
