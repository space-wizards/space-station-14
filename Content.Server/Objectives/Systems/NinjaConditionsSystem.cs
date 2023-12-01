using Content.Server.Roles;
using Content.Server.Objectives.Components;
using Content.Server.Warps;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the objective conditions that hard depend on ninja.
/// Survive is handled by <see cref="SurviveConditionSystem"/> since it works without being a ninja.
/// </summary>
public sealed class NinjaConditionsSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DoorjackConditionComponent, ObjectiveGetProgressEvent>(OnDoorjackGetProgress);

        SubscribeLocalEvent<SpiderChargeConditionComponent, ObjectiveAfterAssignEvent>(OnSpiderChargeAfterAssign);
        SubscribeLocalEvent<SpiderChargeConditionComponent, ObjectiveGetProgressEvent>(OnSpiderChargeGetProgress);

        SubscribeLocalEvent<StealResearchConditionComponent, ObjectiveGetProgressEvent>(OnStealResearchGetProgress);

        SubscribeLocalEvent<TerrorConditionComponent, ObjectiveGetProgressEvent>(OnTerrorGetProgress);
    }

    // doorjack

    private void OnDoorjackGetProgress(EntityUid uid, DoorjackConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = DoorjackProgress(comp, _number.GetTarget(uid));
    }

    private float DoorjackProgress(DoorjackConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.DoorsJacked / (float) target, 1f);
    }

    // spider charge

    private void OnSpiderChargeAfterAssign(EntityUid uid, SpiderChargeConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, SpiderChargeTitle(args.MindId), args.Meta);
    }

    private void OnSpiderChargeGetProgress(EntityUid uid, SpiderChargeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.SpiderChargeDetonated ? 1f : 0f;
    }

    private string SpiderChargeTitle(EntityUid mindId)
    {
        if (!TryComp<NinjaRoleComponent>(mindId, out var role) ||
            role.SpiderChargeTarget == null ||
            !TryComp<WarpPointComponent>(role.SpiderChargeTarget, out var warp))
        {
            // this should never really happen but eh
            return Loc.GetString("objective-condition-spider-charge-title-no-target");
        }

        return Loc.GetString("objective-condition-spider-charge-title", ("location", warp.Location ?? Name(role.SpiderChargeTarget.Value)));
    }

    // steal research

    private void OnStealResearchGetProgress(EntityUid uid, StealResearchConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = StealResearchProgress(comp, _number.GetTarget(uid));
    }

    private float StealResearchProgress(StealResearchConditionComponent comp, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        return MathF.Min(comp.DownloadedNodes.Count / (float) target, 1f);
    }

    private void OnTerrorGetProgress(EntityUid uid, TerrorConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = comp.CalledInThreat ? 1f : 0f;
    }
}
