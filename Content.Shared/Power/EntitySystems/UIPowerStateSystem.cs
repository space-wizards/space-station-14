using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// System for entities with <see cref="UIPowerStateComponent"/>.
/// Entities with this component will increase their power usage to a working state
/// when a UI on the entity is open.
/// </summary>
public sealed class UIPowerStateSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerStateSystem _powerState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UIPowerStateComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<UIPowerStateComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    private void OnUiClosed(Entity<UIPowerStateComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.Keys is null)
        {
            if (_ui.IsAnyUiOpen(ent.Owner))
                return;
        }
        else
        {
            if (_ui.IsUiOpen(ent.Owner, ent.Comp.Keys))
                return;
        }

        _powerState.SetWorkingState(ent.Owner, false);
    }

    private void OnUiOpened(Entity<UIPowerStateComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.Keys is not null && !ent.Comp.Keys.Contains(args.UiKey))
            return;

        _powerState.SetWorkingState(ent.Owner, true);
    }
}
