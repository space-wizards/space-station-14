using Content.Server.Administration.Managers;
using Content.Shared.Movement.Systems;
using Robust.Server.Player;
using Robust.Shared.Players;

namespace Content.Server.Movement.Systems;

public sealed class ContentEyeSystem : SharedContentEyeSystem
{
    [Dependency] private readonly IAdminManager _admin = default!;

    protected override bool CanZoom(EntityUid uid, ICommonSession session)
    {
        if (session is not IPlayerSession pSession || !_admin.IsAdmin(pSession))
            return false;

        return true;
    }
}
