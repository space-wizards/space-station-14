using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.Decals
{
    public class SharedDecalSystem : EntitySystem
    {
        [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        protected readonly Dictionary<GridId, ChunkCollection<Dictionary<uint, Decal>>> _chunkCollections = new();
        protected readonly Dictionary<uint, (GridId gridId, Vector2i chunkIndices)> _chunkIndex = new();

        private const int ChunkSize = 32;

        private float _viewSize;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
            _viewSize = _configurationManager.GetCVar(CVars.NetMaxUpdateRange)*2f;
            _configurationManager.OnValueChanged(CVars.NetMaxUpdateRange, f => _viewSize = f*2f);
        }

        private void OnGridRemoval(GridRemovalEvent msg)
        {
            _chunkCollections.Remove(msg.GridId);
        }

        private void OnGridInitialize(GridInitializeEvent msg)
        {
            _chunkCollections[msg.GridId] = new ChunkCollection<Dictionary<uint, Decal>>(new Vector2i(ChunkSize, ChunkSize));
        }

        protected void DirtyChunk((GridId, Vector2i) values) => DirtyChunk(values.Item1, values.Item2);
        protected virtual void DirtyChunk(GridId id, Vector2i chunkIndices) {}

        protected void RegisterDecal(uint uid, Decal decal, GridId gridId)
        {
            var chunkIndices = _chunkCollections[gridId].GetIndices(decal.Coordinates);
            _chunkCollections[gridId].GetChunk(chunkIndices).Add(uid, decal);
            _chunkIndex.Add(uid, (gridId, chunkIndices));
            DirtyChunk(gridId, chunkIndices);
        }

        protected bool RemoveDecalInternal(uint uid)
        {
            if (!_chunkIndex.TryGetValue(uid, out var values))
            {
                return false;
            }

            if (!_chunkCollections[values.gridId].GetChunk(values.chunkIndices).Remove(uid))
            {
                return false;
            }

            _chunkIndex.Remove(uid);
            return true;
        }

        private (Box2 view, MapId mapId) CalcViewBounds(in EntityUid euid)
        {
            var xform = EntityManager.GetComponent<ITransformComponent>(euid);

            var view = Box2.UnitCentered.Scale(_viewSize).Translated(xform.WorldPosition);
            var map = xform.MapID;

            return (view, map);
        }

        protected Dictionary<GridId, HashSet<Vector2i>> GetChunksForViewers(HashSet<EntityUid> viewers)
        {
            var chunks = new Dictionary<GridId, HashSet<Vector2i>>();
            foreach (var viewerUid in viewers)
            {
                var (bounds, mapId) = CalcViewBounds(viewerUid);
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, bounds, approx: true))
                {
                    if (!chunks.ContainsKey(grid.Index))
                        chunks[grid.Index] = new();

                    var enumerator = new ChunkIndicesEnumerator(grid.InvWorldMatrix.TransformBox(bounds), ChunkSize);
                    while (enumerator.MoveNext(out var indices))
                    {
                        chunks[grid.Index].Add(indices.Value);
                    }
                }
            }
            return chunks;
        }
    }

    internal struct ChunkIndicesEnumerator
    {
        private Vector2i _chunkLB;
        private Vector2i _chunkRT;

        private int _xIndex;
        private int _yIndex;

        internal ChunkIndicesEnumerator(Box2 localAABB, int chunkSize)
        {
            _chunkLB = new Vector2i((int)Math.Floor(localAABB.Left / chunkSize), (int)Math.Floor(localAABB.Bottom / chunkSize));
            _chunkRT = new Vector2i((int)Math.Floor(localAABB.Right / chunkSize), (int)Math.Floor(localAABB.Top / chunkSize));

            _xIndex = _chunkLB.X;
            _yIndex = _chunkLB.Y;
        }

        public bool MoveNext([NotNullWhen(true)] out Vector2i? indices)
        {
            if (_yIndex > _chunkRT.Y)
            {
                _yIndex = _chunkLB.Y;
                _xIndex += 1;
            }

            for (var x = _xIndex; x <= _chunkRT.X; x++)
            {
                for (var y = _yIndex; y <= _chunkRT.Y; y++)
                {
                    indices = new Vector2i(x, y);
                    _xIndex = x;
                    _yIndex = y + 1;
                    return true;
                }

                _yIndex = _chunkLB.Y;
            }

            indices = null;
            return false;
        }
    }

    public class ChunkCollection<T> where T : new()
    {
        private readonly Dictionary<Vector2i, T> _chunks = new();
        private readonly Vector2i _chunkSize;

        public ChunkCollection(Vector2i chunkSize)
        {
            _chunkSize = chunkSize;
        }

        public Vector2i GetIndices(Vector2 a)
        {
            return new ((int) Math.Floor(a.X / _chunkSize.X), (int) Math.Floor(a.Y / _chunkSize.Y));
        }

        public T GetChunk(Vector2i indices)
        {
            if (_chunks.TryGetValue(indices, out var chunk))
                return chunk;

            return _chunks[indices] = new T();
        }

        public IEnumerable<T> GetChunksForArea(Box2 area)
        {
            var coordinates = new HashSet<Vector2i>();

            var bottomRight = GetIndices(area.BottomRight);
            var topLeft = GetIndices(area.TopLeft);

            for (var x = 0; x < bottomRight.X - topLeft.X; x++)
            {
                for (var y = 0; y < topLeft.Y - bottomRight.Y; y++)
                {
                    coordinates.Add(new Vector2i(x, y));
                }
            }

            foreach (var indices in coordinates)
            {
                yield return GetChunk(indices);
            }
        }
    }

}
