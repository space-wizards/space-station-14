using System.Numerics;
using Content.Server.Atmos.Components;
using Content.Server.Overlay;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosDebugOverlaySystem : DebugOverlaySystem<AtmosDebugOverlayMessage>
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly MapSystem _mapSystem = default!;

        // Keep in mind, this system is hilariously unoptimized. The goal here is to provide accurate debug data.
        public const int LocalViewRange = 16;
        private float _accumulatedFrameTime;

        /// <summary>
        ///     Overlay update ticks per second.
        /// </summary>
        private float _updateCooldown;

        private List<Entity<MapGridComponent>> _grids = new();

        private AtmosDebugOverlayData? ConvertTileToData(TileAtmosphere tile)
        {
            return new AtmosDebugOverlayData(
                tile.GridIndices,
                tile.Air?.Temperature ?? default,
                tile.Air?.Moles,
                tile.PressureDirection,
                tile.LastPressureDirection,
                tile.AirtightData.BlockedDirections,
                tile.ExcitedGroup?.GetHashCode(),
                tile.Space,
                tile.MapAtmosphere,
                tile.NoGridTile,
                tile.Air?.Immutable ?? false);
        }

        public override void Update(float frameTime)
        {
            _accumulatedFrameTime += frameTime;
            _updateCooldown = 1 / _configManager.GetCVar(CCVars.NetAtmosDebugOverlayTickRate);

            if (_accumulatedFrameTime < _updateCooldown)
            {
                return;
            }

            // This is the timer from GasTileOverlaySystem
            _accumulatedFrameTime -= _updateCooldown;

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            foreach (var session in _playerObservers)
            {
                if (session.AttachedEntity is not {Valid: true} entity)
                    continue;

                var transform = Transform(entity);
                var pos = _transform.GetWorldPosition(transform);
                var worldBounds = Box2.CenteredAround(pos,
                    new Vector2(LocalViewRange, LocalViewRange));

                _grids.Clear();
                _mapManager.FindGridsIntersecting(transform.MapID, worldBounds, ref _grids);

                foreach (var grid in _grids)
                {
                    var uid = grid.Owner;

                    if (!Exists(uid))
                        continue;

                    if (!TryComp(uid, out GridAtmosphereComponent? gridAtmos))
                        continue;

                    var entityTile = _mapSystem.GetTileRef(grid, grid, transform.Coordinates).GridIndices;
                    var baseTile = new Vector2i(entityTile.X - LocalViewRange / 2, entityTile.Y - LocalViewRange / 2);
                    var debugOverlayContent = new AtmosDebugOverlayData?[LocalViewRange * LocalViewRange];

                    var index = 0;
                    for (var y = 0; y < LocalViewRange; y++)
                    {
                        for (var x = 0; x < LocalViewRange; x++)
                        {
                            var vector = new Vector2i(baseTile.X + x, baseTile.Y + y);
                            gridAtmos.Tiles.TryGetValue(vector, out var tile);
                            debugOverlayContent[index++] = tile == null ? null : ConvertTileToData(tile);
                        }
                    }

                    var msg = new AtmosDebugOverlayMessage(GetNetEntity(grid), baseTile, debugOverlayContent);
                    RaiseNetworkEvent(msg, session.Channel);
                }
            }
        }
    }
}
