using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IRadialMenuConfig : ISheetletConfig
{
    public ResPath ButtonNormalPath { get; }
    public ResPath ButtonHoverPath { get; }
    public ResPath CloseNormalPath { get; }
    public ResPath CloseHoverPath { get; }
    public ResPath BackNormalPath { get; }
    public ResPath BackHoverPath { get; }
}
