using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Implants.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Implants.UI;

public sealed class ImplanterStatusControl : Control
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private readonly ImplanterComponent _parent;
    private readonly RichTextLabel _label;

    public ImplanterStatusControl(ImplanterComponent parent)
    {
        IoCManager.InjectDependencies(this);
        _parent = parent;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        _label.MaxWidth = 350;
        AddChild(new ClipControl { Children = { _label } });

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

        if (_parent.CurrentMode == ImplanterToggleMode.Draw)
        {
            string implantName = _parent.DeimplantChosen != null
                ? (_prototype.TryIndex(_parent.DeimplantChosen.Value, out EntityPrototype? implantProto) ? implantProto.Name : Loc.GetString("implanter-empty-text"))
                : Loc.GetString("implanter-empty-text");

            _label.SetMarkup(Loc.GetString("implanter-label-draw",
                    ("implantName", implantName),
                    ("modeString", modeStringLocalized)));
        }
        else
        {
            var implantName = _parent.ImplanterSlot.HasItem
                ? _parent.ImplantData.Item1
                : Loc.GetString("implanter-empty-text");

            _label.SetMarkup(Loc.GetString("implanter-label-inject",
                    ("implantName", implantName),
                    ("modeString", modeStringLocalized)));
        }
    }
}
