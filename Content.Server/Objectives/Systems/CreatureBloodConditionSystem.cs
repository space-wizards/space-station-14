using Content.Server.Objectives.Components;
using Content.Shared.Creatures.TheCreature;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class CreatureBloodConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CreatureBloodConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, CreatureBloodConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.Mind, _number.GetTarget(uid));
    }

    private float GetProgress(MindComponent mind, int target)
    {
        if (target <= 0)
            return 1f;

        if (mind.OwnedEntity is not { } ent || !TryComp<CreatureComponent>(ent, out var creature))
            return 0f;

        return Math.Min(1f, creature.BloodConsumedTotal / target);
    }
}
