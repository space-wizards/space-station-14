#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Atmos;
using Content.Shared;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    [UsedImplicitly]
    public sealed class AtmosDebugOverlaySystem : SharedAtmosDebugOverlaySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;

        /// <summary>
        ///     Players allowed to see the atmos debug overlay.
        ///     To modify it see <see cref="AddObserver"/> and
        ///     <see cref="RemoveObserver"/>.
        /// </summary>
        private readonly HashSet<IPlayerSession> _playerObservers = new();

        /// <summary>
        ///     Overlay update ticks per second.
        /// </summary>
        private float _updateCooldown;

        public override void Initialize()
        {
            base.Initialize();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        public bool AddObserver(IPlayerSession observer)
        {
            return _playerObservers.Add(observer);
        }

        public bool HasObserver(IPlayerSession observer)
        {
            return _playerObservers.Contains(observer);
        }

        public bool RemoveObserver(IPlayerSession observer)
        {
            if (!_playerObservers.Remove(observer))
            {
                return false;
            }

            var message = new AtmosDebugOverlayDisableMessage();
            RaiseNetworkEvent(message, observer.ConnectedClient);

            return true;
        }

        /// <summary>
        ///     Adds the given observer if it doesn't exist, removes it otherwise.
        /// </summary>
        /// <param name="observer">The observer to toggle.</param>
        /// <returns>true if added, false if removed.</returns>
        public bool ToggleObserver(IPlayerSession observer)
        {
            if (HasObserver(observer))
            {
                RemoveObserver(observer);
                return false;
            }
            else
            {
                AddObserver(observer);
                return true;
            }
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
            {
                RemoveObserver(e.Session);
            }
        }

        private AtmosDebugOverlayData ConvertTileToData(TileAtmosphere? tile)
        {
            var gases = new float[Atmospherics.TotalNumberOfGases];
            if (tile?.Air == null)
            {
                return new AtmosDebugOverlayData(0, gases, AtmosDirection.Invalid, false, tile?.BlockedAirflow ?? AtmosDirection.Invalid);
            }
            else
            {
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    gases[i] = tile.Air.GetMoles(i);
                }
                return new AtmosDebugOverlayData(tile.Air.Temperature, gases, tile.PressureDirectionForDebugOverlay, tile.ExcitedGroup != null, tile.BlockedAirflow);
            }
        }

        public override void Update(float frameTime)
        {
            AccumulatedFrameTime += frameTime;
            _updateCooldown = 1 / _configManager.GetCVar(CCVars.NetAtmosDebugOverlayTickRate);

            if (AccumulatedFrameTime < _updateCooldown)
            {
                return;
            }

            // This is the timer from GasTileOverlaySystem
            AccumulatedFrameTime -= _updateCooldown;

            var currentTick = _gameTiming.CurTick;

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            foreach (var session in _playerObservers)
            {
                if (session.AttachedEntity == null) continue;

                var entity = session.AttachedEntity;

                var worldBounds = Box2.CenteredAround(entity.Transform.WorldPosition,
                    new Vector2(LocalViewRange, LocalViewRange));

                foreach (var grid in _mapManager.FindGridsIntersecting(entity.Transform.MapID, worldBounds))
                {
                    if (!EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) continue;

                    if (!gridEnt.TryGetComponent<GridAtmosphereComponent>(out var gam)) continue;

                    var entityTile = grid.GetTileRef(entity.Transform.Coordinates).GridIndices;
                    var baseTile = new Vector2i(entityTile.X - (LocalViewRange / 2), entityTile.Y - (LocalViewRange / 2));
                    var debugOverlayContent = new AtmosDebugOverlayData[LocalViewRange * LocalViewRange];

                    var index = 0;
                    for (var y = 0; y < LocalViewRange; y++)
                    {
                        for (var x = 0; x < LocalViewRange; x++)
                        {
                            var Vector2i = new Vector2i(baseTile.X + x, baseTile.Y + y);
                            debugOverlayContent[index++] = ConvertTileToData(gam.GetTile(Vector2i));
                        }
                    }

                    RaiseNetworkEvent(new AtmosDebugOverlayMessage(grid.Index, baseTile, debugOverlayContent), session.ConnectedClient);
                }
            }
        }
    }
}
