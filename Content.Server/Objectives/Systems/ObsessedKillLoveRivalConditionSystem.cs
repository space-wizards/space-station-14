using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class ObsessedKillLoveRivalConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObsessedPickRandomRivalComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
    }

    private void OnPersonAssigned(EntityUid objectiveUid, ObsessedPickRandomRivalComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (Comp<ObsessedRoleComponent>(args.MindId).TargetofAffection != null)
        {
            // invalid objective prototype
            if (!TryComp<TargetObjectiveComponent>(objectiveUid, out var target))
            {
                args.Cancelled = true;
                return;
            }

            // target already assigned
            if (target.Target != null)
                return;

            var listOfExclusions = new List<EntityUid>();
            var targetOfAffection = Comp<ObsessedRoleComponent>(args.MindId).TargetofAffection;

            //we cant hate the person we're obsessed with
            if (targetOfAffection != null)
                listOfExclusions.Add((EntityUid) targetOfAffection);
            //we cant be obsessed with ourselves
            listOfExclusions.Add(args.MindId);

            var allHumans = _mind.GetAliveHumansExcept(listOfExclusions);
            // no other humans to kill
            if (allHumans.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            EntityUid targetWeNeedToKill = GetNewTarget(allHumans);

            if (Comp<ObsessedRoleComponent>(args.MindId).TargetofJealousy == null)
            {
                AssignTarget(ref objectiveUid, args.MindId, targetWeNeedToKill, target);
            }
        }
    }
    private EntityUid GetNewTarget(List<EntityUid> allHumans)
    {
        return _random.Pick(allHumans);
    }
    private void AssignTarget(ref EntityUid objectiveUid, EntityUid mindIdOfPlayer, EntityUid targetWeNeedToKill, TargetObjectiveComponent? target = null)
    {
        Comp<ObsessedRoleComponent>(mindIdOfPlayer).TargetofJealousy = targetWeNeedToKill;
        _target.SetTarget(objectiveUid, targetWeNeedToKill, target);
    }
}
