using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using System.Threading;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Dragon
{
    public sealed class DragonSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DragonComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DragonComponent, DragonDevourComplete>(OnDragonDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourActionEvent>(OnDevourAction);
            SubscribeLocalEvent<DragonComponent, DragonSpawnActionEvent>(OnDragonSpawnAction);
            SubscribeLocalEvent<DragonComponent, DragonBreatheFireActionEvent>(OnDragonBreathFire);

            SubscribeLocalEvent<DragonComponent, DragonStructureDevourComplete>(OnDragonStructureDevourComplete);
            SubscribeLocalEvent<DragonComponent, DragonDevourCancelledEvent>(OnDragonDevourCancelled);
            SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);
        }

        private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
        {
            //Empties the stomach upon death
            //TODO: Do this when the dragon gets butchered instead
            if (args.CurrentMobState.IsDead())
            {
                if (component.SoundDeath != null)
                    SoundSystem.Play(component.SoundDeath.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid, component.SoundDeath.Params);

                component.DragonStomach.EmptyContainer();
            }
        }

        private void OnDragonDevourCancelled(EntityUid uid, DragonComponent component, DragonDevourCancelledEvent args)
        {
            component.CancelToken = null;
        }

        private void OnDragonDevourComplete(EntityUid uid, DragonComponent component, DragonDevourComplete args)
        {
            component.CancelToken = null;
            var ichorInjection = new Solution(component.DevourChem, component.DevourHealRate);

            //Humanoid devours allow dragon to get eggs, corpses included
            if (EntityManager.HasComponent<HumanoidAppearanceComponent>(args.Target))
            {
                // Add a spawn for a consumed humanoid
                component.SpawnsLeft = Math.Min(component.SpawnsLeft + 1, component.MaxSpawns);
            }
            //Non-humanoid mobs can only heal dragon for half the normal amount, with no additional spawn tickets
            else
            {
                ichorInjection.ScaleSolution(0.5f);
            }

            _bloodstreamSystem.TryAddToChemicals(uid, ichorInjection);
            component.DragonStomach.Insert(args.Target);

            if (component.SoundDevour != null)
                SoundSystem.Play(component.SoundDevour.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid, component.SoundDevour.Params);
        }

        private void OnDragonStructureDevourComplete(EntityUid uid, DragonComponent component, DragonStructureDevourComplete args)
        {
            component.CancelToken = null;
            //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
            EntityManager.QueueDeleteEntity(args.Target);

            if (component.SoundDevour != null)
                SoundSystem.Play(component.SoundDevour.GetSound(), Filter.Pvs(args.User, entityManager: EntityManager), uid, component.SoundDevour.Params);
        }

        private void OnStartup(EntityUid uid, DragonComponent component, ComponentStartup args)
        {
            component.SpawnsLeft = Math.Min(component.SpawnsLeft, component.MaxSpawns);

            //Dragon doesn't actually chew, since he sends targets right into his stomach.
            //I did it mom, I added ERP content into upstream. Legally!
            component.DragonStomach = _containerSystem.EnsureContainer<Container>(uid, "dragon_stomach");

            if (component.DevourAction != null)
                _actionsSystem.AddAction(uid, component.DevourAction, null);

            if (component.SpawnAction != null)
                _actionsSystem.AddAction(uid, component.SpawnAction, null);

            if(component.BreatheFireAction != null)
                _actionsSystem.AddAction(uid, component.BreatheFireAction, null);

            if (component.SoundRoar != null)
                SoundSystem.Play(component.SoundRoar.GetSound(), Filter.Pvs(uid, 4f, EntityManager), uid, component.SoundRoar.Params);
        }

        /// <summary>
        /// The devour action
        /// </summary>
        private void OnDevourAction(EntityUid uid, DragonComponent component, DragonDevourActionEvent args)
        {
            if (component.CancelToken != null ||
                args.Handled ||
                component.DevourWhitelist?.IsValid(args.Target, EntityManager) != true) return;

            args.Handled = true;
            var target = args.Target;

            // Structure and mob devours handled differently.
            if (EntityManager.TryGetComponent(target, out MobStateComponent? targetState))
            {
                switch (targetState.CurrentState)
                {
                    case DamageState.Critical:
                    case DamageState.Dead:
                        component.CancelToken = new CancellationTokenSource();

                        _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.DevourTime, component.CancelToken.Token, target)
                        {
                            UserFinishedEvent = new DragonDevourComplete(uid, target),
                            UserCancelledEvent = new DragonDevourCancelledEvent(),
                            BreakOnTargetMove = true,
                            BreakOnUserMove = true,
                            BreakOnStun = true,
                        });
                        break;
                    default:
                        _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-fail-target-alive"), uid, Filter.Entities(uid));
                        break;
                }

                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("devour-action-popup-message-structure"), uid, Filter.Entities(uid));

            if (component.SoundStructureDevour != null)
                SoundSystem.Play(component.SoundStructureDevour.GetSound(), Filter.Pvs(uid, entityManager: EntityManager), uid, component.SoundStructureDevour.Params);

            component.CancelToken = new CancellationTokenSource();

            _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.StructureDevourTime, component.CancelToken.Token, target)
            {
                UserFinishedEvent = new DragonStructureDevourComplete(uid, target),
                UserCancelledEvent = new DragonDevourCancelledEvent(),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
            });
        }

        private void OnDragonSpawnAction(EntityUid dragonuid, DragonComponent component, DragonSpawnActionEvent args)
        {
            if (component.SpawnPrototype == null) return;

            // If dragon has spawns then add one.
            if (component.SpawnsLeft > 0)
            {
                Spawn(component.SpawnPrototype, Transform(dragonuid).Coordinates);
                component.SpawnsLeft--;
                return;
            }

            _popupSystem.PopupEntity(Loc.GetString("dragon-spawn-action-popup-message-fail-no-eggs"), dragonuid, Filter.Entities(dragonuid));
        }


        /// <summary>
        /// Determines the list of tiles that the breath attack should affects.
        /// </summary>
        /// <param name="breathDirection">Normalized vector representing the direction.</param>
        /// <param name="distance">The number of tiles to propogate the attack over.</param>
        /// <param name="start">The position to start from.</param>
        /// <param name="referenceGrid">The grid to use as the basis for tile coordinates.</param>
        /// <returns>List of tile coordinates to apply the breath effect to, starting from the origin position.</returns>
        private List<Vector2i> CalculateBreathTiles(Vector2 breathDirection, int distance, MapCoordinates start, IMapGrid referenceGrid)
        {
            var points = new List<Vector2i>();
            var startTile = referenceGrid.TileIndicesFor(start);
            var lastFreeTile = startTile;

            int offset = 0; //
            int curDistance = 0;
            while (curDistance < distance)
            {
                var tilePos = start.Offset(breathDirection * ((curDistance + offset) * referenceGrid.TileSize));
                var tile = referenceGrid.TileIndicesFor(tilePos); //

                if (tile == lastFreeTile)
                {
                    offset++;
                    continue;
                }

                points.Add(tile);
                lastFreeTile = tile;
                curDistance++;
            }

            return points;
        }

        /// <summary>
        ///     Determines whether an entity is blocking a tile or not. (whether it should prevent the line from continuing).
        /// </summary>
        /// <remarks>
        ///     Used for a variation of <see cref="TurfHelpers.IsBlockedTurf()"/> that makes use of the fact that we have
        ///     already done an entity lookup on a tile, and don't need to do so again.
        /// </remarks>
        private bool IsBlockingTurf(EntityUid uid, EntityQuery<PhysicsComponent> physicsQuery)
        {
            if (EntityManager.IsQueuedForDeletion(uid))
                return false;

            if (!physicsQuery.TryGetComponent(uid, out var physics))
                return false;

            return physics.CanCollide && physics.Hard && (physics.CollisionLayer & (int)CollisionGroup.HumanoidBlockLayer) != 0;
        }

        private void OnDragonBreathFire(EntityUid dragonuid, DragonComponent component,
            DragonBreatheFireActionEvent args)
        {
            if (component.BreatheFireAction == null)
                return;

            var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
            var mobStateQuery = EntityManager.GetEntityQuery<MobStateComponent>();
            var physicsQuery = EntityManager.GetEntityQuery<PhysicsComponent>();
            var damageableQuery = EntityManager.GetEntityQuery<DamageableComponent>();

            var dragonXform = Transform(dragonuid);
            var dragonPos = dragonXform.MapPosition;

            if (dragonXform.GridUid == null)
                return; // TODO: Support fire breath from outside of grids

            var breathDirection = (args.Target.Position - dragonPos.Position).Normalized;
            Vector2i breathInitialTile;

            var grid = _mapManager.GetGrid(dragonXform.GridUid.Value);

            // Get a list of points to apply the fire breath to.
            var points = CalculateBreathTiles(breathDirection, (int) component.BreatheFireAction.Range, dragonPos, grid);


            var lookup = Comp<EntityLookupComponent>(grid.GridEntityId);

            HashSet<EntityUid> processed = new(); // Entities that have previously been processed.
            processed.Add(dragonuid); // Ignore the creator of the fire breath.

            // TODO: Fire effects and hit check should propagate over time.
            // TODO: Figure out why cooldown does not activate.
            // TODO: Optimize with usage of entity queries and lookup systems.
            foreach(var p in points)
            {
                var tileBox = new Box2(p * grid.TileSize, (p + 1) * grid.TileSize);

                // Get the entities on the tile.
                List<TransformComponent> list = new();

                var state = (list, processed, xformQuery, mobStateQuery);
                lookup.Tree.QueryAabb(ref state, GridQueryCallback, tileBox);

                // process the entities
                foreach (var xform in list)
                {
                    Logger.Debug("Hit {0}", MetaData(xform.Owner).EntityName);
                    ProcessEntity(xform.Owner, component.BreathDamage, damageableQuery);
                }

                // process anchored entities
                var tileBlocked = false;
                var anchoredEntities = grid.GetAnchoredEntities(p).ToList();
                foreach (var anchored in anchoredEntities)
                {
                    processed.Add(anchored);
                    // TODO: Can entities with mob state be anchored?
                    // ProcessEntity(anchored, component.BreathDamage, damageableQuery);
                    tileBlocked |= IsBlockingTurf(anchored, physicsQuery);
                }

                if (tileBlocked)
                    break; // Blocked by an obstruction.

                // Attempt to ignite the tile.
                _atmosphereSystem.HotspotExpose(grid.GridEntityId, p, 700f, 50f, true);

                var coords = grid.GridTileToLocal(p);
                Spawn(component.BreathEffectPrototype, coords);
            }

            if(component.SoundBreathFire != null)
                SoundSystem.Play(component.SoundBreathFire.GetSound(), Filter.Pvs(args.Performer, 4f, EntityManager), dragonuid, component.SoundBreathFire.Params);
        }

        /// <summary>
        /// Apply damage to the entity
        /// </summary>
        private void ProcessEntity(
            EntityUid uid,
            DamageSpecifier? damage,
            EntityQuery<DamageableComponent> damageableQuery)
        {
            if (damage == null || !damageableQuery.HasComponent(uid))
                return;

            _damageableSystem.TryChangeDamage(uid, damage);
        }

        /// <summary>
        /// Callback function to check if an entity should have further processing performed.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        private bool GridQueryCallback(
            ref (List<TransformComponent> List, HashSet<EntityUid> Processed, EntityQuery<TransformComponent> XformQuery, EntityQuery<MobStateComponent> mobStateQuery) state,
            in EntityUid uid)
        {
            // Add entities with transform components to the list of entities to process.
            if (state.Processed.Add(uid) &&
                state.mobStateQuery.HasComponent(uid) &&
                state.XformQuery.TryGetComponent(uid, out var xform))
            {
                state.List.Add(xform);
            }

            return true;
        }

        private sealed class DragonDevourComplete : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public DragonDevourComplete(EntityUid user, EntityUid target)
            {
                User = user;
                Target = target;
            }
        }

        private sealed class DragonStructureDevourComplete : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid Target { get; }

            public DragonStructureDevourComplete(EntityUid user, EntityUid target)
            {
                 User = user;
                 Target = target;
            }
        }

        private sealed class DragonDevourCancelledEvent : EntityEventArgs {}
    }
}
