using Content.Shared.Job;
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
