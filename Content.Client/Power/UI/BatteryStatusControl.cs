using Content.Shared.Power.Components;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
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
    private readonly Entity<BatteryItemStatusComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly RichTextLabel _label;

    public BatteryStatusControl(
        Entity<BatteryItemStatusComponent> parent,
        IEntityManager entityManager)
    {
        _parent = parent;
        _entityManager = entityManager;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        // Do not add battery status to guns that already show an ammo counter.
        if (_entityManager.TryGetComponent(_parent.Owner, out AmmoCounterComponent? _))
            return default;

        if (!_entityManager.TryGetComponent(_parent.Owner, out BatteryItemStatusComponent? _))
            return default;

        var chargePercent = _parent.Comp.ChargePercent;

        bool? toggleState = null;
        if (_parent.Comp.ShowToggleState)
        {
            if (_entityManager.TryGetComponent(_parent.Owner, out ItemToggleComponent? toggle))
                toggleState = toggle.Activated;
            else if (_entityManager.TryGetComponent(_parent.Owner, out PowerCellDrawComponent? powerDraw))
                toggleState = powerDraw.Enabled;
        }

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
