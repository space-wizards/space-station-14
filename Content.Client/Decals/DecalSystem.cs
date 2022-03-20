using System.Linq;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Decals
{
    public sealed class DecalSystem : SharedDecalSystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly SharedTransformSystem _transforms = default!;
        [Dependency] private readonly SpriteSystem _sprites = default!;

        private DecalOverlay _overlay = default!;
        public Dictionary<GridId, SortedDictionary<int, SortedDictionary<uint, Decal>>> DecalRenderIndex = new();
        private Dictionary<GridId, Dictionary<uint, int>> DecalZIndexIndex = new();

        public override void Initialize()
        {
            base.Initialize();

            _overlay = new DecalOverlay(this, _transforms, _sprites, EntityManager, MapManager, PrototypeManager);
            _overlayManager.AddOverlay(_overlay);

            SubscribeNetworkEvent<DecalChunkUpdateEvent>(OnChunkUpdate);
            SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var localPlayer = IoCManager.Resolve<IPlayerManager>().LocalPlayer;
            var ent = localPlayer?.ControlledEntity;

            if (localPlayer == null || ent == null) return;

            // var viewers = localPlayer.Session.ViewSubscriptions.ToHashSet();
            var viewers = new HashSet<EntityUid>(1) { ent.Value };

            var chunksInRange = GetChunksForViewers(viewers);

            // If we ever spawn a billion grids then this may be a problem but eh.
            foreach (var comp in EntityQuery<DecalGridComponent>(true))
            {
                var gridId = comp.Owner;
                var toRemove = new RemQueue<Vector2i>();

                // Kill the grid
                if (!chunksInRange.TryGetValue(_mapManager.GetGridEuid(comp.Owner), out var knownChunks))
                {
                    foreach (var (index, _) in comp.ChunkCollection.ChunkCollection)
                    {
                        toRemove.Add(index);
                    }
                }
                else
                {
                    foreach (var (index, _) in comp.ChunkCollection.ChunkCollection)
                    {
                        if (knownChunks.Contains(index)) continue;

                        toRemove.Add(index);
                    }
                }

                foreach (var index in toRemove)
                {
                    var chunk = comp.ChunkCollection.ChunkCollection[index];

                    foreach (var (id, _) in chunk)
                    {
                        RemoveDecalFromRenderIndex(gridId, id);
                    }

                    comp.ChunkCollection.ChunkCollection.Remove(index);
                }
            }
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

        private void OnChunkUpdate(DecalChunkUpdateEvent ev)
        {
            foreach (var (gridId, gridChunks) in ev.Data)
            {
                var chunkCollection = ChunkCollection(gridId);

                foreach (var (indices, newChunkData) in gridChunks)
                {
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
                        if (!DecalRenderIndex[gridId].ContainsKey(decal.ZIndex))
                            DecalRenderIndex[gridId][decal.ZIndex] = new();

                        if (DecalZIndexIndex.TryGetValue(gridId, out var values) &&
                            values.TryGetValue(uid, out var zIndex))
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
    }
}
