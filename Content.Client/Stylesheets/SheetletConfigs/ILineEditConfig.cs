using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface ILineEditConfig : ISheetletConfig
{
    public ResPath LineEditPath { get; }
}
