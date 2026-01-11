using Content.Client.Stylesheets.Palette;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.SheetletConfigs;

public interface IButtonConfig : ISheetletConfig
{
    public ResPath BaseButtonPath { get; }
    public ResPath OpenLeftButtonPath { get; }
    public ResPath OpenRightButtonPath { get; }
    public ResPath OpenBothButtonPath { get; }
    public ResPath SmallButtonPath { get; }
    public ResPath RoundedButtonPath { get; }
    public ResPath RoundedButtonBorderedPath { get; }
    public ResPath MonotoneBaseButtonPath { get; }
    public ResPath MonotoneOpenLeftButtonPath { get; }
    public ResPath MonotoneOpenRightButtonPath { get; }
    public ResPath MonotoneOpenBothButtonPath { get; }

    public ColorPalette ButtonPalette { get; }
    public ColorPalette PositiveButtonPalette { get; }
    public ColorPalette NegativeButtonPalette { get; }
}
