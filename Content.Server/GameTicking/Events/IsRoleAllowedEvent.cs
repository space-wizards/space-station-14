using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Events;

[ByRefEvent]
public struct IsRoleAllowedEvent(ICommonSession player, string roleId, bool cancelled = false)
{
    public readonly ICommonSession Player = player;
    public readonly ProtoId<JobPrototype> RoleId = roleId;
    public bool Cancelled = cancelled;
}
