using Content.Server.Objectives.Components;
using Content.Shared.Creatures.SpaceLeech;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed partial class SpaceLeechBloodConditionSystem : EntitySystem
{
    [Dependency] private NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpaceLeechBloodConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<SpaceLeechBloodConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, _number.GetTarget(ent));
    }

    private float GetProgress(MindComponent mind, int target)
    {
        if (target <= 0)
            return 1f;

        if (mind.OwnedEntity is not { } owned || !TryComp<SpaceLeechComponent>(owned, out var leech))
            return 0f;

        return Math.Min(1f, leech.BloodConsumedTotal.Float() / target);
    }
}
