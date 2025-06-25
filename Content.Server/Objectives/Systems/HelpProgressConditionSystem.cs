using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles help progress condition logic.
/// </summary>
public sealed class HelpProgressConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HelpProgressConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, HelpProgressConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value);
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
        return completion;
    }
}
