using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Implants.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
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
            ImplanterToggleMode.Draw => Loc.GetString("implanter-draw-text"),
            ImplanterToggleMode.Inject => Loc.GetString("implanter-inject-text"),
            _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };

        var entitiesStringLocalized = _parent.NumberOfEntities switch
        {
            0 => Loc.GetString("implanter-empty-text"),
            1 => Loc.GetString("implanter-implant-text", ("implantName", _parent.ImplantData.Item1), ("implantDescription", _parent.ImplantData.Item2), ("lineBreak", "\n")),
            _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };


        _label.SetMarkup(Loc.GetString("implanter-label", ("currentEntities", entitiesStringLocalized), ("modeString", modeStringLocalized), ("lineBreak", "\n")));
    }
}
