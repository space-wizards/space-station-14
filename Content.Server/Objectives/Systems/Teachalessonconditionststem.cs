using Content.Server.Objectives.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles teach a lesson condition logic, does not assign target.
/// </summary>
public sealed class TeachLessonConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    private readonly List<EntityUid> _wasKilled = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeachLessonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
    }

    private void OnGetProgress(Entity<TeachLessonConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(ent, out var target))
            return;

        args.Progress = GetProgress(target.Value);
    }

    private float GetProgress(EntityUid target)
    {
        if (TryComp<MindComponent>(target, out var mind) && mind.OwnedEntity != null && !_mind.IsCharacterDeadIc(mind))
            return _wasKilled.Contains(target) ? 1f : 0f;

        _wasKilled.Add(target);
        return 1f;
    }

    // Clear the wasKilled list on round end
    private void OnRoundEnd(RoundRestartCleanupEvent  ev)
    {
        _wasKilled.Clear();
    }
}
