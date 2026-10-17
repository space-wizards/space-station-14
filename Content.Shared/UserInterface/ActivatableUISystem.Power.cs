using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Content.Shared.Power;
using Content.Shared.Power.Components;

namespace Content.Shared.UserInterface;

public sealed partial class ActivatableUISystem
{
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private PowerCellSystem _cell = default!;

    [SubscribeLocalEvent]
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

        _ui.CloseUi(ent.Owner, activatable.Key);
    }

    [SubscribeLocalEvent]
    private void OnBatteryOpened(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref BoundUIOpenedEvent args)
    {
        var activatable = Comp<ActivatableUIComponent>(ent);

        if (!args.UiKey.Equals(activatable.Key))
            return;

        _toggle.TryActivate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBatteryClosed(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref BoundUIClosedEvent args)
    {
        var activatable = Comp<ActivatableUIComponent>(ent);

        if (!args.UiKey.Equals(activatable.Key))
            return;

        // Stop drawing power if this was the last person with the UI open.
        if (!_ui.IsUiOpen(ent.Owner, activatable.Key))
            _toggle.TryDeactivate(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBatteryStateChanged(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref BatteryStateChangedEvent args)
    {
        // Deactivate when empty.
        if (args.NewState != BatteryState.Empty)
            return;

        var activatable = Comp<ActivatableUIComponent>(ent);
        if (activatable.Key != null)
            _ui.CloseUi(ent.Owner, activatable.Key);
    }

    [SubscribeLocalEvent]
    private void OnBatteryOpenAttempt(Entity<ActivatableUIRequiresPowerCellComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Check if we have the appropriate drawrate / userate to even open it.
        // Don't pass in the user for the popup if silent.
        if (!_cell.HasActivatableCharge(ent.Owner, user: args.Silent ? null : args.User, predicted: true) ||
            !_cell.HasDrawCharge(ent.Owner, user: args.Silent ? null : args.User, predicted: true))
        {
            args.Cancel();
        }
    }
}
