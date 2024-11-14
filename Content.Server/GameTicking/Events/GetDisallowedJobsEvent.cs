using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Events;

[ByRefEvent]
public readonly record struct GetDisallowedJobsEvent(ICommonSession Player, HashSet<ProtoId<JobPrototype>> Jobs);
