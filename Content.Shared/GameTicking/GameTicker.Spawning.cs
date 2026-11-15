using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    /// <summary>
    /// Makes a player join into the game and spawn on a station.
    /// </summary>
    /// <param name="player">The player joining</param>
    /// <param name="station">The station they're spawning on</param>
    /// <param name="jobId">An optional job for them to spawn as</param>
    /// <param name="silent">Whether or not the player should be greeted upon joining</param>
    public virtual void MakeJoinGame(ICommonSession player, EntityUid station, string? jobId = null, bool silent = false) { }
}
