using Content.Client.Power.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Power;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Power.UI;

/// <summary>
/// Displays battery charge information for <see cref="BatteryItemStatusComponent"/>.
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
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        // Get battery info using the shared event
        var batteryEvent = new GetBatteryInfoEvent();
        _entityManager.EventBus.RaiseLocalEvent(_parent.Owner, ref batteryEvent);

        if (!batteryEvent.HasBattery)
            return default;

        var chargePercent = (int)(batteryEvent.ChargePercent * 100);

        // Check if item has toggle state (like stun baton)
        bool? toggleState = null;
        if (_parent.Comp.ShowToggleState && _entityManager.TryGetComponent(_parent.Owner, out ItemToggleComponent? toggle))
        {
            toggleState = toggle.Activated;
        }

        return new Data(chargePercent, toggleState);
    }

    protected override void Update(in Data data)
    {
        var markup = Loc.GetString("battery-status-charge", ("percent", data.ChargePercent));

        if (data.ToggleState.HasValue)
        {
            var toggleText = data.ToggleState.Value
                ? Loc.GetString("battery-status-on")
                : Loc.GetString("battery-status-off");
            markup += "\n" + toggleText;
        }

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(int ChargePercent, bool? ToggleState);
}
