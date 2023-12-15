using Content.Client.Atmos.Overlays;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;

namespace Content.Client.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;
        [Dependency] private readonly SpriteSystem _spriteSys = default!;

        private GasTileOverlay _overlay = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<GasOverlayUpdateEvent>(HandleGasOverlayUpdate);
            SubscribeLocalEvent<GasTileOverlayComponent, ComponentHandleState>(OnHandleState);

            _overlay = new GasTileOverlay(this, EntityManager, _resourceCache, ProtoMan, _spriteSys);
            _overlayMan.AddOverlay(_overlay);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _overlayMan.RemoveOverlay<GasTileOverlay>();
        }

        private void OnHandleState(EntityUid gridUid, GasTileOverlayComponent comp, ref ComponentHandleState args)
        {
            if (args.Current is not GasTileOverlayState state)
                return;

            // is this a delta or full state?
            if (!state.FullState)
            {
                foreach (var index in comp.Chunks.Keys)
                {
                    if (!state.AllChunks!.Contains(index))
                        comp.Chunks.Remove(index);
                }
            }
            else
            {
                foreach (var index in comp.Chunks.Keys)
                {
                    if (!state.Chunks.ContainsKey(index))
                        comp.Chunks.Remove(index);
                }
            }

            foreach (var (index, data) in state.Chunks)
            {
                comp.Chunks[index] = data;
            }
        }

        private void HandleGasOverlayUpdate(GasOverlayUpdateEvent ev)
        {
            foreach (var (nent, removedIndicies) in ev.RemovedChunks)
            {
                var grid = GetEntity(nent);

                if (!TryComp(grid, out GasTileOverlayComponent? comp))
                    continue;

                foreach (var index in removedIndicies)
                {
                    comp.Chunks.Remove(index);
                }
            }

            foreach (var (nent, gridData) in ev.UpdatedChunks)
            {
                var grid = GetEntity(nent);

                if (!TryComp(grid, out GasTileOverlayComponent? comp))
                    continue;

                foreach (var chunkData in gridData)
                {
                    comp.Chunks[chunkData.Index] = chunkData;
                }
            }
        }
    }
}
