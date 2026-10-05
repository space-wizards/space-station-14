using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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

        private readonly IEntityManager _sEntityManager;

        private readonly ConcurrentDictionary<EntityUid, List<EntityData>> _entities;
        private readonly Dictionary<EntityUid, List<DecalData>> _decals;

        public GridPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _entityPainter = new EntityPainter(client, server);
            _decalPainter = new DecalPainter(client, server);

            _cEntityManager = client.ResolveDependency<IEntityManager>();

            _sEntityManager = server.ResolveDependency<IEntityManager>();

            _entities = GetEntities();
            _decals = GetDecals();
        }

        public void Run(Image gridCanvas, EntityUid gridUid, MapGridComponent grid, Vector2 customOffset = default)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (!_entities.TryGetValue(gridUid, out var entities))
            {
                Console.WriteLine($"No entities found on grid {gridUid}");
                return;
            }

            // Decals are always painted before entities, and are also optional.
            if (_decals.TryGetValue(gridUid, out var decals))
                _decalPainter.Run(gridCanvas, CollectionsMarshal.AsSpan(decals), customOffset);


            _entityPainter.Run(gridCanvas, entities, customOffset);
            Console.WriteLine($"{nameof(GridPainter)} painted grid {gridUid} in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        private ConcurrentDictionary<EntityUid, List<EntityData>> GetEntities()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var components = new ConcurrentDictionary<EntityUid, List<EntityData>>();

            foreach (var serverEntity in _sEntityManager.GetEntities())
            {
                var clientEntity = _cEntityManager.GetEntity(_sEntityManager.GetNetEntity(serverEntity));
                if (!_cEntityManager.TryGetComponent(clientEntity, out SpriteComponent? sprite))
                {
                    continue;
                }

                var prototype = _sEntityManager.GetComponent<MetaDataComponent>(serverEntity).EntityPrototype;
                if (prototype == null)
                {
                    continue;
                }

                var transform = _sEntityManager.GetComponent<TransformComponent>(serverEntity);
                if (_sEntityManager.TryGetComponent(transform.GridUid, out MapGridComponent? grid))
                {
                    var position = transform.LocalPosition;

                    var (x, y) = TransformLocalPosition(position, grid);
                    var data = new EntityData(serverEntity, sprite, x, y);

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
            var query = _sEntityManager.AllEntityQueryEnumerator<MapGridComponent>();
            var chunkEntities = _sEntityManager.System<ChunkEntitySystem>();
            var decalChunkQuery = _sEntityManager.GetEntityQuery<DecalChunkComponent>();

            while (query.MoveNext(out var uid, out var grid))
            {
                foreach (var chunk in chunkEntities.GetChunks(uid))
                {
                    if (!decalChunkQuery.TryGetComponent(chunk.Owner, out var decalComp))
                    {
                        continue;
                    }

                    foreach (var (id, decal) in decalComp.Decals)
                    {
                        var (x, y) = TransformLocalPosition(decal.Coordinates, grid);
                        var index = new DecalIndex(chunk.Comp.Chunk, id);
                        decals.GetOrNew(uid).Add(new DecalData(index, decal, x, y));
                    }
                }
            }

            Console.WriteLine($"Found {decals.Values.Sum(l => l.Count)} decals on {decals.Count} grids in {(int) stopwatch.Elapsed.TotalMilliseconds} ms");
            return decals;
        }

        private static (float x, float y) TransformLocalPosition(Vector2 position, MapGridComponent grid)
        {
            var xOffset = (int) -grid.LocalAABB.Left;
            var yOffset = (int) -grid.LocalAABB.Bottom;
            var tileSize = grid.TileSize;

            var x = (position.X + xOffset) * tileSize * TilePainter.TileImageSize;
            var y = (position.Y + yOffset) * tileSize * TilePainter.TileImageSize;

            return (x, y);
        }
    }
}
