using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Implants.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Implants.UI;

public sealed class ImplanterStatusControl : Control
{
    private readonly ImplanterComponent _parent;
    private readonly RichTextLabel _label;

    public ImplanterStatusControl(ImplanterComponent parent)
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

    private void Update()
    {
        _parent.UiUpdateNeeded = false;

        var modeStringLocalized = _parent.CurrentMode switch
        {
            ImplanterToggleMode.Draw => Loc.GetString("injector-draw-text"),
            ImplanterToggleMode.Inject => Loc.GetString("injector-inject-text"),
            _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };

        _label.SetMarkup(Loc.GetString("implanter-label", ("modeString", modeStringLocalized)));
    }
}
