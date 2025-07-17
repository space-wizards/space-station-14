using Content.Server._Starlight.Objectives.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Objectives.Systems;

public sealed class DepartmentObjectiveSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DepartmentObjectiveComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
    }

    private void OnAfterAssign(Entity<DepartmentObjectiveComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (ent.Comp.TargetDepartment is not { } target)
            return;

        var departmentName = Loc.GetString(_protoMan.Index(target).Name);
        _meta.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("department", departmentName)), args.Meta);
    }
    
}
