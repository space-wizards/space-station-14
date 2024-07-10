using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface ITooltipConfig : ISheetletConfig
{
    public ResPath TooltipBoxPath { get; }
    public ResPath WhisperBoxPath { get; }
}
