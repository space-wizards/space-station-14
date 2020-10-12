#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems.Atmos;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.EntitySystems.Atmos
{
    [UsedImplicitly]
    public sealed class AtmosDebugOverlaySystem : SharedAtmosDebugOverlaySystem
    {
        [Robust.Shared.IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [Robust.Shared.IoC.Dependency] private readonly IPlayerManager _playerManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IEntityManager _entityManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly IConfigurationManager _configManager = default!;

        /// <summary>
        ///     Players allowed to see the atmos debug overlay
        /// </summary>
        public HashSet<IPlayerSession> PlayerObservers = new HashSet<IPlayerSession>();

        /// <summary>
        ///     Overlay update ticks per second.
        /// </summary>
        private float _updateCooldown;

        private AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            _atmosphereSystem = Get<AtmosphereSystem>();
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _configManager.RegisterCVar("net.atmosdbgoverlaytickrate", 3.0f);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
            {
                if (PlayerObservers.Contains(e.Session))
                {
                    PlayerObservers.Remove(e.Session);
                }

                return;
            }
        }

        private AtmosDebugOverlayData ConvertTileToData(TileAtmosphere? tile)
        {
            var gases = new float[Atmospherics.TotalNumberOfGases];
            if (tile?.Air == null)
            {
                return new AtmosDebugOverlayData(0, gases, AtmosDirection.Invalid, false);
            }
            else
            {
                for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
                {
                    gases[i] = tile.Air.GetMoles(i);
                }
                return new AtmosDebugOverlayData(tile.Air.Temperature, gases, tile.PressureDirectionForDebugOverlay, tile.ExcitedGroup != null);
            }
        }

        public override void Update(float frameTime)
        {
            AccumulatedFrameTime += frameTime;
            _updateCooldown = 1 / _configManager.GetCVar<float>("net.atmosdbgoverlaytickrate");

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
            foreach (var session in PlayerObservers)
            {
                if (session.AttachedEntity == null) continue;

                var entity = session.AttachedEntity;

                var worldBounds = Box2.CenteredAround(entity.Transform.WorldPosition,
                    new Vector2(LocalViewRange, LocalViewRange));

                foreach (var grid in _mapManager.FindGridsIntersecting(entity.Transform.MapID, worldBounds))
                {
                    if (!_entityManager.TryGetEntity(grid.GridEntityId, out var gridEnt)) continue;

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
