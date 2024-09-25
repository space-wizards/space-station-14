using System.Numerics;
using Content.Client.Stylesheets.Redux.Palette;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface IButtonConfig : ISheetletConfig
{
    public ResPath BaseButtonPath { get; }
    public ResPath OpenLeftButtonPath { get; }
    public ResPath OpenRightButtonPath { get; }
    public ResPath OpenBothButtonPath { get; }
    public ResPath SmallButtonPath { get; }
    public ResPath RoundedButtonPath { get; }
    public ResPath RoundedButtonBorderedPath { get; }

    public ColorPalette ButtonPalette { get; }
    public ColorPalette PositiveButtonPalette { get; }
    public ColorPalette NegativeButtonPalette { get; }
}
