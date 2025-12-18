using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.UserInterface.Controls;

[CommonSheetlet]
public sealed class CommunicationsConsoleSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        var lcdFontLarge = ResCache.GetFont("/Fonts/7SegmentDisplayDigits.ttf", 20);

        return [
            E<Label>().Class("LabelLCDBig")
                .Prop("font-color", sheet.NegativePalette.Text)
                .Prop("font", lcdFontLarge),

            /// Large red texture button
            E<TextureButton>().Identifier("TemptingRedButton")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTextureOr(sheet.RoundedButtonPath, NanotrasenStylesheet.TextureRoot))
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.Element),

            E<TextureButton>().Identifier("TemptingRedButton")
                .Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.Element),
            E<TextureButton>().Identifier("TemptingRedButton")
                .Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/Interface/Nano/rounded_locked_button.svg.96dpi.png"))
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.DisabledElement),
            E<TextureButton>().Identifier("TemptingRedButton").Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.HoveredElement),

            E<TextureRect>().Identifier("ScrewHead")
                .Prop(TextureRect.StylePropertyTexture, ResCache.GetTexture("/Textures/Interface/Diegetic/screw.svg.96dpi.png")),
        ];
    }
}
