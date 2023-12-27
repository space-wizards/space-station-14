using Content.Server.Objectives.Components;
using Content.Server.Terminator.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles copying the exterminator's target override to this objective.
/// </summary>
public sealed class TerminatorTargetOverrideSystem : EntitySystem
{
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorTargetOverrideComponent, ObjectiveAssignedEvent>(OnAssigned);
    }

    private void OnAssigned(EntityUid uid, TerminatorTargetOverrideComponent comp, ref ObjectiveAssignedEvent args)
    {
        if (args.Mind.OwnedEntity == null)
        {
            args.Cancelled = true;
            return;
        }

        var user = args.Mind.OwnedEntity.Value;
        if (!TryComp<TerminatorComponent>(user, out var terminator))
        {
            args.Cancelled = true;
            return;
        }

        // this exterminator has a target override so set its objective target accordingly
        if (terminator.Target != null)
            _target.SetTarget(uid, terminator.Target.Value);
    }
}
