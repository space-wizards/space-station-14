using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class ObsessedKillPersonConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObsessedPickRandomPersonComponent, ObjectiveAssignedEvent>(OnPersonAssigned);
    }

    private void OnPersonAssigned(EntityUid objectiveUid, ObsessedPickRandomPersonComponent randomPersonComponent, ref ObjectiveAssignedEvent args)
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
        var targetOfJealousy = Comp<ObsessedRoleComponent>(args.MindId).TargetofJealousy;

        //we cant be obsessed with the person we're jealous of for spending time with the person we're obsessed with
        if (targetOfJealousy != null)
            listOfExclusions.Add((EntityUid) targetOfJealousy);
        //we cant be obsessed with ourselves
        listOfExclusions.Add(args.MindId);

        var allHumans = _mind.GetAliveHumansExcept(listOfExclusions);
        // no other humans to kill
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        EntityUid targetWeNeedToKill = _random.Pick(allHumans);

        //if we dont have a target of affection
        if (Comp<ObsessedRoleComponent>(args.MindId).TargetofAffection == null)
        {
            //set the target of affection to the person we need to kill (They're our final target)
            Comp<ObsessedRoleComponent>(args.MindId).TargetofAffection = targetWeNeedToKill;
            _target.SetTarget(objectiveUid, targetWeNeedToKill, target);
        }
    }
}
