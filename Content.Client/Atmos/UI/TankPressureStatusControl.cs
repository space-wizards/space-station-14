using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Atmos.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Displays gas tank pressure information for <see cref="GasTankComponent"/>.
/// </summary>
/// <seealso cref="GasTankSystem"/>
public sealed partial class TankPressureStatusControl : PollingItemStatusControl<TankPressureStatusControl.Data>
{
    private readonly Entity<GasTankComponent> _parent;
    private readonly RichTextLabel _label;

    public TankPressureStatusControl(Entity<GasTankComponent> parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);

        // Default placeholder.
        var markup = Loc.GetString("tank-pressure-status", ("pressure", 0));
        var stateColor = Loc.GetString("tank-status-switchable-state", ("state", "closed"));
        var stateLine = Loc.GetString("tank-status-state", ("state", stateColor));
        markup += "\n" + stateLine;
        _label.SetMarkup(markup);
    }

    protected override Data PollData()
    {
        var tank = _parent.Comp;
        var pressureKpa = tank.Air.Pressure;
        var isValveOpen = tank.ReleaseValveOpen;

        return new Data(pressureKpa, isValveOpen);
    }

    protected override void Update(in Data data)
    {
        var markup = Loc.GetString("tank-pressure-status", ("pressure", $"{data.PressureKpa:F1}"));

        var stateValue = data.IsValveOpen ? "open" : "closed";
        var stateColor = Loc.GetString("tank-status-switchable-state", ("state", stateValue));
        var stateLine = Loc.GetString("tank-status-state", ("state", stateColor));
        markup += "\n" + stateLine;

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(float PressureKpa, bool IsValveOpen);
}
