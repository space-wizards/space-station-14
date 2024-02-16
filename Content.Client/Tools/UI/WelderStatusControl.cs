using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Client.Tools.Components;
using Content.Shared.Item;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using ItemToggleComponent = Content.Shared.Item.ItemToggle.Components.ItemToggleComponent;

namespace Content.Client.Tools.UI;

public sealed class WelderStatusControl : Control
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly WelderComponent _parent;
    private readonly ItemToggleComponent? _toggleComponent;
    private readonly RichTextLabel _label;

    public WelderStatusControl(Entity<WelderComponent> parent)
    {
        _parent = parent;
        _entMan = IoCManager.Resolve<IEntityManager>();
        if (_entMan.TryGetComponent<ItemToggleComponent>(parent, out var itemToggle))
            _toggleComponent = itemToggle;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);

        UpdateDraw();
    }

    /// <inheritdoc />
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_parent.UiUpdateNeeded)
        {
            return;
        }
        Update();
    }

    public void Update()
    {
        _parent.UiUpdateNeeded = false;

        var fuelCap = _parent.FuelCapacity;
        var fuel = _parent.Fuel;
        var lit = false;
        if (_toggleComponent != null)
        {
            lit = _toggleComponent.Activated;
        }

        _label.SetMarkup(Loc.GetString("welder-component-on-examine-detailed-message",
            ("colorName", fuel < fuelCap / 4f ? "darkorange" : "orange"),
            ("fuelLeft", Math.Round(fuel, 1)),
            ("fuelCapacity", fuelCap),
            ("status", Loc.GetString(lit ? "welder-component-on-examine-welder-lit-message" : "welder-component-on-examine-welder-not-lit-message"))));
    }
}
