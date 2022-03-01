using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Decals;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SixLabors.ImageSharp;
using static Robust.UnitTesting.RobustIntegrationTest;

namespace Content.MapRenderer.Painters
{
    public sealed class GridPainter
    {
        private readonly EntityPainter _entityPainter;
        private readonly DecalPainter _decalPainter;

        private readonly IEntityManager _cEntityManager;
        private readonly IMapManager _cMapManager;

        private readonly IEntityManager _sEntityManager;
        private readonly IMapManager _sMapManager;

        private readonly ConcurrentDictionary<GridId, List<EntityData>> _entities;
        private readonly Dictionary<GridId, List<DecalData>> _decals;

        public GridPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _entityPainter = new EntityPainter(client, server);
            _decalPainter = new DecalPainter(client, server);

            _cEntityManager = client.ResolveDependency<IEntityManager>();
            _cMapManager = client.ResolveDependency<IMapManager>();

            _sEntityManager = server.ResolveDependency<IEntityManager>();
            _sMapManager = server.ResolveDependency<IMapManager>();

            _entities = GetEntities();
            _decals = GetDecals();
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

            if (!_decals.TryGetValue(grid.Index, out var decals))
            {
                Console.WriteLine($"No decals found on grid {grid.Index}");
                return;
            }

            // Decals are always painted before entities.
            _decalPainter.Run(gridCanvas, decals);
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

                var transform = _sEntityManager.GetComponent<TransformComponent>(entity);
                if (_cMapManager.TryGetGrid(transform.GridID, out var grid))
                {
                    var position = transform.LocalPosition;

                    var (x, y) = TransformLocalPosition(position, grid);
                    var data = new EntityData(sprite, x, y);

                    components.GetOrAdd(transform.GridID, _ => new List<EntityData>()).Add(data);
                }
            }

            Console.WriteLine($"Found {components.Values.Sum(l => l.Count)} entities on {components.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return components;
        }

        private Dictionary<GridId, List<DecalData>> GetDecals()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var decals = new Dictionary<GridId, List<DecalData>>();

            foreach (var grid in _sMapManager.GetAllGrids())
            {
                // TODO this needs to use the client entity manager because the client
                // actually has the correct z-indices for decals for some reason when the server doesn't,
                // BUT can't do that yet because the client hasn't actually received everything yet
                // for some reason decal moment i guess.
                if (_sEntityManager.TryGetComponent<DecalGridComponent>(grid.GridEntityId, out var comp))
                {
                    foreach (var (_, list) in comp.ChunkCollection.ChunkCollection)
                    {
                        foreach (var (_, decal) in list)
                        {
                            var (x, y) = TransformLocalPosition(decal.Coordinates, grid);
                            decals.GetOrNew(grid.Index).Add(new DecalData(decal, x, y));
                        }
                    }
                }
            }

            Console.WriteLine($"Found {decals.Values.Sum(l => l.Count)} decals on {decals.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
            return decals;
        }

        private (float x, float y) TransformLocalPosition(Vector2 position, IMapGrid grid)
        {
            var xOffset = (int) Math.Abs(grid.LocalBounds.Left);
            var yOffset = (int) Math.Abs(grid.LocalBounds.Bottom);
            var tileSize = grid.TileSize;

            var x = ((float) Math.Floor(position.X) + xOffset) * tileSize * TilePainter.TileImageSize;
            var y = ((float) Math.Floor(position.Y) + yOffset) * tileSize * TilePainter.TileImageSize;

            return (x, y);
        }
    }
}
