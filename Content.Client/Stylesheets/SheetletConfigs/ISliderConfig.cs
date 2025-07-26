using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ISliderConfig : ISheetletConfig
{
    public ResPath SliderFillPath { get; }
    public ResPath SliderOutlinePath { get; }
    public ResPath SliderGrabber { get; }
}
