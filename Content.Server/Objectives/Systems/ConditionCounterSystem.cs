using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using JetBrains.Annotations;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// This system handles returning the progress for CounterConditionComponents,
/// which simple increment for traitor NumberObjectives, e.g. cut into 12 envelopes.
/// </summary>
public sealed class CounterConditionSystem : EntitySystem
{
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private EntityQuery<CounterConditionComponent> _compQuery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CounterConditionComponent, ObjectiveGetProgressEvent>(OnCounterGetProgress);
    }

    private void OnCounterGetProgress(Entity<CounterConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(ent, _number.GetTarget(ent.Owner));
    }

    private float GetProgress(Entity<CounterConditionComponent> ent, int target)
    {
        // prevent divide-by-zero
        if (target == 0)
            return 1f;

        if (ent.Comp.Count >= target)
            return 1f;

        return (float)ent.Comp.Count / target;
    }

    [PublicAPI]
    public void IncreaseCount(Entity<CounterConditionComponent?> objective)
    {
        if (_compQuery.Resolve(objective, ref objective.Comp))
        {
            objective.Comp.Count++;
        }
    }
}
