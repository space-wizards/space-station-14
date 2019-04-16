using System;
using Content.Server.GameTicking;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Interfaces.GameObjects;
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
    }
}
