using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// System for entities with <see cref="UiPowerStateComponent"/>.
/// Entities with this component will increase their power usage to a working state
/// when a UI on the entity is open.
/// </summary>
public sealed class UiPowerStateSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerStateSystem _powerState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UiPowerStateComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<UiPowerStateComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    private void OnUiClosed(Entity<UiPowerStateComponent> ent, ref BoundUIClosedEvent args)
    {
        // If non-null, we're filtering by specific UI keys,
        // so check to see if this is one of them.
        if (ent.Comp.Keys is not null && !ent.Comp.Keys.Contains(args.UiKey))
            return;

        // Other UIs are still open.
        if (_ui.IsUiOpen(ent.Owner, args.UiKey))
            return;

        _powerState.SetWorkingState(ent.Owner, false);
    }

    private void OnUiOpened(Entity<UiPowerStateComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.Keys is not null && !ent.Comp.Keys.Contains(args.UiKey))
            return;

        _powerState.SetWorkingState(ent.Owner, true);
    }
}
