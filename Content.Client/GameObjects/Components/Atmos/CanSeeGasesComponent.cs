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

        /// <summary>
        ///     Remove chunks that are out of out range to save memory.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void RemoveOutOfRange()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            
            var entityTile = mapManager
                .GetGrid(Owner.Transform.GridID)
                .GetTileRef(Owner.Transform.GridPosition)
                .GridIndices;
            
            var worldBounds = Box2.CenteredAround(Owner.Transform.WorldPosition,
                new Vector2(20.0f, 20.0f));

            var toRemove = new List<GasOverlayChunk>();

            foreach (var grid in mapManager.FindGridsIntersecting(Owner.Transform.MapID, worldBounds))
            {
                if (!_tileData.TryGetValue(grid.Index, out var chunks)) continue;

                foreach (var (indices, chunk) in chunks)
                {
                    // Am I dumb, this feels wrong.
                    // Anyway, if the chunk's to the left of us there could be the very edge of it in range so need to consider
                    // Chunk sizes when determining pruning
                    var xDiff = indices.X - entityTile.X;
                    var yDiff = indices.Y - entityTile.Y;

                    if (xDiff > 0 && xDiff > SharedGasTileOverlaySystem.UpdateRange || xDiff < 0 && Math.Abs(xDiff + SharedGasTileOverlaySystem.ChunkSize) > SharedGasTileOverlaySystem.UpdateRange 
                     || yDiff > 0 && yDiff > SharedGasTileOverlaySystem.UpdateRange || yDiff < 0 && Math.Abs(yDiff + SharedGasTileOverlaySystem.ChunkSize) > SharedGasTileOverlaySystem.UpdateRange)
                    {
                        toRemove.Add(chunk);
                        continue;
                    }
                }

                foreach (var chunk in toRemove)
                {
                    chunks.Remove(chunk.MapIndices);
                }
                
                toRemove.Clear();
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
