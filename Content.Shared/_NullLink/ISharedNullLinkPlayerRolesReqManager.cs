using Robust.Shared.Player;

namespace Content.Shared._NullLink;

public interface ISharedNullLinkPlayerRolesReqManager
{
    bool IsAllRolesAvailable(EntityUid uid);
    bool IsAllRolesAvailable(ICommonSession session);
}
