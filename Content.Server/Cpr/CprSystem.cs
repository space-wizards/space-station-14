using Content.Shared.Cpr;
using Robust.Shared.Player;

namespace Content.Server.Cpr;

public sealed partial class CprSystem : SharedCprSystem
{
    public override void DoLunge(EntityUid user)
    {
        // raise event for all nearby players
        Filter filter = Filter.PvsExcept(user, entityManager: Ent);

        RaiseNetworkEvent(new CprLungeEvent(GetNetEntity(user)), filter);
    }
}
