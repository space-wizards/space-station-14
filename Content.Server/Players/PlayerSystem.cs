using Content.Shared.Players;
using Robust.Shared.Player;

namespace Content.Server.Players;

public sealed class PlayerSystem : SharedPlayerSystem
{
    public override ContentPlayerData? ContentData(ICommonSession? session)
    {
        return session?.ContentData();
    }
}
