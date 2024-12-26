using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles help progress condition logic and picking random help targets.
/// </summary>
public sealed class HelpProgressConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HelpProgressConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<RandomTraitorProgressComponent, ObjectiveAssignedEvent>(OnTraitorAssigned);
    }

    private void OnGetProgress(EntityUid uid, HelpProgressConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value);
    }

    private void OnTraitorAssigned(EntityUid uid, RandomTraitorProgressComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind)
            .Select(pair => pair.Item1)
            .ToHashSet();
        var removeList = new List<EntityUid>();

        // cant help anyone who is tasked with helping:
        // 1. thats boring
        // 2. no cyclic progress dependencies!!!
        foreach (var traitor in traitors)
        {
            // TODO: replace this with TryComp<ObjectivesComponent>(traitor) or something when objectives are moved out of mind
            if (!TryComp<MindComponent>(traitor, out var mind))
                continue;

            foreach (var objective in mind.Objectives)
            {
                if (HasComp<HelpProgressConditionComponent>(objective))
                    removeList.Add(traitor);
            }
        }

        foreach (var tot in removeList)
        {
            traitors.Remove(tot);
        }

        // no more helpable traitors
        if (traitors.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(uid, _random.Pick(traitors), target);
    }

    private float GetProgress(EntityUid target)
    {
        var total = 0f; // how much progress they have
        var max = 0f; // how much progress is needed for 100%

        if (TryComp<MindComponent>(target, out var mind))
        {
            foreach (var objective in mind.Objectives)
            {
                // this has the potential to loop forever, anything setting target has to check that there is no HelpProgressCondition.
                var info = _objectives.GetInfo(objective, target, mind);
                if (info == null)
                    continue;

                max++; // things can only be up to 100% complete yeah
                total += info.Value.Progress;
            }
        }

        // no objectives that can be helped with...
        if (max == 0f)
            return 1f;

        // require 50% completion for this one to be complete
        var completion = total / max;
        return completion >= 0.5f ? 1f : completion / 0.5f;
    }
}
