using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared._Starlight.Abstract.Conditions;

public sealed partial class SubjectMindJobCondition : BaseCondition
{
    [DataField]
    public HashSet<ProtoId<JobPrototype>>? Whitelist;
    [DataField]
    public HashSet<ProtoId<JobPrototype>>? Blacklist;
    [DataField]
    public bool PassIfJobEmpty = false;

    public override bool Handle(EntityUid @subject, EntityUid @object)
    {
        base.Handle(@subject, @object);
        return Ent.System<SharedJobSystem>() is var jobSys
            && Ent.System<SharedMindSystem>() is var mindSys
            && mindSys.GetMind(@subject) is { } mind
            && ((jobSys.MindTryGetJob(mind, out var job)
            && (Blacklist == null || !Blacklist.Contains(job.ID))
            && (Whitelist == null || Whitelist.Contains(job.ID))) || PassIfJobEmpty);
    }
}
