using Content.Shared.Actions;
using Content.Shared.Exterminator.Components;

namespace Content.Shared.Exterminator.Systems;

/// <summary>
/// Handles curse action adding but not action usage.
/// </summary>
public abstract class SharedExterminatorSystem : EntitySystem
{
    [Dependency] protected readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExterminatorComponent, MapInitEvent>(OnMapInit);
    }

    protected virtual void OnMapInit(Entity<ExterminatorComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.CurseActionEntity, ent.Comp.CurseAction);
    }
}
