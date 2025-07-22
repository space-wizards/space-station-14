using System.Linq;
using Content.Server._Starlight.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Objectives.Systems;

public sealed class PickObjectiveTargetDepartmentSystem: EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<PickRandomDepartmentComponent, ObjectiveAssignedEvent>(OnRandomDepartmentAssigned);
    }

    private void OnRandomDepartmentAssigned(Entity<PickRandomDepartmentComponent> ent, ref ObjectiveAssignedEvent args)
    {
        if (!TryComp<DepartmentObjectiveComponent>(ent.Owner, out var target))
        {
            args.Cancelled = true;
            return;
        }
        
        //  target already assigned
        if (target.TargetDepartment != null)
            return;

        var depProtos = _protoMan.EnumeratePrototypes<DepartmentPrototype>()
            .Where(d => !ent.Comp.Exclude.Contains(d.ID));
        if(ent.Comp.ExcludeNonPrimary)
            depProtos = depProtos.Where(d => d.Primary);
        if (ent.Comp.ExcludeHidden)
            depProtos = depProtos.Where(d => !d.EditorHidden);

        var departments = depProtos.ToList();
        
        if (departments.Count == 0)
        {
            args.Cancelled = true;
            return;
        }
        
        target.TargetDepartment = _random.Pick(departments.ToList());
    }
}