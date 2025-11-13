using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ITabContainerConfig : ISheetletConfig
{
    public ResPath TabContainerPanelPath { get; }
}
