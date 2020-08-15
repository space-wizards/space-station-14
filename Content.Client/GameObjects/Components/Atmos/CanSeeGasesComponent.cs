#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class CanSeeGasesComponent : SharedCanSeeGasesComponent
    {
        // This data never gets cleaned up unless we change entity.
        // The advantage is we never need to re-send the same data to the client.
        // The disadvantage is it could be more prone to memory leaks.
        // Could look at running some cleanup every minute or so just in case it starts ballooning
        private Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>> _tileData = 
            new Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>>();

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            // TODO: Move to the overlay component
            IOverlayManager overlayManager;
            
            switch (message)
            {
                case PlayerAttachedMsg _:
                    overlayManager = IoCManager.Resolve<IOverlayManager>();
                    if(!overlayManager.HasOverlay(nameof(GasTileOverlay)))
                        overlayManager.AddOverlay(new GasTileOverlay());
                    break;

                case PlayerDetachedMsg _:
                    _tileData.Clear();
                    overlayManager = IoCManager.Resolve<IOverlayManager>();
                    if(!overlayManager.HasOverlay(nameof(GasTileOverlay)))
                        overlayManager.RemoveOverlay(nameof(GasTileOverlay));
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case SharedGasTileOverlaySystem.GasOverlayMessage msg:
                    HandleGasOverlayMessage(msg);
                    break;
            }
        }

        // Slightly different to the server-side system version
        private GasOverlayChunk GetOrCreateChunk(GridId gridId, MapIndices indices)
        {
            if (!_tileData.TryGetValue(gridId, out var chunks))
            {
                chunks = new Dictionary<MapIndices, GasOverlayChunk>();
                _tileData[gridId] = chunks;
            }
            
            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(indices);

            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                chunk = new GasOverlayChunk(gridId, chunkIndices);
                chunks[chunkIndices] = chunk;
            }

            return chunk;
        }

        private void HandleGasOverlayMessage(SharedGasTileOverlaySystem.GasOverlayMessage message)
        {
            foreach (var (indices, data) in message.OverlayData)
            {
                var chunk = GetOrCreateChunk(message.GridId, indices);
                chunk.Update(data, indices);
            }
        }

        public SharedGasTileOverlaySystem.GasOverlayData? GetData(GridId gridId, MapIndices indices)
        {
            if (!_tileData.TryGetValue(gridId, out var chunks))
            {
                return null;
            }

            var chunkIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(indices);
            
            if (!chunks.TryGetValue(chunkIndices, out var chunk))
            {
                return null;
            }

            return chunk.GetData(indices);
        }
    }
}
