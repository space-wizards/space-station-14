// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Objectives.Components;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologyAssemblyConditionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly UnitologySubmissionConditionSystem _submissionCondition = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UnitologyAssemblyConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<UnitologyAssemblyConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAfterAssign(Entity<UnitologyAssemblyConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        ent.Comp.Target = _submissionCondition.GetTarget();
    }

    private void OnGetProgress(Entity<UnitologyAssemblyConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.Target <= 0)
            ent.Comp.Target = _submissionCondition.GetTarget();

        args.Progress = CalculateProgress(ent.Comp.Target, ent.Comp.Range);
    }

    public bool TryGetProgress(EntityUid objective,
        out float progress,
        UnitologyAssemblyConditionComponent? component = null)
    {
        progress = 0f;
        if (!Resolve(objective, ref component) || component.Target <= 0)
            return false;

        progress = CalculateProgress(component.Target, component.Range);
        return true;
    }

    private float CalculateProgress(int target, float range)
    {
        if (target == 0)
            return 1f;

        var nearby = 0;
        var heads = AllEntityQuery<UnitologyHeadComponent>();
        while (heads.MoveNext(out var head, out _))
        {
            nearby = Math.Max(nearby,
                _lookup.GetEntitiesInRange<UnitologyEnslavedComponent>(Transform(head).Coordinates, range).Count);
        }

        return MathF.Min((float) nearby / target, 1f);
    }
}
