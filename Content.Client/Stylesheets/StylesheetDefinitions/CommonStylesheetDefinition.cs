using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.StylesheetDefinitions;

/// <summary>
/// Common style definitions used by the content stylesheet definitions.
/// </summary>
public abstract class CommonStylesheetDefinition : StylesheetDefinition, IButtonConfig, IWindowConfig, IIconConfig,
    ITabContainerConfig, ISliderConfig, IRadialMenuConfig, IPlaceholderConfig, ITooltipConfig, IPanelConfig,
    INanoHeadingConfig, ILineEditConfig, IStripebackConfig, ICheckboxConfig, ISwitchButtonConfig, IPaletteConfig,
    IFontConfig
{
    /// <inheritdoc/>
    ResPath ICheckboxConfig.CheckboxUncheckedPath => new("checkbox_unchecked.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ICheckboxConfig.CheckboxCheckedPath => new("checkbox_checked.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IStripebackConfig.StripebackPath => new("stripeback.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath INanoHeadingConfig.NanoHeadingPath => new("nanoheading.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ILineEditConfig.LineEditPath => new("lineedit.png");

    /// <inheritdoc/>
    ResPath IPanelConfig.GeometricPanelBorderPath => new("geometric_panel_border.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IPanelConfig.BlackPanelDarkThinBorderPath => new("black_panel_dark_thin_border.png");

    /// <inheritdoc/>
    ResPath ITooltipConfig.TooltipBoxPath => new("tooltip.png");

    /// <inheritdoc/>
    ResPath ITooltipConfig.WhisperBoxPath => new("whisper.png");

    /// <inheritdoc/>
    ResPath IPlaceholderConfig.PlaceholderPath => new("placeholder.png");

    /// <inheritdoc/>
    ResPath IRadialMenuConfig.ButtonNormalPath => new("Radial/button_normal.png");

    /// <inheritdoc/>
    ResPath IRadialMenuConfig.ButtonHoverPath => new("Radial/button_hover.png");

    /// <inheritdoc/>
    ResPath IRadialMenuConfig.CloseNormalPath => new("Radial/close_normal.png");

    /// <inheritdoc/>
    ResPath IRadialMenuConfig.CloseHoverPath => new("Radial/close_hover.png");

    /// <inheritdoc/>
    ResPath IRadialMenuConfig.BackNormalPath => new("Radial/back_normal.png");

    /// <inheritdoc/>
    ResPath IRadialMenuConfig.BackHoverPath => new("Radial/back_hover.png");

    /// <inheritdoc/>
    ResPath ISliderConfig.SliderFillPath => new("slider_fill.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISliderConfig.SliderOutlinePath => new("slider_outline.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISliderConfig.SliderGrabber => new("slider_grabber.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ITabContainerConfig.TabContainerPanelPath => new("tabcontainer_panel.png");

    /// <inheritdoc/>
    ResPath IIconConfig.HelpIconPath => new("help.png");

    /// <inheritdoc/>
    ResPath IIconConfig.CrossIconPath => new("cross.svg.png");

    /// <inheritdoc/>
    ResPath IIconConfig.RefreshIconPath => new("circular_arrow.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IIconConfig.InvertedTriangleIconPath => new("inverted_triangle.svg.png");


    /// <inheritdoc/>
    ResPath IWindowConfig.WindowHeaderTexturePath => new("window_header.png");

    /// <inheritdoc/>
    ResPath IWindowConfig.WindowHeaderAlertTexturePath => new("window_header_alert.png");

    /// <inheritdoc/>
    ResPath IWindowConfig.WindowBackgroundPath => new("window_background.png");

    /// <inheritdoc/>
    ResPath IWindowConfig.WindowBackgroundBorderedPath => new("window_background_bordered.png");

    /// <inheritdoc/>
    ResPath IWindowConfig.TransparentWindowBackgroundBorderedPath => new("transparent_window_background_bordered.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.BaseButtonPath => new("button.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.OpenLeftButtonPath => new("button.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.OpenRightButtonPath => new("button.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.OpenBothButtonPath => new("button.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.SmallButtonPath => new("button_small.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.RoundedButtonPath => new("rounded_button.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.RoundedButtonBorderedPath => new("rounded_button_bordered.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.MonotoneBaseButtonPath => new("Monotone/monotone_button.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.MonotoneOpenLeftButtonPath => new("Monotone/monotone_button_open_left.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.MonotoneOpenRightButtonPath => new("Monotone/monotone_button_open_right.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath IButtonConfig.MonotoneOpenBothButtonPath => new("Monotone/monotone_button_open_both.svg.96dpi.png");

    /// <inheritdoc/>
    ColorPalette IButtonConfig.ButtonPalette => PrimaryPalette with { PressedElement = PositivePalette.PressedElement };

    /// <inheritdoc/>
    ColorPalette IButtonConfig.PositiveButtonPalette => PositivePalette;

    /// <inheritdoc/>
    ColorPalette IButtonConfig.NegativeButtonPalette => NegativePalette;

    /// <inheritdoc/>
    ResPath ISwitchButtonConfig.SwitchButtonTrackFillPath => new("switchbutton_track_fill.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISwitchButtonConfig.SwitchButtonTrackOutlinePath => new("switchbutton_track_outline.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISwitchButtonConfig.SwitchButtonThumbFillPath => new("switchbutton_thumb_fill.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISwitchButtonConfig.SwitchButtonThumbOutlinePath => new("switchbutton_thumb_outline.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISwitchButtonConfig.SwitchButtonSymbolOffPath => new("switchbutton_symbol_off.svg.96dpi.png");

    /// <inheritdoc/>
    ResPath ISwitchButtonConfig.SwitchButtonSymbolOnPath => new("switchbutton_symbol_on.svg.96dpi.png");

    /// <inheritdoc/>
    public abstract ColorPalette PrimaryPalette { get; }

    /// <inheritdoc/>
    public abstract ColorPalette SecondaryPalette { get; }

    /// <inheritdoc/>
    public abstract ColorPalette PositivePalette { get; }

    /// <inheritdoc/>
    public abstract ColorPalette NegativePalette { get; }

    /// <inheritdoc/>
    public abstract ColorPalette HighlightPalette { get; }

    // Using the newer [] collection syntax was causing sandbox errors for some reason.
    /// <inheritdoc/>
    List<(string?, int)> IFontConfig.CommonFontSizes => new()
    {
        (null, 12),
        (StyleClass.FontSmall, 10),
        (StyleClass.FontLarge, 14),
    };

    /// <inheritdoc/>
    public FontFamilyStack BaseFont => FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSans/NotoSans-Regular.ttf"))
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSans/NotoSans-Bold.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSans/NotoSans-Italic.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSans/NotoSans-BoldItalic.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoEmoji.ttf"))
        .Build();

    /// <inheritdoc/>
    public FontFamilyStack DisplayFont => FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-Regular.ttf"))
        .AddKind(FontKind.Regular, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-Italic.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Regular.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSansDisplay/NotoSansDisplay-BoldItalic.ttf"))
        .AddKind(FontKind.BoldItalic, new ResPath("/Fonts/NotoSans/NotoSansSymbols-Bold.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"))
        .AddExtra(new ResPath("/Fonts/NotoEmoji.ttf"))
        .Build();

    /// <inheritdoc/>
    public FontFamilyStack DecorativeFont => FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/Boxfont-round/Boxfont Round.ttf"))
        .Build();

    /// <inheritdoc/>
    public FontFamilyStack MonoFont => FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/RobotoMono/RobotoMono-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/RobotoMono/RobotoMono-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/RobotoMono/RobotoMono-Italic.ttf"))
        .Build();
}
