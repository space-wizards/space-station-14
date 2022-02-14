using System.Buffers;
using System.Linq;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly SharedTransformSystem _transforms = default!;
        [Dependency] private readonly SpriteSystem _sprites = default!;

        private DecalOverlay _overlay = default!;
        public Dictionary<GridId, SortedDictionary<int, SortedDictionary<uint, Decal>>> DecalRenderIndex = new();
        private Dictionary<GridId, Dictionary<uint, int>> DecalZIndexIndex = new();

        public const float MaxDecalSize = 1f;

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, _transforms, _sprites, EntityManager, MapManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        }

        public void ToggleOverlay()
        {
            if (_overlayManager.HasOverlay<DecalOverlay>())
            {
                _overlayManager.RemoveOverlay(_overlay);
            }
            else
            {
                _overlayManager.AddOverlay(_overlay);
            }
        }

        private void OnGridRemoval(GridRemovalEvent ev)
        {
            DecalRenderIndex.Remove(ev.GridId);
            DecalZIndexIndex.Remove(ev.GridId);
        }

        private void OnGridInitialize(GridInitializeEvent ev)
        {
            DecalRenderIndex[ev.GridId] = new();
            DecalZIndexIndex[ev.GridId] = new();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlayManager.RemoveOverlay(_overlay);
        }

        protected override bool RemoveDecalHook(GridId gridId, uint uid)
        {
            RemoveDecalFromRenderIndex(gridId, uid);

            return base.RemoveDecalHook(gridId, uid);
        }

        private void RemoveDecalFromRenderIndex(GridId gridId, uint uid)
        {
            var zIndex = DecalZIndexIndex[gridId][uid];

            DecalRenderIndex[gridId][zIndex].Remove(uid);
            if (DecalRenderIndex[gridId][zIndex].Count == 0)
                DecalRenderIndex[gridId].Remove(zIndex);

            DecalZIndexIndex[gridId].Remove(uid);
        }

        public IEnumerable<Decal> GetDecals(GridId gridId, Box2Rotated worldBounds, TransformComponent xform)
        {
            var decals = new List<Decal>(64);
            // TODO: Need some way to do this better and also enlarge the viewport by the necessary amount.
            // Sprites also have this problem.

            // When maps support decals just take in an Euid instead
            var chunks = ChunkCollection(gridId);
            var localAABB = Transforms.GetInvWorldMatrix(xform).TransformBox(worldBounds);
            var enumerator = new ChunkIndicesEnumerator(localAABB, ChunkSize);

            while (enumerator.MoveNext(out var chunkIndex))
            {
                if (!chunks.TryGetValue(chunkIndex.Value, out var chunk)) continue;

                foreach (var (_, decal) in chunk)
                {
                    var decalAABB = new Box2(decal.Coordinates - MaxDecalSize, decal.Coordinates + MaxDecalSize);
                    if (!localAABB.Intersects(decalAABB)) continue;

                    decals.Add(decal);
                }
            }

            var indices = new int[decals.Count];

            for (var i = 0; i < decals.Count; i++)
            {
                indices[i] = i;
            }

            Array.Sort(indices, 0, decals.Count, new DecalComparer(decals));

            for (var i = 0; i < decals.Count; i++)
            {
                yield return decals[i];
            }
        }

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, gridChunks) in ev.Data)
            {
                foreach (var (indices, newChunkData) in gridChunks)
                {
                    var chunkCollection = ChunkCollection(gridId);
                    if (chunkCollection.TryGetValue(indices, out var chunk))
                    {
                        var removedUids = new HashSet<uint>(chunk.Keys);
                        removedUids.ExceptWith(newChunkData.Keys);
                        foreach (var removedUid in removedUids)
                        {
                            RemoveDecalFromRenderIndex(gridId, removedUid);
                        }
                    }
                    foreach (var (uid, decal) in newChunkData)
                    {
                        if(!DecalRenderIndex[gridId].ContainsKey(decal.ZIndex))
                            DecalRenderIndex[gridId][decal.ZIndex] = new();

                        if (DecalZIndexIndex.TryGetValue(gridId, out var values) && values.TryGetValue(uid, out var zIndex))
                        {
                            DecalRenderIndex[gridId][zIndex].Remove(uid);
                        }

                        DecalRenderIndex[gridId][decal.ZIndex][uid] = decal;
                        DecalZIndexIndex[gridId][uid] = decal.ZIndex;

                        ChunkIndex[gridId][uid] = indices;
                    }
                    chunkCollection[indices] = newChunkData;
                }
            }
        }

        private sealed class DecalComparer : IComparer<int>
        {
            private List<Decal> _decals;

            public DecalComparer(List<Decal> decals)
            {
                _decals = decals;
            }


            public int Compare(int x, int y)
            {
                var decalX = _decals[x];
                var decalY = _decals[y];

                return decalY.ZIndex.CompareTo(decalX.ZIndex);
            }
        }
    }
}
