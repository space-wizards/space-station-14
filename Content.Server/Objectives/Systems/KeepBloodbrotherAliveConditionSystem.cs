using Content.Server.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using System.Linq;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.Objectives.Systems;

public sealed class KeepBloodbrotherAliveConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly BloodBrotherRuleSystem _broRule = default!;

    private List<(EntityUid Id, MindComponent Mind)> _bros = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeepBloodbrotherAliveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<KeepBloodbrotherAliveConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnGetProgress(EntityUid uid, KeepBloodbrotherAliveConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (_bros == null || _bros.Count == 0)
            return;

        args.Progress = GetProgressMultiple(_bros);
    }

    private void OnAssigned(EntityUid uid, KeepBloodbrotherAliveConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var bros = Enumerable.ToList(_broRule.GetOtherBroMindsAliveAndConnected(args.Mind));

        // You are the first/only traitor.
        if (bros.Count == 0)
        {
            args.Cancelled = true;
            return;
        }
        _bros = bros;
    }

    private float GetProgressMultiple(List<(EntityUid Id, MindComponent Mind)> targets)
    {
        var progress = 0f;
        foreach (var target in targets)
        {
            if (!TryComp<MindComponent>(target.Id, out var mind))
                return 0f;
            if (!_mind.IsCharacterDeadIc(mind))
                progress += 1 / (targets.Count - 1);
        }
        return progress;
    }
}
