using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Content.Shared.Roles;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests
{
    public class DummyGameTicker : GameTickerBase, IGameTicker
    {
        public GameRunLevel RunLevel { get; } = GameRunLevel.InRound;

        public event Action<GameRunLevelChangedEventArgs> OnRunLevelChanged
        {
            add { }
            remove { }
        }

        public event Action<GameRuleAddedEventArgs> OnRuleAdded
        {
            add{ }
            remove { }
        }

        public void Update(FrameEventArgs frameEventArgs)
        {
        }

        public void RestartRound()
        {
        }

        public void StartRound(bool force = false)
        {
        }

        public void EndRound(string roundEnd)
        {
        }

        public void Respawn(IPlayerSession targetPlayer)
        {
        }

        public void MakeObserve(IPlayerSession player)
        {
        }

        public void MakeJoinGame(IPlayerSession player, string jobId)
        {
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
        }

        public EntityCoordinates GetLateJoinSpawnPoint() => EntityCoordinates.Invalid;
        public EntityCoordinates GetJobSpawnPoint(string jobId) => EntityCoordinates.Invalid;
        public EntityCoordinates GetObserverSpawnPoint() => EntityCoordinates.Invalid;

        public void EquipStartingGear(IEntity entity, StartingGearPrototype startingGear)
        {
        }

        public T AddGameRule<T>() where T : GameRule, new()
        {
            return new T();
        }

        public bool HasGameRule(Type type)
        {
            return false;
        }

        public void RemoveGameRule(GameRule rule)
        {
        }

        public IEnumerable<GameRule> ActiveGameRules { get; } = Array.Empty<GameRule>();

        public bool TryGetPreset(string name, out Type type)
        {
            type = default;
            return false;
        }

        public void SetStartPreset(Type type, bool force = false)
        {
        }

        public void SetStartPreset(string name, bool force = false)
        {
        }

        public bool DelayStart(TimeSpan time)
        {
            return true;
        }

        public bool PauseStart(bool pause = true)
        {
            return true;
        }

        public bool TogglePause()
        {
            return false;
        }

        public Dictionary<string, int> GetAvailablePositions()
        {
            return new Dictionary<string, int>();
        }
    }
}
