#nullable enable
using System.Drawing;
using Content.Server.GameObjects.Components.Pointing;
using Content.Shared.Input;
using Content.Shared.Interfaces;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PointingSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            EntityQuery = new TypeEntityQuery(typeof(PointingArrowComponent));

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.Point, new PointerInputCmdHandler(Point))
                .Register<PointingSystem>();
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (!entity.TryGetComponent(out PointingArrowComponent arrow))
                {
                    continue;
                }

                arrow.Update(frameTime);
            }
        }

        private bool Point(ICommonSession? session, GridCoordinates coords, EntityUid uid)
        {
            var player = session?.AttachedEntity;
            if (player == null)
            {
                return false;
            }

            if (!coords.InRange(_mapManager, player.Transform.GridPosition, 15))
            {
                player.PopupMessage(player, Loc.GetString("You can't reach there!"));
                return false;
            }

            string message;
            if (EntityManager.TryGetEntity(uid, out var pointed))
            {
                message = player == pointed
                    ? $"{player.Name} {Loc.GetString("points at {0:themself}", player)}"
                    : $"{player.Name} {Loc.GetString("points at {0:theName}", pointed)}";
            }
            else
            {
                var tileRef = _mapManager.GetGrid(coords.GridID).GetTileRef(coords);
                var tileDef = _tileDefinitionManager[tileRef.Tile.TypeId];

                message = $"{player.Name} {Loc.GetString("points at {0}", tileDef.DisplayName)}";
            }

            player.Transform.LocalRotation = new Angle(
                coords.ToMapPos(_mapManager) -
                player.Transform.MapPosition.Position);

            var viewers = _playerManager.GetPlayersInRange(player.Transform.GridPosition, 15);

            EntityManager.SpawnEntity("pointingarrow", coords);

            // TODO: FOV
            foreach (var viewer in viewers)
            {
                if (viewer.AttachedEntity == null)
                {
                    continue;
                }

                player.PopupMessage(viewer.AttachedEntity, message);
            }

            return true;
        }
    }
}
