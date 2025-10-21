using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.UserInterface.Controls;

[CommonSheetlet]
public sealed class CommunicationsConsoleSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        IIconConfig iconCfg = sheet;
        IButtonConfig buttonCfg = sheet;

        var lcdFontLarge = ResCache.GetFont("/Fonts/7SegmentDisplayDigits.ttf", 20);

        return [
            E<Label>().Class("LabelLCDBig")
                .Prop("font-color", sheet.NegativePalette.Text)
                .Prop("font", lcdFontLarge),

            E<TextureButton>().Class("CrossButtonDark")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTextureOr(iconCfg.CrossIconPath, NanotrasenStylesheet.TextureRoot))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#2b2b2b")),

            E<TextureButton>().Class("CrossButtonDark").Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#7F3636")),

            E<TextureButton>().Class("CrossButtonDark").Pseudo(TextureButton.StylePseudoClassPressed)
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#753131")),

            /// Large red texture button
            E<TextureButton>().Class("TemptingRedButton")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTextureOr(buttonCfg.RoundedButtonPath, NanotrasenStylesheet.TextureRoot))
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.Element),

            E<TextureButton>().Class("TemptingRedButton")
                .Pseudo(ContainerButton.StylePseudoClassNormal)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.Element),
            E<TextureButton>().Class("TemptingRedButton")
                .Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(TextureButton.StylePropertyTexture, ResCache.GetTexture("/Textures/Interface/Nano/rounded_locked_button.svg.96dpi.png"))
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.DisabledElement),
            E<TextureButton>().Class("TemptingRedButton").Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(Control.StylePropertyModulateSelf, sheet.NegativePalette.HoveredElement),

            E<TextureRect>().Class("ScrewHead")
                .Prop(TextureRect.StylePropertyTexture, ResCache.GetTexture("/Textures/Interface/Diegetic/screw.svg.96dpi.png")),
        ];
    }
}
