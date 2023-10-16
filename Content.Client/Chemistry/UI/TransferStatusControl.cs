using Content.Client.Chemistry.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class TransferStatucControl : Control
{
    private readonly ITransferControlValues _parent;
    private readonly RichTextLabel _label;

    public TransferStatucControl(ITransferControlValues parent)
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
            SharedTransferToggleMode.Draw => Loc.GetString("injector-draw-text"),
            SharedTransferToggleMode.Inject => Loc.GetString("injector-inject-text"),
            _ => Loc.GetString("injector-invalid-injector-toggle-mode")
        };
        _label.SetMarkup(Loc.GetString("injector-volume-label",
            ("currentVolume", _parent.CurrentVolume),
            ("totalVolume", _parent.TotalVolume),
            ("modeString", modeStringLocalized)));
    }
}

public struct TransferControlTranlates
{
    public string drawModeText;
    public string injectModeText;
    public string invalidModeText;
    public string volumeLabelText;
}

public interface ITransferControlValues
{
    public FixedPoint2 CurrentVolume { get; set; }
    public FixedPoint2 TotalVolume { get; set; }
    public SharedTransferToggleMode CurrentMode { get; set; }
    public bool UiUpdateNeeded { get; set; }
}


