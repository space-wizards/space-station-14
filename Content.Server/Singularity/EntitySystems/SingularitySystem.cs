using Content.Server.Ghost.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity;
using Content.Shared.Singularity.Components;
using JetBrains.Annotations;
using Robust.Server.GameStates;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public sealed class SingularitySystem : SharedSingularitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly PVSOverrideSystem _pvs = default!;
        [Dependency] private readonly ContainmentFieldGeneratorSystem _fieldGeneratorSystem = default!;
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
            SubscribeLocalEvent<ServerSingularityComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<SingularityDistortionComponent, ComponentStartup>(OnDistortionStartup);
        }

        private void OnDistortionStartup(EntityUid uid, SingularityDistortionComponent component, ComponentStartup args)
        {
            // to avoid distortion overlay pop-in, entities with distortion ignore PVS. Really this should probably be a
            // PVS range-override, but this is good enough for now.
            _pvs.AddGlobalOverride(uid);
        }

        protected override bool PreventCollide(EntityUid uid, SharedSingularityComponent component, PreventCollideEvent args)
        {
            if (base.PreventCollide(uid, component, args)) return true;

            var otherUid = args.BodyB.Owner;

            if (args.Cancelled) return true;

            // If it's not cancelled then we'll cancel if we can't immediately destroy it on collision
            if (!CanDestroy(component, otherUid))
                args.Cancel();

            return true;
        }

        private void OnCollide(EntityUid uid, ServerSingularityComponent component, StartCollideEvent args)
        {
            if (args.OurFixture.ID != "DeleteCircle") return;

            // This handles bouncing off of containment walls.
            // If you want the delete behavior we do it under DeleteEntities for reasons (not everything has physics).

            // If we're being deleted by another singularity, this call is probably for that singularity.
            // Even if not, just don't bother.
            if (component.BeingDeletedByAnotherSingularity)
                return;

            var otherUid = args.OtherFixture.Body.Owner;

            // HandleDestroy will also check CanDestroy for us
            HandleDestroy(component, otherUid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _gravityAccumulator += frameTime;
            _accumulator += frameTime;

            while (_accumulator > _updateInterval)
            {
                _accumulator -= _updateInterval;

                foreach (var singularity in EntityManager.EntityQuery<ServerSingularityComponent>())
                {
                    singularity.Energy -= singularity.EnergyDrain;
                }
            }

            while (_gravityAccumulator > GravityCooldown)
            {
                _gravityAccumulator -= GravityCooldown;

                foreach (var (singularity, xform) in EntityManager.EntityQuery<ServerSingularityComponent, TransformComponent>())
                {
                    Update(singularity, xform, GravityCooldown);
                }
            }
        }

        private void Update(ServerSingularityComponent component, TransformComponent xform, float frameTime)
        {
            if (component.BeingDeletedByAnotherSingularity) return;

            var worldPos = xform.WorldPosition;
            DestroyEntities(component, xform, worldPos);
            DestroyTiles(component, xform, worldPos);
            PullEntities(component, xform, worldPos, frameTime);
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

        private bool CanDestroy(SharedSingularityComponent component, EntityUid entity)
        {
            return entity != component.Owner &&
                   !EntityManager.HasComponent<IMapGridComponent>(entity) &&
                   !EntityManager.HasComponent<GhostComponent>(entity) &&
                   (component.Level > 4 ||
                   !EntityManager.HasComponent<ContainmentFieldComponent>(entity) &&
                   !(EntityManager.TryGetComponent<ContainmentFieldGeneratorComponent>(entity, out var containFieldGen) && _fieldGeneratorSystem.CanRepel(component, containFieldGen)));
        }

        private void HandleDestroy(ServerSingularityComponent component, EntityUid entity)
        {
            // TODO: Need singuloimmune tag
            if (!CanDestroy(component, entity)) return;

            // Singularity priority management / etc.
            if (EntityManager.TryGetComponent<ServerSingularityComponent?>(entity, out var otherSingulo))
            {
                // MERGE
                if (!otherSingulo.BeingDeletedByAnotherSingularity)
                {
                    component.Energy += otherSingulo.Energy;
                }

                otherSingulo.BeingDeletedByAnotherSingularity = true;
            }

            if (EntityManager.TryGetComponent<SinguloFoodComponent?>(entity, out var singuloFood))
                component.Energy += singuloFood.Energy;
            else
                component.Energy++;

            EntityManager.QueueDeleteEntity(entity);
        }

        /// <summary>
        /// Handle deleting entities and increasing energy
        /// </summary>
        private void DestroyEntities(ServerSingularityComponent component, TransformComponent xform, Vector2 worldPos)
        {
            // The reason we don't /just/ use collision is because we'll be deleting stuff that may not necessarily have physics (e.g. carpets).
            var destroyRange = DestroyTileRange(component);

            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, worldPos, destroyRange))
            {
                HandleDestroy(component, entity);
            }
        }

        private bool CanPull(EntityUid entity)
        {
            return !(EntityManager.HasComponent<GhostComponent>(entity) ||
                   EntityManager.HasComponent<IMapGridComponent>(entity) ||
                   EntityManager.HasComponent<MapComponent>(entity) ||
                   EntityManager.HasComponent<ServerSingularityComponent>(entity) ||
                   _container.IsEntityInContainer(entity));
        }

        /// <summary>
        /// Pull dynamic bodies in range to the singulo.
        /// </summary>
        private void PullEntities(ServerSingularityComponent component, TransformComponent xform, Vector2 worldPos, float frameTime)
        {
            // TODO: When we split up dynamic and static trees we might be able to make items always on the broadphase
            // in which case we can just query dynamictree directly for brrt
            var pullRange = PullRange(component);
            var destroyRange = DestroyTileRange(component);

            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapID, worldPos, pullRange))
            {
                // I tried having it so level 6 can de-anchor. BAD IDEA, MASSIVE LAG.
                if (entity == component.Owner ||
                    !TryComp<PhysicsComponent?>(entity, out var collidableComponent) ||
                    collidableComponent.BodyType == BodyType.Static) continue;

                if (!CanPull(entity)) continue;

                var vec = worldPos - Transform(entity).WorldPosition;

                if (vec.Length < destroyRange - 0.01f) continue;

                var speed = vec.Length * component.Level * collidableComponent.Mass * 100f;

                // Because tile friction is so high we'll just multiply by mass so stuff like closets can even move.
                collidableComponent.ApplyLinearImpulse(vec.Normalized * speed * frameTime);
            }
        }

        /// <summary>
        /// Destroy any grid tiles within the relevant Level range.
        /// </summary>
        private void DestroyTiles(ServerSingularityComponent component, TransformComponent xform, Vector2 worldPos)
        {
            var radius = DestroyTileRange(component);

            var circle = new Circle(worldPos, radius);
            var box = new Box2(worldPos - radius, worldPos + radius);

            foreach (var grid in _mapManager.FindGridsIntersecting(xform.MapID, box))
            {
                // Bundle these together so we can use the faster helper to set tiles.
                var toDestroy = new List<(Vector2i, Tile)>();

                foreach (var tile in grid.GetTilesIntersecting(circle))
                {
                    if (tile.Tile.IsEmpty) continue;

                    // Avoid ripping up tiles that may be essential to containment
                    if (component.Level < 5)
                    {
                        var canDelete = true;

                        foreach (var ent in grid.GetAnchoredEntities(tile.GridIndices))
                        {
                            if (EntityManager.HasComponent<ContainmentFieldComponent>(ent) ||
                                EntityManager.HasComponent<ContainmentFieldGeneratorComponent>(ent))
                            {
                                canDelete = false;
                                break;
                            }
                        }

                        if (!canDelete) continue;
                    }

                    toDestroy.Add((tile.GridIndices, Tile.Empty));
                }

                component.Energy += TileEnergyGain * toDestroy.Count;
                grid.SetTiles(toDestroy);
            }
        }
    }
}
