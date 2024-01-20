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

    private void OnAssigned(EntityUid uid, ExterminatorTargetOverrideComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<ExterminatorComponent>(user, out var exterminator))
        {
            args.Cancelled = true;
            return;
        }

        // this exterminator has a target override so set its objective target accordingly
        if (exterminator.Target is {} target)
            _target.SetTarget(uid, target);
    }
}
