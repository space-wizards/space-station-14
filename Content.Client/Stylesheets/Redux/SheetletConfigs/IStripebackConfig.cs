using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface IStripebackConfig : ISheetletConfig
{
    public ResPath StripebackPath { get; }
}
