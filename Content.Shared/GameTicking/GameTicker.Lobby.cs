using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    public bool UserHasJoinedGame(ICommonSession session) => UserHasJoinedGame(session.UserId);

    /// <summary>
    /// Returns if a given session has currently joined the game.
    /// </summary>
    /// <param name="userId">User</param>
    /// <returns>Returns true if the player has joined the game. Only can return true on server!</returns>
    public virtual bool UserHasJoinedGame(NetUserId userId) => false;
}
