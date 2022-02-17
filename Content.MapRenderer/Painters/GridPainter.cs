using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters
{
    public sealed class GridPainter
    {
        private readonly EntityPainter _entityPainter;

        private readonly IEntityManager _cEntityManager;
        private readonly IMapManager _cMapManager;

        private readonly IEntityManager _sEntityManager;

        private readonly ConcurrentDictionary<GridId, List<EntityData>> _entities;

        public GridPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _entityPainter = new EntityPainter(client, server);

            _cEntityManager = client.ResolveDependency<IEntityManager>();
            _cMapManager = client.ResolveDependency<IMapManager>();

            _sEntityManager = server.ResolveDependency<IEntityManager>();

            _entities = GetEntities();
        }

        public void Run(Image gridCanvas, IMapGrid grid)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (!_entities.TryGetValue(grid.Index, out var entities))
            {
                Console.WriteLine($"No entities found on grid {grid.Index}");
                return;
            }

            _entityPainter.Run(gridCanvas, entities);
            Console.WriteLine($"{nameof(GridPainter)} painted grid {grid.Index} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private ConcurrentDictionary<GridId, List<EntityData>> GetEntities()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var components = new ConcurrentDictionary<GridId, List<EntityData>>();

            foreach (var entity in _sEntityManager.GetEntities())
            {
                if (!_sEntityManager.HasComponent<SharedSpriteComponent>(entity))
                {
                    continue;
                }

                var prototype = _sEntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype;
                if (prototype == null)
                {
                    continue;
                }

                if (!_cEntityManager.TryGetComponent(entity, out SpriteComponent sprite))
                {
                    throw new InvalidOperationException(
                        $"No sprite component found on an entity for which a server sprite component exists. Prototype id: {prototype.ID}");
                }

                var xOffset = 0;
                var yOffset = 0;
                var tileSize = 1;

                var transform = _sEntityManager.GetComponent<TransformComponent>(entity);
                if (_cMapManager.TryGetGrid(transform.GridID, out var grid))
                {
                    xOffset = (int) Math.Abs(grid.LocalBounds.Left);
                    yOffset = (int) Math.Abs(grid.LocalBounds.Bottom);
                    tileSize = grid.TileSize;
                }

                var position = transform.LocalPosition;
                var x = ((float) Math.Floor(position.X) + xOffset) * tileSize * TilePainter.TileImageSize;
                var y = ((float) Math.Floor(position.Y) + yOffset) * tileSize * TilePainter.TileImageSize;
                var data = new EntityData(sprite, x, y);

                components.GetOrAdd(transform.GridID, _ => new List<EntityData>()).Add(data);
            }

            Console.WriteLine($"Found {components.Values.Sum(l => l.Count)} entities on {components.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return components;
        }
    }
}
