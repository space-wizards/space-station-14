using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Chemistry.UI;

public sealed class SolutionTransferStatusControl : Control
{
    private readonly ITransferControlValues _parent;
    private readonly RichTextLabel _label;
    private readonly TransferControlTranlates _translates;
    private readonly bool _isShowVolume;
    private readonly bool _isShowToggle;

    public SolutionTransferStatusControl(ITransferControlValues parent,
        TransferControlTranlates tranlates, bool isShowVolume, bool isShowToggleMode)
    {
        _parent = parent;
        _translates = tranlates;
        _isShowVolume = isShowVolume;
        _isShowToggle = isShowToggleMode;

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

        var mainLabelTranslate = "transfer-status-control";

        //Update current volume and injector state
        var modeStringLocalized = "";
        if (_isShowToggle)
        {
            mainLabelTranslate += "-toggle-mode";
            modeStringLocalized = _parent.CurrentMode switch
            {
                SharedTransferToggleMode.Draw => Loc.GetString(_translates.DrawModeText),
                SharedTransferToggleMode.Inject => Loc.GetString(_translates.InjectModeText),
                _ => Loc.GetString(_translates.InvalidModeText)
            };
        }

        var volumeLabelLocalized = "";
        if (_isShowVolume)
        {
            mainLabelTranslate += "-volume-label";
            volumeLabelLocalized = Loc.GetString(_translates.VolumeLabelText,
                ("currentVolume", _parent.CurrentVolume),
                ("totalVolume", _parent.TotalVolume));
        }

        _label.SetMarkup(Loc.GetString(mainLabelTranslate,
                ("volumeLabel", volumeLabelLocalized),
                ("modeString", modeStringLocalized)));
    }
}

public struct TransferControlTranlates
{
    public string DrawModeText;
    public string InjectModeText;
    public string InvalidModeText;
    public string VolumeLabelText;
}

public interface ITransferControlValues
{
    public FixedPoint2 CurrentVolume { get; set; }
    public FixedPoint2 TotalVolume { get; set; }
    public SharedTransferToggleMode? CurrentMode { get; set; }
    public bool UiUpdateNeeded { get; set; }
}


