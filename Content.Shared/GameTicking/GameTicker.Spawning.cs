using Robust.Shared.Player;

namespace Content.Shared.GameTicking;

public abstract partial class GameTicker
{
    public virtual void MakeJoinGame(ICommonSession player, EntityUid station, string? jobId = null, bool silent = false) { }
}
