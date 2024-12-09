using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
///     RoleComponents on a mind entity impact RoleType through this
/// </summary>
public abstract partial class BaseRoleComponent : Component
{
    [DataField]
    public ProtoId<RoleTypePrototype>? RoleType;
}
