using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Visible;
using Content.Server.Warps;
using Content.Shared.Examine;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.EntitySystems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
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
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly FollowerSystem _followerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GhostComponent, ComponentStartup>(OnGhostStartup);
            SubscribeLocalEvent<GhostComponent, ComponentShutdown>(OnGhostShutdown);

            SubscribeLocalEvent<GhostComponent, ExaminedEvent>(OnGhostExamine);

            SubscribeLocalEvent<GhostComponent, MindRemovedMessage>(OnMindRemovedMessage);
            SubscribeLocalEvent<GhostComponent, MindUnvisitedMessage>(OnMindUnvisitedMessage);

            SubscribeLocalEvent<GhostOnMoveComponent, RelayMoveInputEvent>(OnRelayMoveInput);

            SubscribeNetworkEvent<GhostWarpsRequestEvent>(OnGhostWarpsRequest);
            SubscribeNetworkEvent<GhostReturnToBodyRequest>(OnGhostReturnToBodyRequest);
            SubscribeNetworkEvent<GhostWarpToLocationRequestEvent>(OnGhostWarpToLocationRequest);
            SubscribeNetworkEvent<GhostWarpToTargetRequestEvent>(OnGhostWarpToTargetRequest);
        }

        private void OnRelayMoveInput(EntityUid uid, GhostOnMoveComponent component, RelayMoveInputEvent args)
        {
            // Let's not ghost if our mind is visiting...
            if (EntityManager.HasComponent<VisitingMindComponent>(uid)) return;
            if (!EntityManager.TryGetComponent<MindComponent>(uid, out var mind) || !mind.HasMind || mind.Mind!.IsVisitingEntity) return;
            if (component.MustBeDead && TryComp<MobStateComponent>(uid, out var state) && !state.IsDead()) return;

            _ticker.OnGhostAttempt(mind.Mind!, component.CanReturn);
        }

        private void OnGhostStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by other ghosts.
            var visibility = EntityManager.EnsureComponent<VisibilityComponent>(component.Owner);

            _visibilitySystem.AddLayer(visibility, (int) VisibilityFlags.Ghost, false);
            _visibilitySystem.RemoveLayer(visibility, (int) VisibilityFlags.Normal, false);
            _visibilitySystem.RefreshVisibility(visibility);

            if (EntityManager.TryGetComponent(component.Owner, out EyeComponent? eye))
            {
                eye.VisibilityMask |= (uint) VisibilityFlags.Ghost;
            }

            component.TimeOfDeath = _gameTiming.RealTime;
        }

        private void OnGhostShutdown(EntityUid uid, GhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (!Terminating(uid))
            {
                // Entity can't be seen by ghosts anymore.
                if (EntityManager.TryGetComponent(component.Owner, out VisibilityComponent? visibility))
                {
                    _visibilitySystem.RemoveLayer(visibility, (int) VisibilityFlags.Ghost, false);
                    _visibilitySystem.AddLayer(visibility, (int) VisibilityFlags.Normal, false);
                    _visibilitySystem.RefreshVisibility(visibility);
                }

                // Entity can't see ghosts anymore.
                if (EntityManager.TryGetComponent(component.Owner, out EyeComponent? eye))
                {
                    eye.VisibilityMask &= ~(uint) VisibilityFlags.Ghost;
                }
            }
        }

        private void OnGhostExamine(EntityUid uid, GhostComponent component, ExaminedEvent args)
        {
            var timeSinceDeath = _gameTiming.RealTime.Subtract(component.TimeOfDeath);
            var deathTimeInfo = timeSinceDeath.Minutes > 0
                ? Loc.GetString("comp-ghost-examine-time-minutes", ("minutes", timeSinceDeath.Minutes))
                : Loc.GetString("comp-ghost-examine-time-seconds", ("seconds", timeSinceDeath.Seconds));

            args.PushMarkup(deathTimeInfo);
        }

        private void OnMindRemovedMessage(EntityUid uid, GhostComponent component, MindRemovedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnMindUnvisitedMessage(EntityUid uid, GhostComponent component, MindUnvisitedMessage args)
        {
            DeleteEntity(uid);
        }

        private void OnGhostWarpsRequest(GhostWarpsRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} entity ||
                !EntityManager.HasComponent<GhostComponent>(entity))
            {
                Logger.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
                return;
            }

            var response = new GhostWarpsResponseEvent(GetLocationNames().ToList(), GetPlayerWarps(entity));
            RaiseNetworkEvent(response, args.SenderSession.ConnectedClient);
        }

        private void OnGhostReturnToBodyRequest(GhostReturnToBodyRequest msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.TryGetComponent(attached, out GhostComponent? ghost) ||
                !ghost.CanReturnToBody ||
                !EntityManager.TryGetComponent(attached, out ActorComponent? actor))
            {
                Logger.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(GhostReturnToBodyRequest)}");
                return;
            }

            actor.PlayerSession.ContentData()!.Mind?.UnVisit();
        }

        private void OnGhostWarpToLocationRequest(GhostWarpToLocationRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.TryGetComponent(attached, out GhostComponent? ghost))
            {
                Logger.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Name} without being a ghost.");
                return;
            }

            if (FindLocation(msg.Name) is { } warp)
            {
                EntityManager.GetComponent<TransformComponent>(ghost.Owner).Coordinates = EntityManager.GetComponent<TransformComponent>(warp.Owner).Coordinates;
                return;
            }

            Logger.Warning($"User {args.SenderSession.Name} tried to warp to an invalid warp: {msg.Name}");
        }

        private void OnGhostWarpToTargetRequest(GhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity is not {Valid: true} attached ||
                !EntityManager.TryGetComponent(attached, out GhostComponent? ghost))
            {
                Logger.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Target} without being a ghost.");
                return;
            }

            if (!EntityManager.EntityExists(msg.Target))
            {
                Logger.Warning($"User {args.SenderSession.Name} tried to warp to an invalid entity id: {msg.Target}");
                return;
            }

            _followerSystem.StartFollowingEntity(ghost.Owner, msg.Target);
        }

        private void DeleteEntity(EntityUid uid)
        {
            if (Deleted(uid) || Terminating(uid))
                return;

            if (EntityManager.TryGetComponent<MindComponent?>(uid, out var mind))
                _mindSystem.SetGhostOnShutdown(uid, false, mind);
            EntityManager.DeleteEntity(uid);
        }

        private IEnumerable<string> GetLocationNames()
        {
            foreach (var warp in EntityManager.EntityQuery<WarpPointComponent>(true))
            {
                if (warp.Location != null)
                {
                    yield return warp.Location;
                }
            }
        }

        private WarpPointComponent? FindLocation(string name)
        {
            foreach (var warp in EntityManager.EntityQuery<WarpPointComponent>(true))
            {
                if (warp.Location == name)
                {
                    return warp;
                }
            }

            return null;
        }

        private Dictionary<EntityUid, string> GetPlayerWarps(EntityUid except)
        {
            var players = new Dictionary<EntityUid, string>();

            foreach (var player in _playerManager.Sessions)
            {
                if (player.AttachedEntity is {Valid: true} attached)
                {
                    players.Add(attached, EntityManager.GetComponent<MetaDataComponent>(attached).EntityName);
                }
            }

            players.Remove(except);

            return players;
        }
    }
}
