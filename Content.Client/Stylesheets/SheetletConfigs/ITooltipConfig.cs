using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ITooltipConfig : ISheetletConfig
{
    public ResPath TooltipBoxPath { get; }
    public ResPath WhisperBoxPath { get; }
}
