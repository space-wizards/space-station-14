using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
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
        private readonly IMapManager _sMapManager;

        private readonly ConcurrentDictionary<EntityUid, List<EntityData>> _entities;
        private readonly Dictionary<EntityUid, List<DecalData>> _decals;

        public GridPainter(ClientIntegrationInstance client, ServerIntegrationInstance server)
        {
            _entityPainter = new EntityPainter(client, server);
            _decalPainter = new DecalPainter(client, server);

            _cEntityManager = client.ResolveDependency<IEntityManager>();

            _sEntityManager = server.ResolveDependency<IEntityManager>();
            _sMapManager = server.ResolveDependency<IMapManager>();

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

            while (query.MoveNext(out var uid, out var grid))
            {
                // TODO this needs to use the client entity manager because the client
                // actually has the correct z-indices for decals for some reason when the server doesn't,
                // BUT can't do that yet because the client hasn't actually received everything yet
                // for some reason decal moment i guess.
                if (_sEntityManager.TryGetComponent<DecalGridComponent>(uid, out var comp))
                {
                    foreach (var chunk in comp.ChunkCollection.ChunkCollection.Values)
                    {
                        foreach (var decal in chunk.Decals.Values)
                        {
                            var (x, y) = TransformLocalPosition(decal.Coordinates, grid);
                            decals.GetOrNew(uid).Add(new DecalData(decal, x, y));
                        }
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
