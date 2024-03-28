using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Exterminator.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles copying the exterminator's target override to this objective.
/// </summary>
public sealed class ExterminatorTargetOverrideSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExterminatorTargetOverrideComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnAssigned(Entity<ExterminatorTargetOverrideComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        // use random target when spawned naturally
        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<ExterminatorTargetComponent>(user, out var target) || target.Target == null)
            return;

        // force target when admin smite is used
        _target.SetTarget(ent, target.Target.Value);
        RemComp<ExterminatorTargetComponent>(user);
    }
}
