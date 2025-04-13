using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles keep alive condition logic.
/// </summary>
public sealed class EscortTargetConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTargetAliveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, RandomTargetAliveComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(target.Value);
    }

    private float GetProgress(EntityUid target)
    {
        if (!TryComp<MindComponent>(target, out var mind))
            return 0f;

        // If they're dead, you've failed.
        if (_mind.IsCharacterDeadIc(mind) || !mind.OwnedEntity.HasValue)
            return 0f;

        // They have to make it to CentComm, but unlike EscapeShuttleCondition, they can be restrained.
        return _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) ? 1f : 0f;
    }
}
