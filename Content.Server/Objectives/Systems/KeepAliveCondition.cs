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
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeepAliveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<RandomTraitorAliveComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnGetProgress(EntityUid uid, KeepAliveConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value);
    }

    private void OnAssigned(EntityUid uid, RandomTraitorAliveComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).Select(t => t.Id).ToHashSet();
        args.Mind.ObjectiveTargets.ForEach(p => traitors.Remove(p));

        // You are the first/only traitor.
        if (traitors.Count == 0)
        {
            // If not trying to make all possible candidates traitors, cancel the objective
            if (!_traitorRule.ForceAllPossible)
            {
                args.Cancelled = true;
                return;
            }

            //Fallback to assign people who COULD be assigned as traitor - might need to just do this from the start on ForceAll rounds, limiting it to existing traitors could be skewing the numbers towards just a few people.
            var allHumans = _mind.GetAliveHumans(args.MindId).Select(p => p.Owner).ToHashSet();
            var allValidTraitorCandidates = new HashSet<EntityUid>();
            if (_traitorRule.CurrentAntagPool != null)
            {
                var poolSessions = _traitorRule.CurrentAntagPool.GetPoolSessions();
                foreach (var mind in allHumans)
                {
                    if (!args.Mind.ObjectiveTargets.Contains(mind) && _job.MindTryGetJob(mind, out var prototype) && prototype.CanBeAntag && _mind.TryGetSession(mind, out var session) && poolSessions.Contains(session))
                    {
                        allValidTraitorCandidates.Add(mind);
                    }
                }
            }

            // Just try and save some other nerd for some reason. The syndicate needs them alive.
            if (allValidTraitorCandidates.Count == 0)
            {
                allValidTraitorCandidates = allHumans;
            }
            traitors = allValidTraitorCandidates;

            // One last check for the road, then cancel it if there's nothing left
            if (traitors.Count == 0)
            {
                args.Cancelled = true;
                return;
            }
        }

        var randomTarget = _random.Pick(traitors);
        _target.SetTargetExclusive(uid, args.Mind, randomTarget, target);
    }

    private float GetProgress(EntityUid target)
    {
        if (!TryComp<MindComponent>(target, out var mind))
            return 0f;

        return _mind.IsCharacterDeadIc(mind) ? 0f : 1f;
    }
}
