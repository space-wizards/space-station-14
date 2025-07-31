using Content.Shared.NullLink.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._NullLink;

[Virtual]
public abstract class SharedPlayerRolesReqManager : ISharedNullLinkPlayerRolesReqManager, IPostInjectInit
{
    [Dependency] protected readonly IPrototypeManager _proto = default!;
    [Dependency] protected readonly IConfigurationManager _cfg = default!;

    public void PostInject()
    {
        _cfg.OnValueChanged(NullLinkCCVars.RoleReqWithAccessToAllRoles, UpdateAllRoles, true);
    }

    private void UpdateAllRoles(string obj)
    {
        if(_proto.TryIndex<RoleRequirementPrototype>(obj, out var allRoles))
            AllRoles = allRoles;

    }
    protected RoleRequirementPrototype? AllRoles;
    public abstract bool IsAllRolesAvailable(EntityUid uid);

    public abstract bool IsAllRolesAvailable(ICommonSession session);
}