using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Interfaces.GameTicking
{
    /// <summary>
    ///     The game ticker is responsible for managing the round-by-round system of the game.
    /// </summary>
    public interface IGameTicker
    {
        GameRunLevel RunLevel { get; }

        event Action<GameRunLevelChangedEventArgs> OnRunLevelChanged;
        event Action<GameRuleAddedEventArgs> OnRuleAdded;

        void Initialize();
        void Update(FrameEventArgs frameEventArgs);

        void RestartRound();
        void StartRound(bool force = false);
        void EndRound(string roundEndText = "");

        void Respawn(IPlayerSession targetPlayer);
        void MakeObserve(IPlayerSession player);
        void MakeJoinGame(IPlayerSession player, string jobId);
        void ToggleReady(IPlayerSession player, bool ready);

        GridCoordinates GetLateJoinSpawnPoint();
        GridCoordinates GetJobSpawnPoint(string jobId);
        GridCoordinates GetObserverSpawnPoint();

        void EquipStartingGear(IEntity entity, StartingGearPrototype startingGear);

        // GameRule system.
        T AddGameRule<T>() where T : GameRule, new();
        bool HasGameRule(Type type);
        void RemoveGameRule(GameRule rule);
        IEnumerable<GameRule> ActiveGameRules { get; }

        bool TryGetPreset(string name, out Type type);
        void SetStartPreset(Type type, bool force = false);
        void SetStartPreset(string name, bool force = false);

        /// <returns>true if changed, false otherwise</returns>
        bool PauseStart(bool pause = true);

        /// <returns>true if paused, false otherwise</returns>
        bool TogglePause();

        bool DelayStart(TimeSpan time);

        Dictionary<string, int> GetAvailablePositions();
    }
}
