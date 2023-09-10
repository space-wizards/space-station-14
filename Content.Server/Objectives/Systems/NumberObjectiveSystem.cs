using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Provides API for other components, handles picking the count and setting the title and description.
/// </summary>
public sealed class NumberObjectiveSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NumberObjectiveComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<NumberObjectiveComponent, ObjectiveGetInfoEvent>(OnGetInfo);
    }

    private void OnAssigned(EntityUid uid, NumberObjectiveComponent comp, ref ObjectiveAssignedEvent args)
    {
        comp.Target = _random.Next(comp.Min, comp.Max);
    }

    private void OnGetInfo(EntityUid uid, NumberObjectiveComponent comp, ref ObjectiveGetInfoEvent args)
    {
        if (comp.Title != null)
            args.Info.Title = Loc.GetString(comp.Title, ("count", comp.Target));

        if (comp.Description != null)
            args.Info.Description = Loc.GetString(comp.Description, ("count", comp.Target));
    }

    /// <summary>
    /// Gets the objective's target count.
    /// </summary>
    public int GetTarget(EntityUid uid, NumberObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return 0;

        return comp.Target;
    }
}
