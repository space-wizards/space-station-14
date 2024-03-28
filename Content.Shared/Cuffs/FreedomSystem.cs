using Content.Shared.Actions;
using Content.Shared.Cuffs.Components;

namespace Content.Shared.Cuffs;

/// <summary>
/// Handles <see cref="FreedomComponent"/>'s freedom action.
/// </summary>
public sealed class FreedomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FreedomComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FreedomComponent, BreakFreeEvent>(OnBreakFree);
    }

    private void OnMapInit(Entity<FreedomComponent> ent, ref MapInitEvent args)
    {
        // test is mapinit for some reason
        if (ent.Comp.Action == string.Empty)
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnBreakFree(Entity<FreedomComponent> ent, ref BreakFreeEvent args)
    {
        if (!TryComp<CuffableComponent>(ent, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(target: ent, user: ent, cuffsToRemove: cuffs.LastAddedCuffs, cuffs);
        args.Handled = true;
    }
}
