using Content.Shared.Players;
using Robust.Shared.Players;

namespace Content.Server.Players;

public sealed class PlayerSystem : SharedPlayerSystem
{
    public override PlayerData? ContentData(ICommonSession? session)
    {
        return session?.ContentData();
    }
}
