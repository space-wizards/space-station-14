using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IPlaceholderConfig : ISheetletConfig
{
    public ResPath PlaceholderPath { get; }
}

