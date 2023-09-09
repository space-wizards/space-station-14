using Content.Server.Objectives.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles keep alive condition logic and picking random traitors to keep alive.
/// </summary>
public sealed class KeepAliveConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeepAliveConditionComponent, ConditionGetInfoEvent>(OnGetInfo);

        SubscribeLocalEvent<RandomTraitorAliveComponent, ConditionAssignedEvent>(OnAssigned);
    }

    private void OnGetInfo(EntityUid uid, KeepAliveConditionComponent comp, ref ConditionGetInfoEvent args)
    {
        if (comp.Target == null)
            return;

        var target = comp.Target.Value;
        args.Info.Title = GetTitle(target);
        args.Info.Progress = GetProgress(target);
    }

    private void OnAssigned(EntityUid uid, RandomTraitorAliveComponent comp, ref ConditionAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<KeepAliveConditionComponent>(uid, out var alive))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = Enumerable.ToList<(EntityUid Id, MindComponent Mind)>(_traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind));

        // You are the first/only traitor.
        if (traitors.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        alive.Target = _random.Pick(traitors).Id;
    }

    private string GetTitle(EntityUid target)
    {
        var targetName = "Unknown";
        var jobName = _job.MindTryGetJobName(target);

        if (TryComp<MindComponent>(target, out var mind) && mind.OwnedEntity != null)
        {
            targetName = Name(mind.OwnedEntity.Value);
        }

        return Loc.GetString("objective-condition-other-traitor-alive-title", ("targetName", targetName), ("job", jobName));
    }

    private float GetProgress(EntityUid target)
    {
        if (!TryComp<MindComponent>(target, out var mind))
            return 0f;

        return _mind.IsCharacterDeadIc(mind) ? 0f : 1f;
    }
}
