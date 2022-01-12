using System;
using System.Collections.Generic;
using Content.Server.Ghost.Components;
using Content.Server.Players;
using Content.Server.Pointing.Components;
using Content.Server.Visible;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Pointing.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PointingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
        [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;

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

                source.PopupMessage(viewerEntity, message);
            }
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
            var mapCoords = coords.ToMap(EntityManager);
            if ((session as IPlayerSession)?.ContentData()?.Mind?.CurrentEntity is not { } player)
            {
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
            if (TryComp(player, out MobStateComponent? mob) && mob.IsIncapacitated())
            {
                return false;
            }

            if (!InRange(player, coords))
            {
                player.PopupMessage(Loc.GetString("pointing-system-try-point-cannot-reach"));
                return false;
            }

            _rotateToFaceSystem.TryFaceCoordinates(player, mapCoords.Position);

            var arrow = EntityManager.SpawnEntity("pointingarrow", mapCoords);

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
            var playerName = Name(player);

            if (Exists(pointed))
            {
                var pointedName = Name(pointed);

                selfMessage = player == pointed
                    ? Loc.GetString("pointing-system-point-at-self")
                    : Loc.GetString("pointing-system-point-at-other", ("other", pointedName));

                viewerMessage = player == pointed
                    ? Loc.GetString("pointing-system-point-at-self-others", ("otherName", playerName), ("other", playerName))
                    : Loc.GetString("pointing-system-point-at-other-others", ("otherName", playerName), ("other", pointedName));

                viewerPointedAtMessage = Loc.GetString("pointing-system-point-at-you-other", ("otherName", playerName));
            }
            else
            {
                TileRef? tileRef = null;

                if (_mapManager.TryFindGridAt(mapCoords, out var grid))
                {
                    tileRef = grid.GetTileRef(grid.WorldToTile(mapCoords.Position));
                }

                var tileDef = _tileDefinitionManager[tileRef?.Tile.TypeId ?? 0];

                selfMessage = Loc.GetString("pointing-system-point-at-tile", ("tileName", tileDef.Name));

                viewerMessage = Loc.GetString("pointing-system-other-point-at-tile", ("otherName", playerName), ("tileName", tileDef.Name));
            }

            _pointers[session] = _gameTiming.CurTime;

            SendMessage(player, viewers, pointed, selfMessage, viewerMessage, viewerPointedAtMessage);

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GetOtherVerbsEvent>(AddPointingVerb);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Point, new PointerInputCmdHandler(TryPoint))
                .Register<PointingSystem>();
        }

        private void AddPointingVerb(GetOtherVerbsEvent args)
        {
            if (args.Hands == null)
                return;

            //Check if the object is already being pointed at
            if (HasComp<PointingArrowComponent>(args.Target))
                return;

            var transform = Transform(args.Target);

            if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor)  ||
                !InRange(args.User, transform.Coordinates))
                return;

            Verb verb = new();
            verb.Text = Loc.GetString("pointing-verb-get-data-text");
            verb.IconTexture = "/Textures/Interface/VerbIcons/point.svg.192dpi.png";
            verb.Act = () => TryPoint(actor.PlayerSession, transform.Coordinates, args.Target);
            args.Verbs.Add(verb);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _pointers.Clear();
        }

        public override void Update(float frameTime)
        {
            foreach (var component in EntityManager.EntityQuery<PointingArrowComponent>())
            {
                component.Update(frameTime);
            }
        }
    }
}
