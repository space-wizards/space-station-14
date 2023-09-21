using Content.Server.Roles;
using Content.Server.Objectives.Components;
using Content.Server.Warps;
using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;

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
        args.Progress = DoorjackProgress(args.MindId, _number.GetTarget(uid));
    }

    private float DoorjackProgress(EntityUid mindId, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (!TryComp<NinjaRoleComponent>(mindId, out var role))
            return 0f;

        if (role.DoorsJacked >= target)
            return 1f;

        return (float) role.DoorsJacked / (float) target;
    }

    // spider charge

    private void OnSpiderChargeAfterAssign(EntityUid uid, SpiderChargeConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, SpiderChargeTitle(args.MindId), args.Meta);
    }

    private void OnSpiderChargeGetProgress(EntityUid uid, SpiderChargeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = TryComp<NinjaRoleComponent>(args.MindId, out var role) && role.SpiderChargeDetonated ? 1f : 0f;
    }

    private string SpiderChargeTitle(EntityUid mindId)
    {
        if (!TryComp<NinjaRoleComponent>(mindId, out var role) ||
            role.SpiderChargeTarget == null ||
            !TryComp<WarpPointComponent>(role.SpiderChargeTarget, out var warp) ||
            warp.Location == null)
        {
            // this should never really happen but eh
            return Loc.GetString("objective-condition-spider-charge-title-no-target");
        }

        return Loc.GetString("objective-condition-spider-charge-title", ("location", warp.Location));
    }

    // steal research

    private void OnStealResearchGetProgress(EntityUid uid, StealResearchConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = StealResearchProgress(args.MindId, _number.GetTarget(uid));
    }

    private float StealResearchProgress(EntityUid mindId, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (!TryComp<NinjaRoleComponent>(mindId, out var role))
            return 0f;

        if (role.DownloadedNodes.Count >= target)
            return 1f;

        return (float) role.DownloadedNodes.Count / (float) target;
    }

    // terror

    private void OnTerrorGetProgress(EntityUid uid, TerrorConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = TryComp<NinjaRoleComponent>(args.MindId, out var role) && role.CalledInThreat ? 1f : 0f;
    }
}
