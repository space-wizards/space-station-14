using Content.Client.Atmos.Components;
using Content.Client.Items.UI;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Client.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Atmos.UI;

/// <summary>
/// Displays gas tank pressure information for <see cref="TankPressureItemStatusComponent"/>.
/// </summary>
/// <seealso cref="TankPressureItemStatusSystem"/>
public sealed partial class TankPressureStatusControl : PollingItemStatusControl<TankPressureStatusControl.Data>
{
    private readonly Entity<TankPressureItemStatusComponent> _parent;
    private readonly IEntityManager _entityManager;
    private readonly RichTextLabel _label;

    public TankPressureStatusControl(
        Entity<TankPressureItemStatusComponent> parent,
        IEntityManager entityManager)
    {
        _parent = parent;
        _entityManager = entityManager;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);

        // Default placeholder.
        var markup = Loc.GetString("tank-pressure-status", ("pressure", 0));
        markup += "\n" + Loc.GetString("tank-status-closed");
        _label.SetMarkup(markup);
    }

    protected override Data PollData()
    {
        if (!_entityManager.TryGetComponent<GasTankComponent>(_parent.Owner, out var tank))
            return default;

        var pressureKpa = tank.InternalPressure;
        var isValveOpen = tank.ReleaseValveOpen;

        return new Data(pressureKpa, isValveOpen);
    }

    protected override void Update(in Data data)
    {
        var markup = Loc.GetString("tank-pressure-status", ("pressure", $"{data.PressureKpa:F1}"));

        var valveText = data.IsValveOpen
            ? Loc.GetString("tank-status-open")
            : Loc.GetString("tank-status-closed");
        markup += "\n" + valveText;

        _label.SetMarkup(markup);
    }

    public readonly record struct Data(float PressureKpa, bool IsValveOpen);
}
