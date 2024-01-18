using Content.Shared.Actions;
using Content.Shared.Terminator.Components;

namespace Content.Shared.Terminator.Systems;

/// <summary>
/// Handles curse action adding but not action usage.
/// </summary>
public abstract class SharedTerminatorSystem : EntitySystem
{
    [Dependency] protected readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TerminatorComponent, MapInitEvent>(OnMapInit);
    }

    protected virtual void OnMapInit(Entity<TerminatorComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.CurseActionEntity, ent.Comp.CurseAction);
    }
}
