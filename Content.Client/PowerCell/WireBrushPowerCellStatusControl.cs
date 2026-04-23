using System;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.PowerCell;

public sealed class WireBrushPowerCellStatusControl : PollingItemStatusControl<WireBrushPowerCellStatusControl.Data>
{
    private readonly Entity<PowerCellSlotComponent> _parent;
    private readonly PowerCellSystem _powerCell;
    private readonly SharedBatterySystem _battery;
    private readonly RichTextLabel _label;

    public WireBrushPowerCellStatusControl(
        Entity<PowerCellSlotComponent> parent,
        PowerCellSystem powerCell,
        SharedBatterySystem battery)
    {
        _parent = parent;
        _powerCell = powerCell;
        _battery = battery;

        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);
    }

    protected override Data PollData()
    {
        if (!_powerCell.TryGetBatteryFromSlot(_parent.AsNullable(), out var battery))
            return new Data(false, 0f, 0);

        var chargeLevel = _battery.GetChargeLevel(battery.Value.AsNullable());
        var chargePercent = Math.Clamp((int) MathF.Round(chargeLevel * 100f), 0, 100);
        return new Data(true, chargeLevel, chargePercent);
    }

    protected override void Update(in Data data)
    {
        if (!data.HasBattery)
        {
            _label.SetMarkup(Loc.GetString("power-cell-item-status-empty"));
            return;
        }

        var color = data.ChargeLevel switch
        {
            <= 0.15f => "#d14c32",
            <= 0.5f => "#d7a72c",
            _ => "#8dc63f",
        };

        _label.SetMarkup(Loc.GetString("power-cell-item-status",
            ("color", color),
            ("charge", data.ChargePercent)));
    }

    public readonly record struct Data(bool HasBattery, float ChargeLevel, int ChargePercent);
}
