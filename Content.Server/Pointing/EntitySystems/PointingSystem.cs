using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Pointing.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
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
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
        [Dependency] private readonly SharedMindSystem _minds = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ExamineSystemShared _examine = default!;

        private static readonly TimeSpan PointDelay = TimeSpan.FromSeconds(0.5f);

        /// <summary>
        ///     A dictionary of players to the last time that they
        ///     pointed at something.
        /// </summary>
        private readonly Dictionary<ICommonSession, TimeSpan> _pointers = new();

        private const float PointingRange = 15f;

        private void GetCompState(Entity<PointingArrowComponent> entity, ref ComponentGetState args)
        {
            args.State = new SharedPointingArrowComponentState
            {
                StartPosition = entity.Comp.StartPosition,
                EndTime = entity.Comp.EndTime
            };
        }

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
            var netSource = GetNetEntity(source);

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

                RaiseNetworkEvent(new PopupEntityEvent(message, PopupType.Small, netSource), viewerEntity);
            }

            _replay.RecordServerMessage(new PopupEntityEvent(viewerMessage, PopupType.Small, netSource));
        }

        public bool InRange(EntityUid pointer, EntityCoordinates coordinates)
        {
            if (HasComp<GhostComponent>(pointer))
            {
                return Transform(pointer).Coordinates.InRange(EntityManager, _transform, coordinates, 15);
            }
            else
            {
                return _examine.InRangeUnOccluded(pointer, coordinates, 15, predicate: e => e == pointer);
            }
        }

        public bool TryPoint(ICommonSession? session, EntityCoordinates coordsPointed, EntityUid pointed)
        {
            if (session?.AttachedEntity is not { } player)
            {
                Log.Warning($"Player {session} attempted to point without any attached entity");
                return false;
            }

            if (!coordsPointed.IsValid(EntityManager))
            {
                Log.Warning($"Player {ToPrettyString(player)} attempted to point at invalid coordinates: {coordsPointed}");
                return false;
            }

            if (_pointers.TryGetValue(session, out var lastTime) &&
                _gameTiming.CurTime < lastTime + PointDelay)
            {
                return false;
            }

            if (HasComp<PointingArrowComponent>(pointed))
            {
                // this is a pointing arrow. no pointing here...
                return false;
            }

            if (!CanPoint(player))
            {
                return false;
            }

            if (!InRange(player, coordsPointed))
            {
                _popup.PopupEntity(Loc.GetString("pointing-system-try-point-cannot-reach"), player, player);
                return false;
            }

            var mapCoordsPointed = coordsPointed.ToMap(EntityManager, _transform);
            _rotateToFaceSystem.TryFaceCoordinates(player, mapCoordsPointed.Position);

            var arrow = EntityManager.SpawnEntity("PointingArrow", coordsPointed);

            if (TryComp<PointingArrowComponent>(arrow, out var pointing))
            {
                if (TryComp(player, out TransformComponent? xformPlayer))
                    pointing.StartPosition = EntityCoordinates.FromMap(arrow, xformPlayer.Coordinates.ToMap(EntityManager, _transform), _transform).Position;

                pointing.EndTime = _gameTiming.CurTime + PointDuration;

                Dirty(arrow, pointing);
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
                _visibilitySystem.SetLayer(arrow, arrowVisibility, layer);
            }

            // Get players that are in range and whose visibility layer matches the arrow's.
            bool ViewerPredicate(ICommonSession playerSession)
            {
                if (!_minds.TryGetMind(playerSession, out _, out var mind) ||
                    mind.CurrentEntity is not { Valid: true } ent ||
                    !TryComp(ent, out EyeComponent? eyeComp) ||
                    (eyeComp.VisibilityMask & layer) == 0)
                    return false;

                return Transform(ent).MapPosition.InRange(Transform(player).MapPosition, PointingRange);
            }

            var viewers = Filter.Empty()
                .AddWhere(session1 => ViewerPredicate(session1))
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

                var ev = new AfterPointedAtEvent(pointed);
                RaiseLocalEvent(player, ref ev);
                var gotev = new AfterGotPointedAtEvent(player);
                RaiseLocalEvent(pointed, ref gotev);

                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):user} pointed at {ToPrettyString(pointed):target} {Transform(pointed).Coordinates}");
            }
            else
            {
                TileRef? tileRef = null;
                string? position = null;

                if (_mapManager.TryFindGridAt(mapCoordsPointed, out var gridUid, out var grid))
                {
                    position = $"EntId={gridUid} {grid.WorldToTile(mapCoordsPointed.Position)}";
                    tileRef = grid.GetTileRef(grid.WorldToTile(mapCoordsPointed.Position));
                }

                var tileDef = _tileDefinitionManager[tileRef?.Tile.TypeId ?? 0];

                var name = Loc.GetString(tileDef.Name);
                selfMessage = Loc.GetString("pointing-system-point-at-tile", ("tileName", name));

                viewerMessage = Loc.GetString("pointing-system-other-point-at-tile", ("otherName", playerName), ("tileName", name));

                _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(player):user} pointed at {name} {(position == null ? mapCoordsPointed : position)}");
            }

            _pointers[session] = _gameTiming.CurTime;

            SendMessage(player, viewers, pointed, selfMessage, viewerMessage, viewerPointedAtMessage);

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PointingArrowComponent, ComponentGetState>(GetCompState);

            SubscribeNetworkEvent<PointingAttemptEvent>(OnPointAttempt);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Point, new PointerInputCmdHandler(TryPoint))
                .Register<PointingSystem>();
        }

        private void OnPointAttempt(PointingAttemptEvent ev, EntitySessionEventArgs args)
        {
            var target = GetEntity(ev.Target);

            if (TryComp(target, out TransformComponent? xformTarget))
                TryPoint(args.SenderSession, xformTarget.Coordinates, target);
            else
                Log.Warning($"User {args.SenderSession} attempted to point at a non-existent entity uid: {ev.Target}");
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

            var query = AllEntityQuery<PointingArrowComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                Update((uid, component), currentTime);
            }
        }

        private void Update(Entity<PointingArrowComponent> pointing, TimeSpan currentTime)
        {
            // TODO: That pause PR
            var component = pointing.Comp;
            if (component.EndTime > currentTime)
                return;

            if (component.Rogue)
            {
                RemComp<PointingArrowComponent>(pointing);
                EnsureComp<RoguePointingArrowComponent>(pointing);
                return;
            }

            Del(pointing);
        }
    }
}
