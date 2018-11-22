using System;
using System.Linq;
using Content.Server.GameObjects;
using Content.Server.GameTicking.GamePresets;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.GameObjects.Components.Inventory;
using JetBrains.Annotations;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.Maps;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Configuration;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Timing;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameTicking
{
    public class GameTicker : IGameTicker
    {
        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                if (_runLevel == value)
                {
                    return;
                }

                var old = _runLevel;
                _runLevel = value;

                OnRunLevelChanged?.Invoke(new GameRunLevelChangedEventArgs(old, value));
            }
        }

        private const string PlayerPrototypeName = "HumanMob_Content";
        private const string MapFile = "Maps/stationstation.yml";

        private bool _initialized;
        [ViewVariables(VVAccess.ReadWrite)] private GridLocalCoordinates _spawnPoint;

#pragma warning disable 649
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IMapManager _mapManager;
        [Dependency] private IMapLoader _mapLoader;
        [Dependency] private IGameTiming _gameTiming;
        [Dependency] private IConfigurationManager _configurationManager;
#pragma warning restore 649

        public Action<GameRunLevelChangedEventArgs> OnRunLevelChanged;
        private GameRunLevel _runLevel;

        public void Initialize()
        {
            DebugTools.Assert(!_initialized);

            _configurationManager.RegisterCVar("game.lobbyenabled", false, CVar.ARCHIVE);

            RestartRound();

            _initialized = true;
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
        }

        public void RestartRound()
        {
            Logger.InfoS("ticker", "Restarting round!");

            RunLevel = GameRunLevel.PreRoundLobby;
            _resettingCleanup();
            _preRoundSetup();

            if (_configurationManager.GetCVar<bool>("game.lobbyenabled"))
            {
                throw new NotImplementedException();
            }

            StartRound();
        }

        public void StartRound()
        {
            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            Logger.InfoS("ticker", "Starting round!");

            RunLevel = GameRunLevel.InRound;

            // TODO: Allow other presets to be selected.
            var preset = new PresetTraitor();
            preset.Start();
        }

        public void EndRound()
        {
            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            Logger.InfoS("ticker", "Ending round!");

            RunLevel = GameRunLevel.PostRound;
        }

        public IEntity SpawnPlayerMob()
        {
            var entity = _entityManager.ForceSpawnEntityAt(PlayerPrototypeName, _spawnPoint);
            var shoes = _entityManager.SpawnEntity("ShoesItem");
            var uniform = _entityManager.SpawnEntity("UniformAssistant");
            if (entity.TryGetComponent(out InventoryComponent inventory))
            {
                inventory.Equip(EquipmentSlotDefines.Slots.INNERCLOTHING, uniform.GetComponent<ClothingComponent>());
                inventory.Equip(EquipmentSlotDefines.Slots.SHOES, shoes.GetComponent<ClothingComponent>());
            }

            return entity;
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void _resettingCleanup()
        {
            // Delete all entities.
            foreach (var entity in _entityManager.GetEntities().ToList())
            {
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                entity.Delete();
            }

            // Delete all maps outside of nullspace.
            foreach (var map in _mapManager.GetAllMaps().ToList())
            {
                // TODO: Maybe something less naive here?
                if (map.Index != MapId.Nullspace)
                {
                    _mapManager.DeleteMap(map.Index);
                }
            }
        }

        private void _preRoundSetup()
        {
            var newMap = _mapManager.CreateMap();
            var startTime = _gameTiming.RealTime;
            var grid = _mapLoader.LoadBlueprint(newMap, MapFile);

            _spawnPoint = new GridLocalCoordinates(Vector2.Zero, grid);

            var timeSpan = _gameTiming.RealTime - startTime;
            Logger.InfoS("ticker", $"Loaded map in {timeSpan.TotalMilliseconds:N2}ms.");
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound,
        PostRound
    }

    public class GameRunLevelChangedEventArgs : EventArgs
    {
        public GameRunLevel OldRunLevel { get; }
        public GameRunLevel NewRunLevel { get; }

        public GameRunLevelChangedEventArgs(GameRunLevel oldRunLevel, GameRunLevel newRunLevel)
        {
            OldRunLevel = oldRunLevel;
            NewRunLevel = newRunLevel;
        }
    }

    class StartRoundCommand : IClientCommand
    {
        public string Command => "startround";
        public string Description => "Ends PreRoundLobby state and starts the round.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();

            if (ticker.RunLevel != GameRunLevel.PreRoundLobby)
            {
                shell.SendText(player, "This can only be executed while the game is in the pre-round lobby.");
                return;
            }

            ticker.StartRound();
        }
    }

    class EndRoundCommand : IClientCommand
    {
        public string Command => "endround";
        public string Description => "Ends the round and moves the server to PostRound.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();

            if (ticker.RunLevel != GameRunLevel.InRound)
            {
                shell.SendText(player, "This can only be executed while the game is in a round.");
                return;
            }

            ticker.EndRound();
        }
    }

    class NewRoundCommand : IClientCommand
    {
        public string Command => "restartround";
        public string Description => "Moves the server from PostRound to a new PreRoundLobby.";
        public string Help => String.Empty;

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ticker = IoCManager.Resolve<IGameTicker>();
            ticker.RestartRound();
        }
    }
}
