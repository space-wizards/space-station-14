using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;

    private void InitializePower()
    {
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BoundUIOpenedEvent>(OnBatteryOpened);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BoundUIClosedEvent>(OnBatteryClosed);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<ActivatableUIRequiresPowerCellComponent, ActivatableUIOpenAttemptEvent>(OnBatteryOpenAttempt);
    }

    private void OnToggled(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref ItemToggledEvent args)
    {
        // only close ui when losing power
        if (args.Activated || !TryComp<ActivatableUIComponent>(ent, out var activatable))
            return;

        if (activatable.Key == null)
        {
            Log.Error($"Encountered null key in activatable ui on entity {ToPrettyString(ent)}");
            return;
        }

        _uiSystem.CloseUi(ent.Owner, activatable.Key);
    }

    private void OnBatteryOpened(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, BoundUIOpenedEvent args)
    {
        var activatable = Comp<ActivatableUIComponent>(uid);

        if (!args.UiKey.Equals(activatable.Key))
            return;

        _toggle.TryActivate(uid);
    }

    private void OnBatteryClosed(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, BoundUIClosedEvent args)
    {
        var activatable = Comp<ActivatableUIComponent>(uid);

        if (!args.UiKey.Equals(activatable.Key))
            return;

        // Stop drawing power if this was the last person with the UI open.
        if (!_uiSystem.IsUiOpen(uid, activatable.Key))
            _toggle.TryDeactivate(uid);
    }

    private void OnBatteryStateChanged(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref BatteryStateChangedEvent args)
    {
        // Deactivate when empty.
        if (args.NewState != BatteryState.Empty)
            return;

        var activatable = Comp<ActivatableUIComponent>(ent);
        if (activatable.Key != null)
            _uiSystem.CloseUi(ent.Owner, activatable.Key);
    }

    private void OnBatteryOpenAttempt(EntityUid uid, ActivatableUIRequiresPowerCellComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Check if we have the appropriate drawrate / userate to even open it.
        // Don't pass in the user for the popup if silent.
        if (!_cell.HasActivatableCharge(uid, user: args.Silent ? null : args.User, predicted: true) ||
            !_cell.HasDrawCharge(uid, user: args.Silent ? null : args.User, predicted: true))
        {
            args.Cancel();
        }
    }
}
