using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface IPlaceholderConfig : ISheetletConfig
{
    public ResPath PlaceholderPath { get; }
}

