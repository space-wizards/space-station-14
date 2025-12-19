using Content.Client.Power.Components;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Power.UI;

/// <summary>
/// Displays battery charge information for entities with <see cref="BatteryItemStatusComponent"/>.
/// </summary>
/// <seealso cref="BatteryItemStatusSystem"/>
public sealed class BatteryStatusControl : PollingItemStatusControl<BatteryStatusControl.Data>
{
    private readonly EntityUid _parent;
    private readonly IEntityManager _entityManager;
    private readonly SharedBatterySystem _battery;
    private readonly PowerCellSystem _powerCell;
    private readonly RichTextLabel _label;

    public BatteryStatusControl(
        EntityUid parent,
        IEntityManager entityManager,
        SharedBatterySystem battery,
        PowerCellSystem powerCell)
    {
        _parent = parent;
        _entityManager = entityManager;
        _battery = battery;
        _powerCell = powerCell;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        // Do not add battery status to guns that already show an ammo counter.
        if (_entityManager.TryGetComponent(_parent, out AmmoCounterComponent? _))
            return default;

        // Battery charge level.
        int? chargePercent = null;
        if (_powerCell.TryGetBatteryFromEntityOrSlot(_parent, out var battery))
            chargePercent = (int)(_battery.GetChargeLevel(battery.Value.AsNullable()) * 100);

        // On/off state.
        bool? toggleState = null;
        if (_entityManager.TryGetComponent(_parent, out ItemToggleComponent? toggle))
            toggleState = toggle.Activated;
        else if (_entityManager.TryGetComponent(_parent, out PowerCellDrawComponent? powerDraw))
            toggleState = powerDraw.Enabled;

        return new Data(chargePercent, toggleState);
    }

    protected override void Update(in Data data)
    {
        var markup = "";
        if (data.ChargePercent.HasValue)
            markup = Loc.GetString("battery-status-charge", ("percent", data.ChargePercent));

        if (data.ToggleState.HasValue)
        {
            var stateValue = data.ToggleState.Value ? "on" : "off";
            var stateColor = Loc.GetString("battery-status-switchable-state", ("state", stateValue));
            var stateLine = Loc.GetString("battery-status-state", ("state", stateColor));
            markup += "\n" + stateLine;
        }

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(int? ChargePercent, bool? ToggleState);
}
