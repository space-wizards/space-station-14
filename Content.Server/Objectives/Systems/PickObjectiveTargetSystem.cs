using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs; // imp
using Content.Server.GameTicking.Rules;
using Content.Server.Revolutionary.Components;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles assinging a target to an objective entity with <see cref="TargetObjectiveComponent"/> using different components.
/// These can be combined with condition components for objective completions in order to create a variety of objectives.
/// </summary>
public sealed class PickObjectiveTargetSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    [Dependency] private readonly SharedJobSystem _job = default!; // imp edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PickSpecificPersonComponent, ObjectiveAssignedEvent>(OnSpecificPersonAssigned);
        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnRandomPersonAssigned);
        SubscribeLocalEvent<PickRandomHeadComponent, ObjectiveAssignedEvent>(OnRandomHeadAssigned);
        SubscribeLocalEvent<PickRandomTraitorComponent, ObjectiveAssignedEvent>(OnTraitorAssigned);

        SubscribeLocalEvent<RandomTraitorProgressComponent, ObjectiveAssignedEvent>(OnRandomTraitorProgressAssigned);
        SubscribeLocalEvent<RandomTraitorAliveComponent, ObjectiveAssignedEvent>(OnRandomTraitorAliveAssigned);
    }

    private void OnSpecificPersonAssigned(Entity<PickSpecificPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TargetOverrideComponent>(user, out var targetComp) || targetComp.Target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, targetComp.Target.Value);
    }

    private void OnRandomPersonAssigned(Entity<PickRandomPersonComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        var allHumans = _mind.GetAliveHumans(args.MindId);

        // Can't have multiple objectives to kill the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<KillPersonConditionComponent>(objective) && TryComp<TargetObjectiveComponent>(objective, out var kill))
            {
                allHumans.RemoveWhere(x => x.Owner == kill.Target);
            }
        }

        // no other humans to kill
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, _random.Pick(allHumans), target);
    }

    private void OnRandomHeadAssigned(Entity<PickRandomHeadComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allHeads = new HashSet<Entity<MindComponent>>();
        foreach (var person in allHumans)
        {
            if (TryComp<MindComponent>(person, out var mind) && mind.OwnedEntity is { } owned && HasComp<CommandStaffComponent>(owned))
                allHeads.Add(person);
        }

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        _target.SetTarget(ent.Owner, _random.Pick(allHeads), target);
    }

    private void OnRandomTraitorProgressAssigned(Entity<RandomTraitorProgressComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).ToHashSet();

        // cant help anyone who is tasked with helping:
        // 1. thats boring
        // 2. no cyclic progress dependencies!!!
        foreach (var traitor in traitors)
        {
            // TODO: replace this with TryComp<ObjectivesComponent>(traitor) or something when objectives are moved out of mind
            if (!TryComp<MindComponent>(traitor.Id, out var mind))
                continue;

            foreach (var objective in mind.Objectives)
            {
                if (HasComp<HelpProgressConditionComponent>(objective))
                    traitors.RemoveWhere(x => x.Mind == mind);
            }
        }

        // Can't have multiple objectives to help/save the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<RandomTraitorAliveComponent>(objective) || HasComp<RandomTraitorProgressComponent>(objective))
            {
                if (TryComp<TargetObjectiveComponent>(objective, out var help))
                {
                    traitors.RemoveWhere(x => x.Id == help.Target);
                }
            }
        }

        // no more helpable traitors
        if (traitors.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, _random.Pick(traitors).Id, target);
    }

    private void OnRandomTraitorAliveAssigned(Entity<RandomTraitorAliveComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).Select(t => t.Id).ToHashSet(); // Imp edit -  just get entity

        // Can't have multiple objectives to help/save the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<RandomTraitorAliveComponent>(objective) || HasComp<RandomTraitorProgressComponent>(objective))
            {
                if (TryComp<TargetObjectiveComponent>(objective, out var help))
                {
                    traitors.RemoveWhere(x => x == help.Target); // Imp edit
                }
            }
        }

        // You are the first/only traitor.
        if (traitors.Count == 0)
        {
            // Imp edit start
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
            // Imp edit end
        }
        var randomTarget = _random.Pick(traitors); // Imp edit
        _target.SetTarget(ent.Owner, randomTarget, target);
    }

    // Imp addition - ideally I'd redo this funciton to work for both but I'm not doing that right now
    private void OnTraitorAssigned(Entity<PickRandomTraitorComponent> ent, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }

        var traitors = _traitorRule.GetOtherTraitorMindsAliveAndConnected(args.Mind).Select(t => t.Id).ToHashSet(); // Imp edit -  just get entity

        // Can't have multiple objectives to help/save the same person
        foreach (var objective in args.Mind.Objectives)
        {
            if (HasComp<RandomTraitorAliveComponent>(objective) || HasComp<RandomTraitorProgressComponent>(objective))
            {
                if (TryComp<TargetObjectiveComponent>(objective, out var help))
                {
                    traitors.RemoveWhere(x => x == help.Target); // Imp edit
                }
            }
        }

        // You are the first/only traitor.
        if (traitors.Count == 0)
        {
            // Imp edit start
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
            // Imp edit end
        }
        var randomTarget = _random.Pick(traitors); // Imp edit
        _target.SetTarget(ent.Owner, randomTarget, target);
    }
}
