using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IStripebackConfig : ISheetletConfig
{
    public ResPath StripebackPath { get; }
}
