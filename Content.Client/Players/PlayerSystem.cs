using Content.Shared.Players;
using Robust.Shared.Players;

namespace Content.Client.Players;

public sealed class PlayerSystem : SharedPlayerSystem
{
    public override PlayerData? ContentData(ICommonSession? session)
    {
        return null;
    }
}
