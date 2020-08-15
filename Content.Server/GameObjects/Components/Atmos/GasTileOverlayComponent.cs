#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Atmos
{
    /// <summary>
    ///     This will store the relevant data for the GasTileOverlaySystem.
    ///     Especially the nearby chunks to this player so we know when we need to send new ones as they move around.
    /// </summary>
    [RegisterComponent]
    public sealed class CanSeeGasesComponent : SharedCanSeeGasesComponent
    {
        private Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>> _knownChunks = 
            new Dictionary<GridId, Dictionary<MapIndices, GasOverlayChunk>>();
        
        /// <summary>
        ///     We'll store the last time we sent a particular chunk to a client so if they move around
        ///     we can check if they need the chunks sent.
        /// </summary>
        private Dictionary<GasOverlayChunk, TimeSpan> _lastSent = new Dictionary<GasOverlayChunk, TimeSpan>();

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);
            switch (message)
            {
                case PlayerAttachedMsg _:
                    SyncClient();
                    break;
            }
        }

        public IEnumerable<GasOverlayChunk> GetKnownChunks()
        {
            foreach (var (_, chunks) in _knownChunks)
            {
                foreach (var (_, chunk) in chunks)
                {
                    yield return chunk;
                }
            }
        }

        /// <summary>
        ///     Adds this chunk for monitoring for invalidity.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="chunk"></param>
        public void AddChunk(TimeSpan currentTime, GasOverlayChunk chunk)
        {
            if (!_knownChunks.TryGetValue(chunk.GridIndices, out var chunks))
            {
                chunks = new Dictionary<MapIndices, GasOverlayChunk>();
                _knownChunks[chunk.GridIndices] = chunks;
            }

            if (!chunks.ContainsKey(chunk.MapIndices))
            {
                chunks[chunk.MapIndices] = chunk;
            }

            // If we've already sent this chunk to the client don't bother re-sending.
            if (_lastSent.TryGetValue(chunk, out var chunkLastSent) && chunkLastSent >= chunk.LastUpdate)
            {
                return;
            } 

            if (Owner.TryGetComponent(out IActorComponent actorComponent) && 
                actorComponent.playerSession.ConnectedClient.IsConnected && 
                TryChunkToMessage(chunk, out var message))
            {
                chunkLastSent = currentTime;
                _lastSent[chunk] = chunkLastSent;
                SendNetworkMessage(message, actorComponent.playerSession.ConnectedClient);
            }
        }

        /// <summary>
        ///     Send data to the client (if applicable).
        /// </summary>
        /// <param name="gridId"></param>
        /// <param name="data"></param>
        public void UpdateClient(GridId gridId, List<(MapIndices, SharedGasTileOverlaySystem.GasOverlayData)> data)
        {
            if (Owner.TryGetComponent(out IActorComponent actorComponent) && actorComponent.playerSession.ConnectedClient.IsConnected)
            {
                SendNetworkMessage(new SharedGasTileOverlaySystem.GasOverlayMessage(gridId, data), actorComponent.playerSession.ConnectedClient);
            }
        }

        /// <summary>
        ///     Retrieve a whole chunk as a message, only getting the relevant tiles for the gas overlay.
        ///     Don't use this for UpdateClient as it won't account for tiles that had an overlay texture that's been removed.
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool TryChunkToMessage(GasOverlayChunk chunk, [NotNullWhen(true)] out SharedGasTileOverlaySystem.GasOverlayMessage? message)
        {
            // Chunk data should already be up to date.
            // Only send relevant tiles to client.
            
            var tileData = new List<(MapIndices, SharedGasTileOverlaySystem.GasOverlayData)>();

            for (var x = 0; x < SharedGasTileOverlaySystem.ChunkSize; x++)
            {
                for (var y = 0; y < SharedGasTileOverlaySystem.ChunkSize; y++)
                {
                    // TODO: Check could be more robust I think.
                    var data = chunk.TileData[x, y];
                    if ((data.Gas == null || data.Gas.Length == 0) && data.FireState == 0 && data.FireTemperature == 0.0f)
                    {
                        continue;
                    }

                    var indices = new MapIndices(chunk.MapIndices.X + x, chunk.MapIndices.Y + y);
                    tileData.Add((indices, data));
                }
            }

            if (tileData.Count == 0)
            {
                message = null;
                return false;
            }
            
            message = new SharedGasTileOverlaySystem.GasOverlayMessage(chunk.GridIndices, tileData);
            return true;
        }

        /// <summary>
        ///     Remove this chunk from our known chunks.
        ///     This means if an SyncClient() is called then it won't be sent.
        /// </summary>
        /// <param name="chunk"></param>
        public void RemoveChunk(GasOverlayChunk chunk)
        {
            // We'll leave our last-sent time so we don't need to re-send the data if possible.
            if (!_knownChunks.TryGetValue(chunk.GridIndices, out var chunks))
            {
                return;
            }

            if (chunks.ContainsKey(chunk.MapIndices))
            {
                chunks.Remove(chunk.MapIndices);
            }
        }

        /// <summary>
        ///     Sends all gas tile overlay data to the client that we know about.
        /// </summary>
        public void SyncClient()
        {
            if (!Owner.TryGetComponent(out IActorComponent actorComponent) || !actorComponent.playerSession.ConnectedClient.IsConnected)
            {
                return;
            }

            // New client won't have all the same data so we'll reset it.
            _lastSent.Clear();
            
            foreach (var (_, chunks) in _knownChunks)
            {
                foreach (var (_, chunk) in chunks)
                {
                    var indices = new HashSet<MapIndices>();
                    foreach (var tile in chunk.GetAllIndices())
                    {
                        indices.Add(tile);
                    }

                    if (!TryChunkToMessage(chunk, out var message)) continue;
                    
                    SendNetworkMessage(message, actorComponent.playerSession.ConnectedClient);
                }
            }
        }
    }
}