using Content.Server.GameTicking;
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

        void Initialize();
        void Update(FrameEventArgs frameEventArgs);

        void RestartRound();
        void StartRound();
        void EndRound();

        IEntity SpawnPlayerMob();
    }
}
