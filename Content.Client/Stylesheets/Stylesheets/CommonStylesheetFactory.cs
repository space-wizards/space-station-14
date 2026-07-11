using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Stylesheets;

public abstract class CommonStylesheetFactory : StylesheetFactory, IButtonConfig, IWindowConfig, IIconConfig,
    ITabContainerConfig,
    ISliderConfig, IRadialMenuConfig, IPlaceholderConfig, ITooltipConfig, IPanelConfig, INanoHeadingConfig,
    ILineEditConfig, IStripebackConfig, ICheckboxConfig, ISwitchButtonConfig, IPaletteConfig, IFontConfig
{
    /// <remarks>
    ///     This constructor will not access any virtual or abstract properties, so you can set them from your config.
    /// </remarks>
    protected CommonStylesheetFactory()
    {
    }

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

    ResPath IButtonConfig.MonotoneBaseButtonPath => new("Monotone/monotone_button.svg.96dpi.png");
    ResPath IButtonConfig.MonotoneOpenLeftButtonPath => new("Monotone/monotone_button_open_left.svg.96dpi.png");
    ResPath IButtonConfig.MonotoneOpenRightButtonPath => new("Monotone/monotone_button_open_right.svg.96dpi.png");
    ResPath IButtonConfig.MonotoneOpenBothButtonPath => new("Monotone/monotone_button_open_both.svg.96dpi.png");

    ColorPalette IButtonConfig.ButtonPalette => PrimaryPalette with { PressedElement = PositivePalette.PressedElement };
    ColorPalette IButtonConfig.PositiveButtonPalette => PositivePalette;
    ColorPalette IButtonConfig.NegativeButtonPalette => NegativePalette;

    ResPath ISwitchButtonConfig.SwitchButtonTrackFillPath => new("switchbutton_track_fill.svg.96dpi.png");
    ResPath ISwitchButtonConfig.SwitchButtonTrackOutlinePath => new("switchbutton_track_outline.svg.96dpi.png");
    ResPath ISwitchButtonConfig.SwitchButtonThumbFillPath => new("switchbutton_thumb_fill.svg.96dpi.png");
    ResPath ISwitchButtonConfig.SwitchButtonThumbOutlinePath => new("switchbutton_thumb_outline.svg.96dpi.png");
    ResPath ISwitchButtonConfig.SwitchButtonSymbolOffPath => new("switchbutton_symbol_off.svg.96dpi.png");
    ResPath ISwitchButtonConfig.SwitchButtonSymbolOnPath => new("switchbutton_symbol_on.svg.96dpi.png");

    public abstract ColorPalette PrimaryPalette { get; }
    public abstract ColorPalette SecondaryPalette { get; }
    public abstract ColorPalette PositivePalette { get; }
    public abstract ColorPalette NegativePalette { get; }
    public abstract ColorPalette HighlightPalette { get; }

    List<(string?, int)> IFontConfig.CommonFontSizes => new()
    {
        (null, 12),
        (StyleClass.FontSmall, 10),
        (StyleClass.FontLarge, 14),
    };

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

    public FontFamilyStack DecorativeFont => FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/Boxfont-round/Boxfont Round.ttf"))
        .Build();

    public FontFamilyStack MonoFont => FontFamilyStack.New()
        .AddKind(FontKind.Regular, new ResPath("/Fonts/RobotoMono/RobotoMono-Regular.ttf"))
        .AddKind(FontKind.Bold, new ResPath("/Fonts/RobotoMono/RobotoMono-Bold.ttf"))
        .AddKind(FontKind.Italic, new ResPath("/Fonts/RobotoMono/RobotoMono-Italic.ttf"))
        .Build();
}
