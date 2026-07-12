using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class SliderSheetlet<T> : ISheetlet<T>
    where T : ISliderConfig, IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var sliderFillTex = factory.GetTexture(config.SliderFillPath);

        var sliderFillBox = new StyleBoxTexture
        {
            Texture = sliderFillTex,
            Modulate = config.PositivePalette.TextDark,
        };

        var sliderBackBox = new StyleBoxTexture
        {
            Texture = sliderFillTex,
            Modulate = config.SecondaryPalette.BackgroundDark,
        };

        var sliderForeBox = new StyleBoxTexture
        {
            Texture = factory.GetTexture(config.SliderOutlinePath),
            Modulate = Color.FromHex("#494949") // TODO: Unhardcode.
        };

        var sliderGrabBox = new StyleBoxTexture
        {
            Texture = factory.GetTexture(config.SliderGrabber),
        };

        sliderFillBox.SetPatchMargin(StyleBox.Margin.All, 12);
        sliderBackBox.SetPatchMargin(StyleBox.Margin.All, 12);
        sliderForeBox.SetPatchMargin(StyleBox.Margin.All, 12);
        sliderGrabBox.SetPatchMargin(StyleBox.Margin.All, 12);

        // var sliderFillGreen = new StyleBoxTexture(sliderFillBox) { Modulate = Color.LimeGreen };
        // var sliderFillRed = new StyleBoxTexture(sliderFillBox) { Modulate = Color.Red };
        // var sliderFillBlue = new StyleBoxTexture(sliderFillBox) { Modulate = Color.Blue };
        // var sliderFillWhite = new StyleBoxTexture(sliderFillBox) { Modulate = Color.White };

        return
        [
            E<Slider>()
                .Prop(Slider.StylePropertyBackground, sliderBackBox)
                .Prop(Slider.StylePropertyForeground, sliderForeBox)
                .Prop(Slider.StylePropertyGrabber, sliderGrabBox)
                .Prop(Slider.StylePropertyFill, sliderFillBox),
            // these styles seem to be unused now
            // E<ColorableSlider>()
            //     .Prop(ColorableSlider.StylePropertyFillWhite, sliderFillWhite)
            //     .Prop(ColorableSlider.StylePropertyBackgroundWhite, sliderFillWhite),
            //
            // E<Slider>().Class(StyleClass.StyleClassSliderRed)
            //     .Prop(Slider.StylePropertyFill, sliderFillRed),
            // E<Slider>().Class(StyleClass.StyleClassSliderBlue)
            //     .Prop(Slider.StylePropertyFill, sliderFillBlue),
            // E<Slider>().Class(StyleClass.StyleClassSliderGreen)
            //     .Prop(Slider.StylePropertyFill, sliderFillGreen),
            // E<Slider>().Class(StyleClass.StyleClassSliderWhite)
            //     .Prop(Slider.StylePropertyFill, sliderFillWhite),
        ];
    }
}
