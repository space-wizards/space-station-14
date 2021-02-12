#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Localization;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.Map;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Random;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Content.Server.Utility;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using Robust.Shared.Physics;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Maths;
using Robust.Shared.GameObjects.Components;

namespace Content.Server.StationEvents
{
    class SpiderInfestation : StationEvent
    {
        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IRobustRandom _robustRandom = default!;

        private IMapManager? _mapManager;

        public override string Name => "SpiderInfestation";

        public override string StartAnnouncement => Loc.GetString(
            "Unidentified lifesigns detected coming aboard station. Secure any exterior access, including ducting and ventilation.");

        protected override float StartAfter => 20.0f;

        private int _spidersMax = 4;
        public override void Announce()
        {
            base.Announce();
            EndAfter = 30 + StartAfter;
        }

        public override void Startup()
        {
            base.Startup();

            var pauseManager = IoCManager.Resolve<IPauseManager>();
            var gameTicker = IoCManager.Resolve<IGameTicker>();
            _mapManager = IoCManager.Resolve<IMapManager>();
            var defaultGrid = _mapManager.GetGrid(gameTicker.DefaultGridId);

            if (pauseManager.IsGridPaused(defaultGrid))
                return;

            var spidersCounter = _robustRandom.Next(_spidersMax) + 1;

            for (var i = 0; i < spidersCounter; i++)
            {
                SpawnSpider(defaultGrid);
            }
        }

        private void SpawnSpider(IMapGrid mapGrid)
        {
            if (!TryFindRandomGrid(mapGrid, out var coordinates))
            {
                try
                {
                    SpawnSpider(mapGrid);
                    return;
                }
                catch (StackOverflowException e)
                {
                    Logger.Log(LogLevel.Error, "Can't find good place for spider");
                    return;
                }
            }

        }

        public bool TryFindRandomGrid(IMapGrid mapGrid, out EntityCoordinates coordinates)
        {
            if (!mapGrid.Index.IsValid() || _mapManager == null)
            {
                coordinates = default;
                return false;
            }

            var randomX = _robustRandom.Next((int) mapGrid.WorldBounds.Left, (int) mapGrid.WorldBounds.Right);
            var randomY = _robustRandom.Next((int) mapGrid.WorldBounds.Bottom, (int) mapGrid.WorldBounds.Top);

            coordinates = new EntityCoordinates(mapGrid.GridEntityId, randomX, randomY);

            // TODO: Need to get valid tiles? (maybe just move right if the tile we chose is invalid?)
            if (!coordinates.IsValid(_entityManager))
            {
                coordinates = default;
                return false;
            }

            var tile = mapGrid.GetTileRef(coordinates);

            var tileManager = IoCManager.Resolve<ITileDefinitionManager>();

            var tiles = tileManager["underplating"];
            if (!tile.Tile.TypeId.Equals(tiles.TileId)) // it works probably
            {
                return false;
            }

            var physManager = IoCManager.Resolve<IPhysicsManager>();
            if (physManager.GetCollidingEntities(_mapManager.DefaultMap, new Box2(new Vector2(randomX, randomY), new Vector2((float)randomX + 0.5f, (float)randomY + 0.5f))) != null)
            { // Legit? Spawn bug or something?
                return false;
            }

            var spider = _entityManager.SpawnEntity("GiantSpiderMob_Content", coordinates);

            return true;
        }
    }
}
