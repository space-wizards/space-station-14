using Robust.Shared.Player;

namespace Content.Shared._NullLink;

public interface ISharedNullLinkPlayerRolesReqManager
{
    void Initialize();
    bool IsAllRolesAvailable(EntityUid uid);
    bool IsAllRolesAvailable(ICommonSession session);
    bool IsAnyRole(ICommonSession session, ulong[] roles);
    bool IsMentor(EntityUid uid);
    bool IsMentor(ICommonSession session);
    bool IsPeacefulBypass(EntityUid uid);
}
