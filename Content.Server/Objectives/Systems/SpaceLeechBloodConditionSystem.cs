using Content.Server.Objectives.Components;
using Content.Shared.Creatures.SpaceLeech;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class SpaceLeechBloodConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceLeechBloodConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, SpaceLeechBloodConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, _number.GetTarget(uid));
    }

    private float GetProgress(MindComponent mind, int target)
    {
        if (target <= 0)
            return 1f;

        if (mind.OwnedEntity is not { } ent || !TryComp<SpaceLeechComponent>(ent, out var leech))
            return 0f;

        return Math.Min(1f, leech.BloodConsumedTotal / target);
    }
}
