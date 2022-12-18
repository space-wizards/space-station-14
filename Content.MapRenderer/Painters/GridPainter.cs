using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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

        private readonly ConcurrentDictionary<EntityUid, List<EntityData>> _entities;
        private readonly Dictionary<EntityUid, List<DecalData>> _decals;

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

        public void Run(Image gridCanvas, MapGridComponent grid)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (!_entities.TryGetValue(grid.Owner, out var entities))
            {
                Console.WriteLine($"No entities found on grid {grid.Owner}");
                return;
            }

            // Decals are always painted before entities, and are also optional.
            if (_decals.TryGetValue(grid.Owner, out var decals))
                _decalPainter.Run(gridCanvas, decals);


            _entityPainter.Run(gridCanvas, entities);
            Console.WriteLine($"{nameof(GridPainter)} painted grid {grid.Owner} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private ConcurrentDictionary<EntityUid, List<EntityData>> GetEntities()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var components = new ConcurrentDictionary<EntityUid, List<EntityData>>();

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

                if (!_cEntityManager.TryGetComponent(entity, out SpriteComponent? sprite))
                {
                    throw new InvalidOperationException(
                        $"No sprite component found on an entity for which a server sprite component exists. Prototype id: {prototype.ID}");
                }

                var transform = _sEntityManager.GetComponent<TransformComponent>(entity);
                if (_cMapManager.TryGetGrid(transform.GridUid, out var grid))
                {
                    var position = transform.LocalPosition;

                    var (x, y) = TransformLocalPosition(position, grid);
                    var data = new EntityData(sprite, x, y);

                    components.GetOrAdd(transform.GridUid.Value, _ => new List<EntityData>()).Add(data);
                }
            }

            Console.WriteLine($"Found {components.Values.Sum(l => l.Count)} entities on {components.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");

            return components;
        }

        private Dictionary<EntityUid, List<DecalData>> GetDecals()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var decals = new Dictionary<EntityUid, List<DecalData>>();

            foreach (var grid in _sMapManager.GetAllGrids())
            {
                // TODO this needs to use the client entity manager because the client
                // actually has the correct z-indices for decals for some reason when the server doesn't,
                // BUT can't do that yet because the client hasn't actually received everything yet
                // for some reason decal moment i guess.
                if (_sEntityManager.TryGetComponent<DecalGridComponent>(grid.Owner, out var comp))
                {
                    foreach (var chunk in comp.ChunkCollection.ChunkCollection.Values)
                    {
                        foreach (var decal in chunk.Decals.Values)
                        {
                            var (x, y) = TransformLocalPosition(decal.Coordinates, grid);
                            decals.GetOrNew(grid.Owner).Add(new DecalData(decal, x, y));
                        }
                    }
                }
            }

            Console.WriteLine($"Found {decals.Values.Sum(l => l.Count)} decals on {decals.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
            return decals;
        }

        private (float x, float y) TransformLocalPosition(Vector2 position, MapGridComponent grid)
        {
            var xOffset = (int) -grid.LocalAABB.Left;
            var yOffset = (int) -grid.LocalAABB.Bottom;
            var tileSize = grid.TileSize;

            var x = ((float) Math.Floor(position.X) + xOffset) * tileSize * TilePainter.TileImageSize;
            var y = ((float) Math.Floor(position.Y) + yOffset) * tileSize * TilePainter.TileImageSize;

            return (x, y);
        }
    }
}
