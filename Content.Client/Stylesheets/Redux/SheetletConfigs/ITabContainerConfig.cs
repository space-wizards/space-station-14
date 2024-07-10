using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface ITabContainerConfig : ISheetletConfig
{
    public ResPath TabContainerPanelPath { get; }
}
