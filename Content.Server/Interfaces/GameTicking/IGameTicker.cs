using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Robust.Server.Interfaces.Player;
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

        void Initialize();
        void Update(FrameEventArgs frameEventArgs);

        void RestartRound();
        void StartRound();
        void EndRound();

        void Respawn(IPlayerSession targetPlayer);
        void MakeObserve(IPlayerSession player);
        void MakeJoinGame(IPlayerSession player);
        void ToggleReady(IPlayerSession player, bool ready);

        // GameRule system.
        T AddGameRule<T>() where T : GameRule, new();
        void RemoveGameRule(GameRule rule);
        IEnumerable<GameRule> ActiveGameRules { get; }

        void SetStartPreset(Type type);
    }
}
