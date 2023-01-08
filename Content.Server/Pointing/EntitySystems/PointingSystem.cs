using System;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Server.Pointing.Components;
using Content.Server.Visible;
using Content.Shared.Bed.Sleep;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server.Pointing.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PointingSystem : SharedPointingSystem
    {
        [Dependency] private readonly IReplayRecordingManager _replay = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
        [Dependency] private readonly SharedMobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private static readonly TimeSpan PointDelay = TimeSpan.FromSeconds(0.5f);

        /// <summary>
        ///     A dictionary of players to the last time that they
        ///     pointed at something.
        /// </summary>
        private readonly Dictionary<ICommonSession, TimeSpan> _pointers = new();

        private const float PointingRange = 15f;

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.Disconnected)
            {
                return;
            }

            _pointers.Remove(e.Session);
        }

        // TODO: FOV
        private void SendMessage(EntityUid source, IEnumerable<ICommonSession> viewers, EntityUid pointed, string selfMessage,
            string viewerMessage, string? viewerPointedAtMessage = null)
        {
            foreach (var viewer in viewers)
            {
                if (viewer.AttachedEntity is not {Valid: true} viewerEntity)
                {
                    continue;
                }

                var message = viewerEntity == source
                    ? selfMessage
                    : viewerEntity == pointed && viewerPointedAtMessage != null
                        ? viewerPointedAtMessage
                        : viewerMessage;

                RaiseNetworkEvent(new PopupEntityEvent(message, PopupType.Small, source), viewerEntity);
            }

            _replay.QueueReplayMessage(new PopupEntityEvent(viewerMessage, PopupType.Small, source));
        }

        public bool InRange(EntityUid pointer, EntityCoordinates coordinates)
        {
            if (HasComp<GhostComponent>(pointer))
            {
                return Transform(pointer).Coordinates.InRange(EntityManager, coordinates, 15);
            }
            else
            {
                return pointer.InRangeUnOccluded(coordinates, 15, e => e == pointer);
            }
        }

        public bool TryPoint(ICommonSession? session, EntityCoordinates coords, EntityUid pointed)
        {
            if (session?.AttachedEntity is not { } player)
            {
                Logger.Warning($"Player {session} attempted to point without any attached entity");
                return false;
            }

            if (!coords.IsValid(EntityManager))
            {
                Logger.Warning($"Player {ToPrettyString(player)} attempted to point at invalid coordinates: {coords}");
                return false;
            }

            if (_pointers.TryGetValue(session!, out var lastTime) &&
                _gameTiming.CurTime < lastTime + PointDelay)
            {
                return false;
            }

            if (HasComp<PointingArrowComponent>(pointed))
            {
                // this is a pointing arrow. no pointing here...
                return false;
            }

            // Checking mob state directly instead of some action blocker, as many action blockers are blocked for
            // ghosts and there is no obvious choice for pointing.
            if (_mobState.IsIncapacitated(player))
            {
                return false;
            }

            if (HasComp<SleepingComponent>(player))
            {
                return false;
            }

            if (!InRange(player, coords))
            {
                _popup.PopupEntity(Loc.GetString("pointing-system-try-point-cannot-reach"), player, player);
                return false;
            }


            var mapCoords = coords.ToMap(EntityManager);
            _rotateToFaceSystem.TryFaceCoordinates(player, mapCoords.Position);

            var arrow = EntityManager.SpawnEntity("PointingArrow", coords);

            if (TryComp<PointingArrowComponent>(arrow, out var pointing))
            {
                pointing.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(4);
            }

            if (EntityQuery<PointingArrowAngeringComponent>().FirstOrDefault() != null)
            {
                if (TryComp<PointingArrowComponent>(arrow, out var pointingArrowComponent))
                {
                    pointingArrowComponent.Rogue = true;
                }
            }

            var layer = (int) VisibilityFlags.Normal;
            if (TryComp(player, out VisibilityComponent? playerVisibility))
            {
                var arrowVisibility = EntityManager.EnsureComponent<VisibilityComponent>(arrow);
                layer = playerVisibility.Layer;
                _visibilitySystem.SetLayer(arrowVisibility, layer);
            }

            // Get players that are in range and whose visibility layer matches the arrow's.
            bool ViewerPredicate(IPlayerSession playerSession)
            {
                if (playerSession.ContentData()?.Mind?.CurrentEntity is not {Valid: true} ent ||
                    !TryComp(ent, out EyeComponent? eyeComp) ||
                    (eyeComp.VisibilityMask & layer) == 0)
                    return false;

                return Transform(ent).MapPosition.InRange(Transform(player).MapPosition, PointingRange);
            }

            var viewers = Filter.Empty()
                .AddWhere(session1 => ViewerPredicate((IPlayerSession) session1))
                .Recipients;

            string selfMessage;
            string viewerMessage;
            string? viewerPointedAtMessage = null;
            var playerName = Identity.Entity(player, EntityManager);

            if (Exists(pointed))
            {
                var pointedName = Identity.Entity(pointed, EntityManager);

                selfMessage = player == pointed
                    ? Loc.GetString("pointing-system-point-at-self")
                    : Loc.GetString("pointing-system-point-at-other", ("other", pointedName));

                viewerMessage = player == pointed
                    ? Loc.GetString("pointing-system-point-at-self-others", ("otherName", playerName), ("other", playerName))
                    : Loc.GetString("pointing-system-point-at-other-others", ("otherName", playerName), ("other", pointedName));

                viewerPointedAtMessage = Loc.GetString("pointing-system-point-at-you-other", ("otherName", playerName));

                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):user} pointed at {ToPrettyString(pointed):target} {Transform(pointed).Coordinates}");
            }
            else
            {
                TileRef? tileRef = null;
                string? position = null;

                if (_mapManager.TryFindGridAt(mapCoords, out var grid))
                {
                    position = $"EntId={grid.Owner} {grid.WorldToTile(mapCoords.Position)}";
                    tileRef = grid.GetTileRef(grid.WorldToTile(mapCoords.Position));
                }

                var tileDef = _tileDefinitionManager[tileRef?.Tile.TypeId ?? 0];

                var name = Loc.GetString(tileDef.Name);
                selfMessage = Loc.GetString("pointing-system-point-at-tile", ("tileName", name));

                viewerMessage = Loc.GetString("pointing-system-other-point-at-tile", ("otherName", playerName), ("tileName", tileDef.Name));

                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):user} pointed at {name} {(position == null ? mapCoords : position)}");
            }

            _pointers[session] = _gameTiming.CurTime;

            SendMessage(player, viewers, pointed, selfMessage, viewerMessage, viewerPointedAtMessage);

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<PointingAttemptEvent>(OnPointAttempt);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Point, new PointerInputCmdHandler(TryPoint))
                .Register<PointingSystem>();
        }

        private void OnPointAttempt(PointingAttemptEvent ev, EntitySessionEventArgs args)
        {
            if (TryComp(ev.Target, out TransformComponent? xform))
                TryPoint(args.SenderSession, xform.Coordinates, ev.Target);
            else
                Logger.Warning($"User {args.SenderSession} attempted to point at a non-existent entity uid: {ev.Target}");
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _pointers.Clear();
        }

        public override void Update(float frameTime)
        {
            var currentTime = _gameTiming.CurTime;

            foreach (var component in EntityQuery<PointingArrowComponent>(true))
            {
                Update(component, currentTime);
            }
        }

        private void Update(PointingArrowComponent component, TimeSpan currentTime)
        {
            // TODO: That pause PR
            if (component.EndTime > currentTime)
                return;

            if (component.Rogue)
            {
                RemComp<PointingArrowComponent>(component.Owner);
                EnsureComp<RoguePointingArrowComponent>(component.Owner);
                return;
            }

            Del(component.Owner);
        }
    }
}
