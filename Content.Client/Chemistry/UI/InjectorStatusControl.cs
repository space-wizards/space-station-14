using Content.Client.Chemistry.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class InjectorStatusControl : Control
{
    private readonly InjectorComponent _parent;
    private readonly RichTextLabel _label;

    public InjectorStatusControl(InjectorComponent parent)
    {
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);

        Update();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!_parent.UiUpdateNeeded)
            return;
        Update();
    }

    public void Update()
    {
        _parent.UiUpdateNeeded = false;

        //Update current volume and injector state
        var modeStringLocalized = _parent.CurrentMode switch
        {
            SharedInjectorComponent.InjectorToggleMode.Draw => Loc.GetString("injector-draw-text"),
            SharedInjectorComponent.InjectorToggleMode.Inject => Loc.GetString("injector-inject-text"),
            _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };
        _label.SetMarkup(Loc.GetString("injector-volume-label",
            ("currentVolume", _parent.CurrentVolume),
            ("totalVolume", _parent.TotalVolume),
            ("modeString", modeStringLocalized)));
    }
}
