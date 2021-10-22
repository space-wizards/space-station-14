using System.Collections.Generic;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Visible;
using Content.Server.Warps;
using Content.Shared.Examine;
using Content.Shared.Ghost;
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
    public class GhostSystem : SharedGhostSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly GameTicker _ticker = default!;

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

            _ticker.OnGhostAttempt(mind.Mind!, component.CanReturn);
        }

        private void OnGhostStartup(EntityUid uid, GhostComponent component, ComponentStartup args)
        {
            // Allow this entity to be seen by other ghosts.
            var visibility = component.Owner.EnsureComponent<VisibilityComponent>();

            visibility.Layer |= (int) VisibilityFlags.Ghost;
            visibility.Layer &= ~(int) VisibilityFlags.Normal;

            if (component.Owner.TryGetComponent(out EyeComponent? eye))
            {
                eye.VisibilityMask |= (uint) VisibilityFlags.Ghost;
            }

            component.TimeOfDeath = _gameTiming.RealTime;
        }

        private void OnGhostShutdown(EntityUid uid, GhostComponent component, ComponentShutdown args)
        {
            // Perf: If the entity is deleting itself, no reason to change these back.
            if (component.Owner.LifeStage < EntityLifeStage.Terminating)
            {
                // Entity can't be seen by ghosts anymore.
                if (component.Owner.TryGetComponent(out VisibilityComponent? visibility))
                {
                    visibility.Layer &= ~(int) VisibilityFlags.Ghost;
                    visibility.Layer |= (int) VisibilityFlags.Normal;
                }

                // Entity can't see ghosts anymore.
                if (component.Owner.TryGetComponent(out EyeComponent? eye))
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
            var entity = args.SenderSession.AttachedEntity;

            if (entity == null ||
                !entity.HasComponent<GhostComponent>())
            {
                Logger.Warning($"User {args.SenderSession.Name} sent a {nameof(GhostWarpsRequestEvent)} without being a ghost.");
                return;
            }

            var response = new GhostWarpsResponseEvent(GetLocationNames().ToList(), GetPlayerWarps(entity.Uid));
            RaiseNetworkEvent(response, args.SenderSession.ConnectedClient);
        }

        private void OnGhostReturnToBodyRequest(GhostReturnToBodyRequest msg, EntitySessionEventArgs args)
        {
            var entity = args.SenderSession.AttachedEntity;

            if (entity == null ||
                !entity.TryGetComponent(out GhostComponent? ghost) ||
                !ghost.CanReturnToBody ||
                !entity.TryGetComponent(out ActorComponent? actor))
            {
                Logger.Warning($"User {args.SenderSession.Name} sent an invalid {nameof(GhostReturnToBodyRequest)}");
                return;
            }

            actor.PlayerSession.ContentData()!.Mind?.UnVisit();
        }

        private void OnGhostWarpToLocationRequest(GhostWarpToLocationRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity == null ||
                !args.SenderSession.AttachedEntity.TryGetComponent(out GhostComponent? ghost))
            {
                Logger.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Name} without being a ghost.");
                return;
            }

            if (FindLocation(msg.Name) is { } warp)
            {
                ghost.Owner.Transform.Coordinates = warp.Owner.Transform.Coordinates;
            }

            Logger.Warning($"User {args.SenderSession.Name} tried to warp to an invalid warp: {msg.Name}");
        }

        private void OnGhostWarpToTargetRequest(GhostWarpToTargetRequestEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession.AttachedEntity == null ||
                !args.SenderSession.AttachedEntity.TryGetComponent(out GhostComponent? ghost))
            {
                Logger.Warning($"User {args.SenderSession.Name} tried to warp to {msg.Target} without being a ghost.");
                return;
            }

            if (!EntityManager.TryGetEntity(msg.Target, out var entity))
            {
                Logger.Warning($"User {args.SenderSession.Name} tried to warp to an invalid entity id: {msg.Target}");
                return;
            }

            ghost.Owner.Transform.Coordinates = entity.Transform.Coordinates;
        }

        private void DeleteEntity(EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var entity)
                || entity.Deleted
                || entity.LifeStage == EntityLifeStage.Terminating)
                return;

            if (entity.TryGetComponent<MindComponent>(out var mind))
                mind.GhostOnShutdown = false;
            entity.Delete();
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

            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (player.AttachedEntity != null)
                {
                    players.Add(player.AttachedEntity.Uid, player.AttachedEntity.Name);
                }
            }

            players.Remove(except);

            return players;
        }
    }
}
