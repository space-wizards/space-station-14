using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface ILineEditConfig : ISheetletConfig
{
    public ResPath LineEditPath { get; }
}
