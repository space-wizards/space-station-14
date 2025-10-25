using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets;

public abstract class CommonStylesheet : PalettedStylesheet, IButtonConfig, IWindowConfig, IIconConfig, ITabContainerConfig,
    ISliderConfig, IRadialMenuConfig, IPlaceholderConfig, ITooltipConfig, IPanelConfig, INanoHeadingConfig,
    ILineEditConfig, IStripebackConfig, ICheckboxConfig
{
    /// <remarks>
    ///     This constructor will not access any virtual or abstract properties, so you can set them from your config.
    /// </remarks>
    protected CommonStylesheet(object config) : base(config) { }

    ResPath ICheckboxConfig.CheckboxUncheckedPath => new("checkbox_unchecked.svg.96dpi.png");
    ResPath ICheckboxConfig.CheckboxCheckedPath => new("checkbox_checked.svg.96dpi.png");

    ResPath IStripebackConfig.StripebackPath => new("stripeback.svg.96dpi.png");

    ResPath INanoHeadingConfig.NanoHeadingPath => new("nanoheading.svg.96dpi.png");

    ResPath ILineEditConfig.LineEditPath => new("lineedit.png");

    ResPath IPanelConfig.GeometricPanelBorderPath => new("geometric_panel_border.svg.96dpi.png");
    ResPath IPanelConfig.BlackPanelDarkThinBorderPath => new("black_panel_dark_thin_border.png");

    ResPath ITooltipConfig.TooltipBoxPath => new("tooltip.png");
    ResPath ITooltipConfig.WhisperBoxPath => new("whisper.png");

    ResPath IPlaceholderConfig.PlaceholderPath => new("placeholder.png");

    ResPath IRadialMenuConfig.ButtonNormalPath => new("Radial/button_normal.png");
    ResPath IRadialMenuConfig.ButtonHoverPath => new("Radial/button_hover.png");
    ResPath IRadialMenuConfig.CloseNormalPath => new("Radial/close_normal.png");
    ResPath IRadialMenuConfig.CloseHoverPath => new("Radial/close_hover.png");
    ResPath IRadialMenuConfig.BackNormalPath => new("Radial/back_normal.png");
    ResPath IRadialMenuConfig.BackHoverPath => new("Radial/back_hover.png");

    ResPath ISliderConfig.SliderFillPath => new("slider_fill.svg.96dpi.png");

    ResPath ISliderConfig.SliderOutlinePath => new("slider_outline.svg.96dpi.png");

    ResPath ISliderConfig.SliderGrabber => new("slider_grabber.svg.96dpi.png");


    ResPath ITabContainerConfig.TabContainerPanelPath => new("tabcontainer_panel.png");

    ResPath IIconConfig.HelpIconPath => new("help.png");
    ResPath IIconConfig.CrossIconPath => new("cross.svg.png");
    ResPath IIconConfig.RefreshIconPath => new("circular_arrow.svg.96dpi.png");
    ResPath IIconConfig.InvertedTriangleIconPath => new("inverted_triangle.svg.png");

    ResPath IWindowConfig.WindowHeaderTexturePath => new("window_header.png");
    ResPath IWindowConfig.WindowHeaderAlertTexturePath => new("window_header_alert.png");
    ResPath IWindowConfig.WindowBackgroundPath => new("window_background.png");
    ResPath IWindowConfig.WindowBackgroundBorderedPath => new("window_background_bordered.png");
    ResPath IWindowConfig.TransparentWindowBackgroundBorderedPath => new("transparent_window_background_bordered.png");

    ResPath IButtonConfig.BaseButtonPath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.OpenLeftButtonPath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.OpenRightButtonPath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.OpenBothButtonPath => new("button.svg.96dpi.png");
    ResPath IButtonConfig.SmallButtonPath => new("button_small.svg.96dpi.png");
    ResPath IButtonConfig.RoundedButtonPath => new("rounded_button.svg.96dpi.png");
    ResPath IButtonConfig.RoundedButtonBorderedPath => new("rounded_button_bordered.svg.96dpi.png");

    ColorPalette IButtonConfig.ButtonPalette => PrimaryPalette with { PressedElement = PositivePalette.PressedElement };
    ColorPalette IButtonConfig.PositiveButtonPalette => PositivePalette;
    ColorPalette IButtonConfig.NegativeButtonPalette => NegativePalette;
}
