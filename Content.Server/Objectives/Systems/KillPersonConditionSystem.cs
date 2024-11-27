using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared._Impstation.Ghost;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using System.Linq;


namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class KillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillPersonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<PickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned); // Includes traitors by default

        SubscribeLocalEvent<PickRandomHeadComponent, ObjectiveAssignedEvent>(OnHeadAssigned);

        SubscribeLocalEvent<PickRandomTraitorComponent, ObjectiveAssignedEvent>(OnTraitorAssigned); // ONLY traitors
    }

    private void OnGetProgress(EntityUid uid, KillPersonConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value, comp.RequireDead);
    }

    private void OnPersonAssigned(EntityUid uid, PickRandomPersonComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
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

        _target.SetTargetExclusive(uid, args.Mind, _random.Pick(allHumans), target);
    }

    private void OnHeadAssigned(EntityUid uid, PickRandomHeadComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
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
            if (TryComp<MindComponent>(person, out var mind) && mind.OwnedEntity is { } ent && HasComp<CommandStaffComponent>(ent))
                allHeads.Add(person);
        }

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        _target.SetTargetExclusive(uid, args.Mind, _random.Pick(allHeads), target);
    }

    private void OnTraitorAssigned(EntityUid uid, PickRandomTraitorComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(args.MindId).Select(p => p.Owner).ToHashSet();
        if (allHumans.Count == 0)
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

            // Just kill some random nerd if there's literally not a single potential traitor currently available.
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

    private float GetProgress(EntityUid target, bool requireDead)
    {
        // deleted or gibbed or something, counts as dead
        if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null || TryComp<GhostBarPatronComponent>(mind.OwnedEntity, out _))
            return 1f;

        // dead is success
        if (_mind.IsCharacterDeadIc(mind))
            return 1f;

        // if the target has to be dead dead then don't check evac stuff
        if (requireDead)
            return 0f;

        // if evac is disabled then they really do have to be dead
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
            return 0f;

        // target is escaping so you fail
        if (_emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value))
            return 0f;

        // evac has left without the target, greentext since the target is afk in space with a full oxygen tank and coordinates off.
        if (_emergencyShuttle.ShuttlesLeft)
            return 1f;

        // if evac is still here and target hasn't boarded, show 50% to give you an indicator that you are doing good
        return _emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;
    }
}
