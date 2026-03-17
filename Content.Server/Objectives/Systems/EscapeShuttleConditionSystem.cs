using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class EscapeShuttleConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EscapeShuttleConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, EscapeShuttleConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        // not escaping alive if you're deleted/dead
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // You're not escaping if you're restrained!
        // Granting 50% as to allow for partial completion of the objective.
        if (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
            return _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) ? 0.5f : 0f;

        // Any emergency shuttle counts for this objective, but not pods.
        return _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) ? 1f : 0f;
    }
}
