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

            // TODO.eoin:
            //      Template this class to depend on interfaces, rather than Nanotrasen

            /// Large red texture button
            E<TextureButton>().Identifier("TemptingRedButton")
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTextureOr(buttonCfg.RoundedButtonPath, NanotrasenStylesheet.TextureRoot))
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
