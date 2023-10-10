using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class SpiderChargeTargetRequirementSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderChargeTargetRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, SpiderChargeTargetRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<NinjaRoleComponent>(args.MindId)
            || _mind.TryGetObjectiveComp<SpiderChargeConditionComponent>(args.MindId, out var obj, args.Mind) && obj.Target == null)
        {
            args.Cancelled = true;
        }
    }
}
