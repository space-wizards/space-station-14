using System.Linq;
using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Visible;
using Content.Server.Warps;
using Content.Shared.Actions;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Ghost
{
    [UsedImplicitly]
    public sealed class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<GhostComponent, ComponentShutdown>(OnGhostShutdown);

            SubscribeLocalEvent<GhostComponent, ExaminedEvent>(OnGhostExamine);

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);
            SubscribeLocalEvent<GhostComponent, PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<GhostOnMoveComponent, MoveInputEvent>(OnRelayMoveInput);

            SubscribeNetworkEvent<GhostWarpsRequestEvent>(OnGhostWarpsRequest);
            SubscribeNetworkEvent<GhostReturnToBodyRequest>(OnGhostReturnToBodyRequest);
            SubscribeNetworkEvent<GhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);

            SubscribeLocalEvent<GhostComponent, BooActionEvent>(OnActionPerform);
            SubscribeLocalEvent<GhostComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);

            SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));
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
            // Let's not ghost if our mind is visiting...
            if (HasComp<VisitingMindComponent>(uid))
                return;

            if (!TryComp<MindContainerComponent>(uid, out var mind) || !mind.HasMind || mind.Mind.IsVisitingEntity)
                return;

            if (component.MustBeDead && (_mobState.IsAlive(uid) || _mobState.IsCritical(uid)))
                return;

            _ticker.OnGhostAttempt(mind.Mind, component.CanReturn);
        }

        private void OnGhostStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by other ghosts.
            var visibility = EnsureComp<VisibilityComponent>(uid);

            if (_ticker.RunLevel != GameRunLevel.PostRound)
            {
                _visibilitySystem.AddLayer(uid, visibility, (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.RemoveLayer(uid, visibility, (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            SetCanSeeGhosts(uid, true);

            var time = _gameTiming.CurTime;
            component.TimeOfDeath = time;

            // TODO ghost: remove once ghosts are persistent and aren't deleted when returning to body
            if (component.Action.UseDelay != null)
                component.Action.Cooldown = (time, time + component.Action.UseDelay.Value);
            _actions.AddAction(uid, component.Action, null);
        }

        private void OnGhostShutdown(EntityUid uid, GhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (Terminating(uid))
                return;

            // Entity can't be seen by ghosts anymore.
            if (TryComp(uid, out VisibilityComponent? visibility))
            {
                _visibilitySystem.RemoveLayer(uid, visibility, (int) VisibilityFlags.Ghost, false);
                _visibilitySystem.AddLayer(uid, visibility, (int) VisibilityFlags.Normal, false);
                _visibilitySystem.RefreshVisibility(uid, visibilityComponent: visibility);
            }

            // Entity can't see ghosts anymore.
            SetCanSeeGhosts(uid, false);

            _actions.RemoveAction(uid, component.Action);
        }

        private void SetCanSeeGhosts(EntityUid uid, bool canSee, EyeComponent? eyeComponent = null)
        {
            if (!Resolve(uid, ref eyeComponent))
                return;

            if (canSee)
                eyeComponent.VisibilityMask |= (uint) VisibilityFlags.Ghost;
            else
                eyeComponent.VisibilityMask &= ~(uint) VisibilityFlags.Ghost;
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
                || !TryComp(attached, out GhostComponent? ghost)
                || !ghost.CanReturnToBody
                || !TryComp(attached, out ActorComponent? actor))
            {
                Log.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(GhostReturnToBodyRequest)}");
                return;
            }

            _mindSystem.UnVisit(actor.PlayerSession.ContentData()!.Mind);
        }

        #region Warp
        private void OnGhostWarpsRequest(GhostWarpsRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} entity
                || !HasComp<GhostComponent>(entity))
            {
                Log.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
                return;
            }

            var response = new GhostWarpsResponseEvent(GetPlayerWarps(entity).Concat(GetLocationWarps()).ToList());
            RaiseNetworkEvent(response, args.SenderSession.ConnectedClient);
        }

        private void OnGhostWarpToTargetRequest(GhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached
                || !TryComp(attached, out GhostComponent? _))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Target} without being a ghost.");
                return;
            }

            if (!Exists(msg.Target))
            {
                Log.Warning($"User {args.SenderSession.Name} tried to warp to an invalid entity id: {msg.Target}");
                return;
            }

            if (TryComp(msg.Target, out WarpPointComponent? warp) && warp.Follow
                || HasComp<MobStateComponent>(msg.Target))
            {
                 _followerSystem.StartFollowingEntity(attached, msg.Target);
                 return;
            }

            var xform = Transform(attached);
            _transformSystem.SetCoordinates(attached, xform, Transform(msg.Target).Coordinates);
            _transformSystem.AttachToGridOrMap(attached, xform);
            if (TryComp(attached, out PhysicsComponent? physics))
                _physics.SetLinearVelocity(attached, Vector2.Zero, body: physics);
        }

        private IEnumerable<GhostWarp> GetLocationWarps()
        {
            var enumerator = AllEntityQuery<WarpPointComponent>();

            while (enumerator.MoveNext(out var uid, out var warp))
            {
                if (warp.Location != null)
                {
                    yield return new GhostWarp(uid, warp.Location, true);
                }
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

                var playerInfo = $"{Comp<MetaDataComponent>(attached).EntityName} ({mind?.Mind?.CurrentJob?.Name ?? "Unknown"})";

                if (_mobState.IsAlive(attached) || _mobState.IsCritical(attached))
                    yield return new GhostWarp(attached, playerInfo, false);
            }
        }
        #endregion

        private void OnEntityStorageInsertAttempt(EntityUid uid, GhostComponent comp, ref InsertIntoEntityStorageAttemptEvent args)
        {
            args.Cancelled = true;
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
                    _visibilitySystem.AddLayer(uid, vis, (int) VisibilityFlags.Normal, false);
                    _visibilitySystem.RemoveLayer(uid, vis, (int) VisibilityFlags.Ghost, false);
                }
                else
                {
                    _visibilitySystem.AddLayer(uid, vis, (int) VisibilityFlags.Ghost, false);
                    _visibilitySystem.RemoveLayer(uid, vis, (int) VisibilityFlags.Normal, false);
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
    }

    [AnyCommand]
    public sealed class ToggleGhostVisibility : IConsoleCommand
    {
        public string Command => "toggleghosts";
        public string Description => "Toggles ghost visibility";
        public string Help => $"{Command}";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player == null)
                shell.WriteLine("You can only toggle ghost visibility on a client.");

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var uid = shell.Player?.AttachedEntity;
            if (uid == null
                || !entityManager.HasComponent<GhostComponent>(uid)
                || !entityManager.TryGetComponent<EyeComponent>(uid, out var eyeComponent))
                return;

            eyeComponent.VisibilityMask ^= (uint) VisibilityFlags.Ghost;
        }
    }
}
