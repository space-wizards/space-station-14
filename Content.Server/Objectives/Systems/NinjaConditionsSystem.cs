using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Warps;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the objective conditions that hard depend on ninja.
/// Survive is handled by <see cref="SurviveConditionSystem"/> since it works without being a ninja.
/// </summary>
public sealed class NinjaConditionsSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DoorjackConditionComponent, ObjectiveGetProgressEvent>(OnDoorjackGetProgress);

        SubscribeLocalEvent<SpiderChargeConditionComponent, RequirementCheckEvent>(OnSpiderChargeRequirementCheck);
        SubscribeLocalEvent<SpiderChargeConditionComponent, ObjectiveAfterAssignEvent>(OnSpiderChargeAfterAssign);

        SubscribeLocalEvent<StealResearchConditionComponent, ObjectiveGetProgressEvent>(OnStealResearchGetProgress);
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
    private void OnSpiderChargeRequirementCheck(EntityUid uid, SpiderChargeConditionComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled || !_roles.MindHasRole<NinjaRoleComponent>(args.MindId))
            return;

        // choose spider charge detonation point
        var warps = new List<EntityUid>();
        var allEnts = EntityQueryEnumerator<WarpPointComponent>();
        var bombingBlacklist = comp.Blacklist;

        while (allEnts.MoveNext(out var warpUid, out var warp))
        {
            if (_whitelist.IsWhitelistFail(bombingBlacklist, warpUid)
                && !string.IsNullOrWhiteSpace(warp.Location))
            {
                warps.Add(warpUid);
            }
        }

        if (warps.Count <= 0)
        {
            args.Cancelled = true;
            return;
        }
        comp.Target = _random.Pick(warps);
    }

    private void OnSpiderChargeAfterAssign(EntityUid uid, SpiderChargeConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        string title;
        if (comp.Target == null || !TryComp<WarpPointComponent>(comp.Target, out var warp) || warp.Location == null)
        {
            // this should never really happen but eh
            title = Loc.GetString("objective-condition-spider-charge-title-no-target");
        }
        else
        {
            title = Loc.GetString("objective-condition-spider-charge-title", ("location", warp.Location));
        }
        _metaData.SetEntityName(uid, title, args.Meta);
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
}
