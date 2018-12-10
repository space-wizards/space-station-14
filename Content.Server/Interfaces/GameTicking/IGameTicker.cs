using System;
using Content.Server.GameTicking;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Timing;

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
