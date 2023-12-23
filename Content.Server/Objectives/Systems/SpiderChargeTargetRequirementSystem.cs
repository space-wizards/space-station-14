using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class SpiderChargeTargetRequirementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderChargeTargetRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, SpiderChargeTargetRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<NinjaRoleComponent>(args.MindId, out var role) || role.SpiderChargeTarget == null)
            args.Cancelled = true;
    }
}
