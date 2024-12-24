using Content.Server.DeltaV.Objectives.Components;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;

namespace Content.Server.DeltaV.Objectives.Systems;

/// <summary>
/// Handles teach a lesson condition logic, does not assign target.
/// </summary>
public sealed class TeachLessonConditionSystem : EntitySystem
{
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    // TODO: subscribe by ref at some point in the future
    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        // Get the mind of the entity that just died (if it has one)
        if (!_mind.TryGetMind(args.Target, out var mindId, out _))
            return;

        // Get all TeachLessonConditionComponent entities
        var query = EntityQueryEnumerator<TeachLessonConditionComponent, TargetObjectiveComponent>();

        while (query.MoveNext(out var uid, out _, out var targetObjective))
        {
            // Check if this objective's target matches the entity that died
            if (targetObjective.Target != mindId)
                continue;

            _codeCondition.SetCompleted(uid);
        }
    }
}
