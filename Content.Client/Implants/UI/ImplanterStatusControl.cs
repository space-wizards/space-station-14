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
        _label.MaxWidth = 350;
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
            ImplanterToggleMode.Draw => Loc.GetString("implanter-draw-text"),
            ImplanterToggleMode.Inject => Loc.GetString("implanter-inject-text"),
            _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };

        var (implantName, implantDescription) = _parent.ImplanterSlot.HasItem switch
        {
            false => (Loc.GetString("implanter-empty-text"), ""),
            true => (_parent.ImplantData.Item1, _parent.ImplantData.Item2),
        };


        _label.SetMarkup(Loc.GetString("implanter-label",
                ("implantName", implantName),
                ("implantDescription", implantDescription),
                ("modeString", modeStringLocalized),
                ("lineBreak", "\n")));
    }
}
