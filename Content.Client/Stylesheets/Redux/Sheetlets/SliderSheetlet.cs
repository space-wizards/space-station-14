using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class SliderSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var sliderFillTex = sheet.GetTexture("slider_fill.svg.96dpi.png");

        var sliderFillBox = new StyleBoxTexture
        {
            Texture = sliderFillTex,
            Modulate = sheet.PositivePalette[1],
        };

        var sliderBackBox = new StyleBoxTexture
        {
            Texture = sliderFillTex,
            Modulate = sheet.SecondaryPalette[3],
        };

        var sliderForeBox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("slider_outline.svg.96dpi.png"),
            Modulate = Color.FromHex("#494949") // TODO: Unhardcode.
        };

        var sliderGrabBox = new StyleBoxTexture
        {
            Texture = sheet.GetTexture("slider_grabber.svg.96dpi.png"),
        };

        sliderFillBox.SetPatchMargin(StyleBox.Margin.All, 12);
        sliderBackBox.SetPatchMargin(StyleBox.Margin.All, 12);
        sliderForeBox.SetPatchMargin(StyleBox.Margin.All, 12);
        sliderGrabBox.SetPatchMargin(StyleBox.Margin.All, 12);

        var sliderFillGreen = new StyleBoxTexture(sliderFillBox) { Modulate = Color.LimeGreen };
        var sliderFillRed = new StyleBoxTexture(sliderFillBox) { Modulate = Color.Red };
        var sliderFillBlue = new StyleBoxTexture(sliderFillBox) { Modulate = Color.Blue };
        var sliderFillWhite = new StyleBoxTexture(sliderFillBox) { Modulate = Color.White };

        return new StyleRule[]
        {
            E<Slider>()
                .Prop(Slider.StylePropertyBackground, sliderBackBox)
                .Prop(Slider.StylePropertyForeground, sliderForeBox)
                .Prop(Slider.StylePropertyGrabber, sliderGrabBox)
                .Prop(Slider.StylePropertyFill, sliderFillBox),
            E<ColorableSlider>()
                .Prop(ColorableSlider.StylePropertyFillWhite, sliderFillWhite)
                .Prop(ColorableSlider.StylePropertyBackgroundWhite, sliderFillWhite),

            E<Slider>().Class(StyleClass.StyleClassSliderRed)
                .Prop(Slider.StylePropertyFill, sliderFillRed),
            E<Slider>().Class(StyleClass.StyleClassSliderBlue)
                .Prop(Slider.StylePropertyFill, sliderFillBlue),
            E<Slider>().Class(StyleClass.StyleClassSliderGreen)
                .Prop(Slider.StylePropertyFill, sliderFillGreen),
            E<Slider>().Class(StyleClass.StyleClassSliderWhite)
                .Prop(Slider.StylePropertyFill, sliderFillWhite),
        };
    }
}
