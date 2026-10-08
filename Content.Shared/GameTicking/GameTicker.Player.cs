using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    public abstract void PlayerJoinGame(ICommonSession session, bool silent = false);
}
