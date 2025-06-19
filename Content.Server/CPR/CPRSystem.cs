using Content.Shared.CPR;
using Robust.Shared.Player;

namespace Content.Server.CPR;

public sealed partial class CPRSystem : SharedCPRSystem
{
    public override void DoLunge(EntityUid user)
    {
        // raise event for all nearby players
        Filter filter = Filter.PvsExcept(user, entityManager: Ent);

        RaiseNetworkEvent(new CPRLungeEvent(GetNetEntity(user)), filter);
    }
}
