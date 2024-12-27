using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IIconConfig : ISheetletConfig
{
    public ResPath HelpIconPath { get; }
    public ResPath CrossIconPath { get; }
    public ResPath InvertedTriangleIconPath { get; }


}
