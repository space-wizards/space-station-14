using Content.Shared.Power.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.PowerCell;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Power.UI;

/// <summary>
/// Displays battery charge information for entities with SharedBatteryItemComponent.
/// </summary>
public sealed class BatteryStatusControl : PollingItemStatusControl<BatteryStatusControl.Data>
{
    private readonly Entity<SharedBatteryItemComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly RichTextLabel _label;

    public BatteryStatusControl(
        Entity<SharedBatteryItemComponent> parent,
        IEntityManager entityManager)
    {
        _parent = parent;
        _entityManager = entityManager;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
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
        var markup = Loc.GetString("battery-status-charge", ("percent", data.ChargePercent));

        if (data.ToggleState.HasValue)
        {
            var stateValue = data.ToggleState.Value ? "on" : "off";
            var stateColor = Loc.GetString("battery-status-switchable-state", ("state", stateValue));
            var stateLine = Loc.GetString("battery-status-state", ("state", stateColor));
            markup += "\n" + stateLine;
        }

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(int ChargePercent, bool? ToggleState);
}
