using Content.Shared.Ghost.Roles;
using Content.Shared.Job;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Events;

[ByRefEvent]
public struct IsJobAllowedEvent(ICommonSession player, ProtoId<JobPrototype> job, bool cancelled = false)
{
    public readonly ICommonSession Player = player;
    public readonly ProtoId<JobPrototype> Job = job;
    public bool Cancelled = cancelled;
}

[ByRefEvent]
public struct IsGhostRoleAllowedEvent(ICommonSession player, ProtoId<GhostRolePrototype> ghostRole, bool cancelled = false)
{
    public readonly ICommonSession Player = player;
    public readonly ProtoId<GhostRolePrototype> GhostRole = ghostRole;
    public bool Cancelled = cancelled;
}
