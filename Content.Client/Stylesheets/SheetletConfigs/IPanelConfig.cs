using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IPanelConfig : ISheetletConfig
{
    public ResPath GeometricPanelBorderPath { get; }
    public ResPath BlackPanelDarkThinBorderPath { get; }
}
