using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Objectives.Systems;
using Content.Server.Roles;
using Content.Shared.Terminator.Components;
using Content.Shared.Terminator.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;

namespace Content.Server.Terminator.Systems;

public sealed class TerminatorSystem : SharedTerminatorSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly TerminatorRuleSystem _terminatorRule = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
        SubscribeLocalEvent<TerminatorComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnSpawned(EntityUid uid, TerminatorComponent comp, GhostRoleSpawnerUsedEvent args)
    {
        if (!TryComp<TerminatorTargetComponent>(args.Spawner, out var target))
            return;

        comp.Target = target.Target;
    }

    private void OnMindAdded(EntityUid uid, TerminatorComponent comp, MindAddedMessage args)
    {
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
            return;

        // give the player the role
        var mindId = mindContainer.Mind.Value;
        var mind = Comp<MindComponent>(mindId);
        _role.MindAddRole(mindId, new RoleBriefing
        {
            Briefing = Loc.GetString("terminator-role-briefing")
        }, mind);
        _role.MindAddRole(mindId, new TerminatorRole(), mind);

        // add the terminate objective
        foreach (var id in comp.Objectives)
        {
            _mind.TryAddObjective(mindId, mind, id);
        }

        // set its target
        // if there are multiple kill objectives they will all get set to the same target
        var target = args.Target;
        foreach (var objective in mind.AllObjectives)
        {
            if (!HasComp<KillPersonConditionComponent>(objective))
                continue;

            // if its random this will see what it picked
            // if there is already a target set this will set it
            if (target != null)
                _target.SetTarget(objective, target);
            _target.GetTarget(objective, ref target);
        }

        if (target == null)
        {
            Log.Error("Terminator {ToPrettyString(uid):player} had no terminate objective.");
            return;
        }

        var rule = _terminatorRule.GetRule(target);
        _terminatorRule.AddMind(rule, mindId);
    }
}
