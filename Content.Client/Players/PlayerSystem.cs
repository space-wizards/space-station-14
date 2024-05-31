using Content.Shared.Players;
using Robust.Shared.Player;

namespace Content.Client.Players;

public sealed class PlayerSystem : SharedPlayerSystem
{
    public override ContentPlayerData? ContentData(ICommonSession? session)
    {
        return null;
    }
}
