using System.Linq;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Storage.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Ghost
{
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SharedEyeSystem _eye = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly JobSystem _jobs = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly MindSystem _minds = default!;
        [Dependency] private readonly SharedMindSystem _mindSystem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        private EntityQuery<GhostComponent> _ghostQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;

        public override void Initialize()
        {
            base.Initialize();

            _ghostQuery = GetEntityQuery<GhostComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<GhostComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<GhostComponent, ComponentShutdown>(OnGhostShutdown);

            SubscribeLocalEvent<GhostComponent, ExaminedEvent>(OnGhostExamine);

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<GhostOnMoveComponent, MoveInputEvent>(OnRelayMoveInput);

            SubscribeNetworkEvent<GhostWarpsRequestEvent>(OnGhostWarpsRequest);
            SubscribeNetworkEvent<GhostReturnToBodyRequest>(OnGhostReturnToBodyRequest);
            SubscribeNetworkEvent<GhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
            SubscribeNetworkEvent<GhostnadoRequestEvent>(OnGhostnadoRequest);

            SubscribeLocalEvent<GhostComponent, BooActionEvent>(OnActionPerform);
            SubscribeLocalEvent<GhostComponent, ToggleGhostHearingActionEvent>(OnGhostHearingAction);
            SubscribeLocalEvent<GhostComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);

            SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));
            SubscribeLocalEvent<ToggleGhostVisibilityToAllEvent>(OnToggleGhostVisibilityToAll);
        }

        private void OnGhostHearingAction(EntityUid uid, GhostComponent component, ToggleGhostHearingActionEvent args)
        {
            args.Handled = true;

            if (HasComp<GhostHearingComponent>(uid))
            {
                RemComp<GhostHearingComponent>(uid);
                _actions.SetToggled(component.ToggleGhostHearingActionEntity, true);
            }
            else
            {
                AddComp<GhostHearingComponent>(uid);
                _actions.SetToggled(component.ToggleGhostHearingActionEntity, false);
            }

            var str = HasComp<GhostHearingComponent>(uid)
                ? Loc.GetString("ghost-gui-toggle-hearing-popup-on")
                : Loc.GetString("ghost-gui-toggle-hearing-popup-off");

            Popup.PopupEntity(str, uid, uid);
            Dirty(uid, component);
        }

        private void OnActionPerform(EntityUid uid, GhostComponent component, BooActionEvent args)
        {
            if (args.Handled)
                return;

            var entities = _lookup.GetEntitiesInRange(args.Performer, component.BooRadius);

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = DoGhostBooEvent(ent);

                if (handled)
                    booCounter++;

                if (booCounter >= component.BooMaxTargets)
                    break;
            }

            args.Handled = true;
        }

        private void OnRelayMoveInput(EntityUid uid, GhostOnMoveComponent component, ref MoveInputEvent args)
        {
            // If they haven't actually moved then ignore it.
            if ((args.Component.HeldMoveButtons &
                 (MoveButtons.Down | MoveButtons.Left | MoveButtons.Up | MoveButtons.Right)) == 0x0)
            {
                return;
            }

            // Let's not ghost if our mind is visiting...
            if (HasComp<VisitingMindComponent>(uid))
                return;

            if (!_minds.TryGetMind(uid, out var mindId, out var mind) || mind.IsVisitingEntity)
                return;

            if (component.MustBeDead && (_mobState.IsAlive(uid) || _mobState.IsCritical(uid)))
                return;

            _ticker.OnGhostAttempt(mindId, component.CanReturn, mind: mind);
        }

        private void OnGhostStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by other ghosts.
            var visibility = EnsureComp<VisibilityComponent>(uid);

            if (_ticker.RunLevel != GameRunLevel.PostRound)
            {
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            SetCanSeeGhosts(uid, true);

            var time = _gameTiming.CurTime;
            component.TimeOfDeath = time;
        }

        private void OnGhostShutdown(EntityUid uid, GhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (Terminating(uid))
                return;

            // Entity can't be seen by ghosts anymore.
            if (TryComp(uid, out VisibilityComponent? visibility))
            {
                _visibilitySystem.RemoveLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.AddLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            // Entity can't see ghosts anymore.
            SetCanSeeGhosts(uid, false);
            _actions.RemoveAction(uid, component.BooActionEntity);
        }

        private void SetCanSeeGhosts(EntityUid uid, bool canSee, EyeComponent? eyeComponent = null)
        {
            if (!Resolve(uid, ref eyeComponent, false))
                return;

            if (canSee)
                _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | (int) VisibilityFlags.Ghost, eyeComponent);
            else
                _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask & ~(int) VisibilityFlags.Ghost, eyeComponent);
        }

        private void OnMapInit(EntityUid uid, GhostComponent component, MapInitEvent args)
        {
            if (_actions.AddAction(uid, ref component.BooActionEntity, out var act, component.BooAction)
                && act.UseDelay != null)
            {
                var start = _gameTiming.CurTime;
                var end = start + act.UseDelay.Value;
                _actions.SetCooldown(component.BooActionEntity.Value, start, end);
            }

            _actions.AddAction(uid, ref component.ToggleGhostHearingActionEntity, component.ToggleGhostHearingAction);
            _actions.AddAction(uid, ref component.ToggleLightingActionEntity, component.ToggleLightingAction);
            _actions.AddAction(uid, ref component.ToggleFoVActionEntity, component.ToggleFoVAction);
            _actions.AddAction(uid, ref component.ToggleGhostsActionEntity, component.ToggleGhostsAction);
        }

        private void OnGhostExamine(EntityUid uid, GhostComponent component, ExaminedEvent args)
        {
            var timeSinceDeath = _gameTiming.RealTime.Subtract(component.TimeOfDeath);
            var deathTimeInfo = timeSinceDeath.Minutes > 0
                ? Loc.GetString("comp-ghost-examine-time-minutes", ("minutes", timeSinceDeath.Minutes))
                : Loc.GetString("comp-ghost-examine-time-seconds", ("seconds", timeSinceDeath.Seconds));

            args.PushMarkup(deathTimeInfo);
        }

        #region Ghost Deletion

        private void OnMindRemovedMessage(EntityUid uid, GhostComponent component, MindRemovedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnMindUnvisitedMessage(EntityUid uid, GhostComponent component, MindUnvisitedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnPlayerDetached(EntityUid uid, GhostComponent component, PlayerDetachedEvent args)
        {
            DeleteEntity(uid);
        }

        private void DeleteEntity(EntityUid uid)
        {
            if (Deleted(uid) || Terminating(uid))
                return;

            QueueDel(uid);
        }

        #endregion

        private void OnGhostReturnToBodyRequest(GhostReturnToBodyRequest msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !_ghostQuery.TryComp(attached, out var ghost)
                || !ghost.CanReturnToBody
                || !TryComp(attached, out ActorComponent? actor))
            {
                Log.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(GhostReturnToBodyRequest)}");
                return;
            }

            _mindSystem.UnVisit(actor.PlayerSession);
        }

        #region Warp

        private void OnGhostWarpsRequest(GhostWarpsRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} entity
                || !_ghostQuery.HasComp(entity))
            {
                Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
                return;
            }

            var response = new GhostWarpsResponseEvent(GetPlayerWarps(entity).Concat(GetLocationWarps()).ToList());
            RaiseNetworkEvent(response, args.SenderSession.Channel);
        }

        private void OnGhostWarpToTargetRequest(GhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !_ghostQuery.HasComp(attached))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Target} without being a ghost.");
                return;
            }

            var target = GetEntity(msg.Target);

            if (!Exists(target))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to an invalid entity id: {msg.Target}");
                return;
            }

            WarpTo(attached, target);
        }

        private void OnGhostnadoRequest(GhostnadoRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {} uid
                || !_ghostQuery.HasComp(uid))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to ghostnado without being a ghost.");
                return;
            }

            if (_followerSystem.GetMostGhostFollowed() is not {} target)
                return;

            WarpTo(uid, target);
        }

        private void WarpTo(EntityUid uid, EntityUid target)
        {
            if ((TryComp(target, out WarpPointComponent? warp) && warp.Follow) || HasComp<MobStateComponent>(target))
            {
                _followerSystem.StartFollowingEntity(uid, target);
                return;
            }

            var xform = Transform(uid);
            _transformSystem.SetCoordinates(uid, xform, Transform(target).Coordinates);
            _transformSystem.AttachToGridOrMap(uid, xform);
            if (_physicsQuery.TryComp(uid, out var physics))
                _physics.SetLinearVelocity(uid, Vector2.Zero, body: physics);
        }

        private IEnumerable<GhostWarp> GetLocationWarps()
        {
            var allQuery = AllEntityQuery<WarpPointComponent>();

            while (allQuery.MoveNext(out var uid, out var warp))
            {
                yield return new GhostWarp(GetNetEntity(uid), warp.Location ?? Name(uid), true);
            }
        }

        private IEnumerable<GhostWarp> GetPlayerWarps(EntityUid except)
        {
            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is not {Valid: true} attached)
                    continue;

                if (attached == except) continue;

                TryComp<MindContainerComponent>(attached, out var mind);

                var jobName = _jobs.MindTryGetJobName(mind?.Mind);
                var playerInfo = $"{Comp<MetaDataComponent>(attached).EntityName} ({jobName})";

                if (_mobState.IsAlive(attached) || _mobState.IsCritical(attached))
                    yield return new GhostWarp(GetNetEntity(attached), playerInfo, false);
            }
        }

        #endregion

        private void OnEntityStorageInsertAttempt(EntityUid uid, GhostComponent comp, ref InsertIntoEntityStorageAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnToggleGhostVisibilityToAll(ToggleGhostVisibilityToAllEvent ev)
        {
            if (ev.Handled)
                return;

            ev.Handled = true;
            MakeVisible(true);
        }

        /// <summary>
        /// When the round ends, make all players able to see ghosts.
        /// </summary>
        public void MakeVisible(bool visible)
        {
            var entityQuery = EntityQueryEnumerator<GhostComponent, VisibilityComponent>();
            while (entityQuery.MoveNext(out var uid, out _, out var vis))
            {
                if (visible)
                {
                    _visibilitySystem.AddLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                    _visibilitySystem.RemoveLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                }
                else
                {
                    _visibilitySystem.AddLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                    _visibilitySystem.RemoveLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                }
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: vis);
            }
        }

        public bool DoGhostBooEvent(EntityUid target)
        {
            var ghostBoo = new GhostBooEvent();
            RaiseLocalEvent(target, ghostBoo, true);

            return ghostBoo.Handled;
        }

        public EntityUid? SpawnGhost(Entity<MindComponent?> mind, EntityUid targetEntity,
            bool canReturn = false)
        {
            _transformSystem.TryGetMapOrGridCoordinates(targetEntity, out var spawnPosition);
            return SpawnGhost(mind, spawnPosition, canReturn);
        }

        private bool IsValidSpawnPosition(EntityCoordinates? spawnPosition)
        {
            if (spawnPosition?.IsValid(EntityManager) != true)
                return false;

            var mapUid = spawnPosition?.GetMapUid(EntityManager);
            var gridUid = spawnPosition?.EntityId;
            // Test if the map is being deleted
            if (mapUid == null || TerminatingOrDeleted(mapUid.Value))
                return false;
            // Test if the grid is being deleted
            if (gridUid != null && TerminatingOrDeleted(gridUid.Value))
                return false;

            return true;
        }

        public EntityUid? SpawnGhost(Entity<MindComponent?> mind, EntityCoordinates? spawnPosition = null,
            bool canReturn = false)
        {
            if (!Resolve(mind, ref mind.Comp))
                return null;

            // Test if the map or grid is being deleted
            if (!IsValidSpawnPosition(spawnPosition))
                spawnPosition = null;

            // If it's bad, look for a valid point to spawn
            spawnPosition ??= _ticker.GetObserverSpawnPoint();

            // Make sure the new point is valid too
            if (!IsValidSpawnPosition(spawnPosition))
            {
                Log.Warning($"No spawn valid ghost spawn position found for {mind.Comp.CharacterName}"
                    + $" \"{ToPrettyString(mind)}\"");
                _minds.TransferTo(mind.Owner, null, createGhost: false, mind: mind.Comp);
                return null;
            }

            var ghost = SpawnAtPosition(GameTicker.ObserverPrototypeName, spawnPosition.Value);
            var ghostComponent = Comp<GhostComponent>(ghost);

            // Try setting the ghost entity name to either the character name or the player name.
            // If all else fails, it'll default to the default entity prototype name, "observer".
            // However, that should rarely happen.
            if (!string.IsNullOrWhiteSpace(mind.Comp.CharacterName))
                _metaData.SetEntityName(ghost, mind.Comp.CharacterName);
            else if (!string.IsNullOrWhiteSpace(mind.Comp.Session?.Name))
                _metaData.SetEntityName(ghost, mind.Comp.Session.Name);

            if (mind.Comp.TimeOfDeath.HasValue)
            {
                SetTimeOfDeath(ghost, mind.Comp.TimeOfDeath!.Value, ghostComponent);
            }

            SetCanReturnToBody(ghostComponent, canReturn);

            if (canReturn)
                _minds.Visit(mind.Owner, ghost, mind.Comp);
            else
                _minds.TransferTo(mind.Owner, ghost, mind: mind.Comp);
            Log.Debug($"Spawned ghost \"{ToPrettyString(ghost)}\" for {mind.Comp.CharacterName}.");
            return ghost;
        }
    }
}
