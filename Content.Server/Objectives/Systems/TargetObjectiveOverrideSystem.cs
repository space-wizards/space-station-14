using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class TargetObjectiveOverrideSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetObjectiveOverrideComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnAssigned(Entity<TargetObjectiveOverrideComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TargetOverrideComponent>(user, out var targetComp) || targetComp.Target == null)
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTarget(ent.Owner, targetComp.Target.Value);
    }
}
