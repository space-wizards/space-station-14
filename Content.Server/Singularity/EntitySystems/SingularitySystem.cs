using Content.Server.Ghost.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public class SingularitySystem : SharedSingularitySystem
    {
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <summary>
        /// How much energy the singulo gains from destroying a tile.
        /// </summary>
        private const int TileEnergyGain = 1;

        private const float GravityCooldown = 0.5f;
        private float _gravityAccumulator;

        private int _updateInterval = 1;
        private float _accumulator;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ServerSingularityComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, ServerSingularityComponent component, StartCollideEvent args)
        {
            // This handles bouncing off of containment walls.
            // If you want the delete behavior we do it under DeleteEntities for reasons (not everything has physics).

            // If we're being deleted by another singularity, this call is probably for that singularity.
            // Even if not, just don't bother.
            if (component.BeingDeletedByAnotherSingularity)
                return;

            // Using this to also get smooth deletions is hard because we need to be hard for good bounce
            // off of containment but also we need to be non-hard so we can freely move through the station.
            // For now I've just made it so only the lookup does deletions and collision is just for fields.

            // NOTE FROM THE FUTURE: The above decision caused particles to regularly miss the singularity once it was put on a cooldown.
            // Once HandleCollide actually does deletion, Update doesn't need to be run every frame anymore.

            // Because singularity field generators do collide and *stop* the singularity, they should have deletion run on collision regardless for good behaviour.
            // This also conveniently is what the code would more or less look like anyway if removing DestroyEntities.
            // Keep in mind, though, *none of this will work unless the layers and masks are setup right.*
            HandleDestroy(component, args.OtherFixture.Body.Owner);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _gravityAccumulator += frameTime;
            _accumulator += frameTime;

            while (_accumulator > _updateInterval)
            {
                _accumulator -= _updateInterval;

                foreach (var singularity in ComponentManager.EntityQuery<ServerSingularityComponent>())
                {
                    singularity.Energy -= singularity.EnergyDrain;
                }
            }

            var gravityUpdateMultiplier = 0;
            while (_gravityAccumulator > GravityCooldown)
            {
                _gravityAccumulator -= GravityCooldown;
                gravityUpdateMultiplier += 1;
            }

            // It's important to note that updates must occur even if gravityUpdateMultiplier == 0.
            // Otherwise, fast-moving objects such as particles can and do miss the singularity.

            foreach (var singularity in ComponentManager.EntityQuery<ServerSingularityComponent>())
            {
                Update(singularity, gravityUpdateMultiplier);
            }
        }

        private void Update(ServerSingularityComponent component, int gravityUpdateMultiplier)
        {
            if (component.BeingDeletedByAnotherSingularity) return;

            var worldPos = component.Owner.Transform.WorldPosition;
            // This particular bit must run every frame to avoid missing particles
            DestroyEntities(component, worldPos);
            if (gravityUpdateMultiplier != 0)
            {
                DestroyTiles(component, worldPos);
                PullEntities(component, worldPos, gravityUpdateMultiplier);
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

        private bool CanDestroy(SharedSingularityComponent component, IEntity entity)
        {
            if (entity == component.Owner ||
                   entity.HasComponent<IMapGridComponent>() ||
                   entity.HasComponent<GhostComponent>() ||
                   entity.HasComponent<ContainmentFieldComponent>())
                return false;
            // Don't bother checking if the field itself can repel for now, we still can't destroy it.
            if (entity.TryGetComponent<ContainmentFieldGeneratorComponent>(out var field))
                if (field.CanRepell(component.Owner))
                    return false;
            return true;
        }

        private void HandleDestroy(ServerSingularityComponent component, IEntity entity)
        {
            // TODO: Need singuloimmune tag
            if (!CanDestroy(component, entity)) return;

            // Singularity priority management / etc.
            if (entity.TryGetComponent<ServerSingularityComponent>(out var otherSingulo))
            {
                // MERGE
                if (!otherSingulo.BeingDeletedByAnotherSingularity)
                {
                    component.Energy += otherSingulo.Energy;
                }

                otherSingulo.BeingDeletedByAnotherSingularity = true;
            }

            entity.QueueDelete();

            if (entity.TryGetComponent<SinguloFoodComponent>(out var singuloFood))
                component.Energy += singuloFood.Energy;
            else
                component.Energy++;
        }

        /// <summary>
        /// Handle deleting entities and increasing energy
        /// </summary>
        private void DestroyEntities(ServerSingularityComponent component, Vector2 worldPos)
        {
            // The reason we don't /just/ use collision is because we'll be deleting stuff that may not necessarily have physics (e.g. carpets).
            var destroyRange = DestroyTileRange(component);

            foreach (var entity in _lookup.GetEntitiesInRange(component.Owner.Transform.MapID, worldPos, destroyRange))
            {
                HandleDestroy(component, entity);
            }
        }

        private bool CanPull(IEntity entity)
        {
            return !(entity.HasComponent<GhostComponent>() ||
                   entity.HasComponent<IMapGridComponent>() ||
                   entity.HasComponent<MapComponent>() ||
                   entity.IsInContainer());
        }

        private void PullEntities(ServerSingularityComponent component, Vector2 worldPos, int gravityUpdateMultiplier)
        {
            // TODO: When we split up dynamic and static trees we might be able to make items always on the broadphase
            // in which case we can just query dynamictree directly for brrt
            var pullRange = PullRange(component);
            var destroyRange = DestroyTileRange(component);

            foreach (var entity in _lookup.GetEntitiesInRange(component.Owner.Transform.MapID, worldPos, pullRange))
            {
                // I tried having it so level 6 can de-anchor. BAD IDEA, MASSIVE LAG.
                if (entity == component.Owner ||
                    !entity.TryGetComponent<PhysicsComponent>(out var collidableComponent) ||
                    collidableComponent.BodyType == BodyType.Static) continue;

                if (!CanPull(entity)) continue;

                var vec = worldPos - entity.Transform.WorldPosition;

                if (vec.Length < destroyRange - 0.01f) continue;

                var speed = vec.Length * component.Level * collidableComponent.Mass;

                // Because tile friction is so high we'll just multiply by mass so stuff like closets can even move.
                collidableComponent.ApplyLinearImpulse(vec.Normalized * speed * gravityUpdateMultiplier);
            }
        }

        /// <summary>
        /// Destroy any grid tiles within the relevant Level range.
        /// </summary>
        private void DestroyTiles(ServerSingularityComponent component, Vector2 worldPos)
        {
            // The - 0.05f here is a margin of error to stop a singularity from eating force field equipment.
            // Raised to 0.05f because 0.01f wasn't enough (it's around certain internal physics margins).
            // Note that it has to be a margin like this because if the singularity *envelopes* a target, it should still be eating it regardless.
            var radius = DestroyTileRange(component) - 0.05f;

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
