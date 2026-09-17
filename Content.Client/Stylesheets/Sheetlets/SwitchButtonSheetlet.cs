using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SwitchButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, ISwitchButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        ISwitchButtonConfig switchButtonCfg = sheet;

        var trackFillTex = sheet.GetTextureOr(switchButtonCfg.SwitchButtonTrackFillPath, NanotrasenStylesheet.TextureRoot);
        var trackOutlineTex = sheet.GetTextureOr(switchButtonCfg.SwitchButtonTrackOutlinePath, NanotrasenStylesheet.TextureRoot);
        var thumbFillTex = sheet.GetTextureOr(switchButtonCfg.SwitchButtonThumbFillPath, NanotrasenStylesheet.TextureRoot);
        var thumbOutlineTex = sheet.GetTextureOr(switchButtonCfg.SwitchButtonThumbOutlinePath, NanotrasenStylesheet.TextureRoot);
        var symbolOffTex = sheet.GetTextureOr(switchButtonCfg.SwitchButtonSymbolOffPath, NanotrasenStylesheet.TextureRoot);
        var symbolOnTex = sheet.GetTextureOr(switchButtonCfg.SwitchButtonSymbolOnPath, NanotrasenStylesheet.TextureRoot);

        return
        [
            // SwitchButton
            E<SwitchButton>().Prop(SwitchButton.StylePropertySeparation, 10),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Prop(TextureRect.StylePropertyTexture, trackFillTex)
                .Modulate(sheet.SecondaryPalette.BackgroundDark),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackOutline))
                .Prop(TextureRect.StylePropertyTexture, trackOutlineTex)
                .Modulate(sheet.SecondaryPalette.Text),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .Prop(TextureRect.StylePropertyTexture, thumbFillTex)
                .Modulate(sheet.PrimaryPalette.Element)
                .HorizontalAlignment(Control.HAlignment.Left),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .Prop(TextureRect.StylePropertyTexture, thumbOutlineTex)
                .Modulate(sheet.PrimaryPalette.Text)
                .HorizontalAlignment(Control.HAlignment.Left),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Prop(TextureRect.StylePropertyTexture, symbolOffTex)
                .Modulate(sheet.SecondaryPalette.Text),

            // Pressed styles
            E<SwitchButton>()
                .PseudoPressed()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(sheet.PositivePalette.Text),

            E<SwitchButton>()
                .PseudoPressed()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Prop(TextureRect.StylePropertyTexture, symbolOnTex)
                .Modulate(Color.White), // Same color as text, not yet in any of the palettes

            E<SwitchButton>()
                .PseudoPressed()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .HorizontalAlignment(Control.HAlignment.Right),

            E<SwitchButton>()
                .PseudoPressed()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .HorizontalAlignment(Control.HAlignment.Right),

            // Disabled styles
            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(sheet.SecondaryPalette.DisabledElement),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackOutline))
                .Modulate(sheet.SecondaryPalette.DisabledElement),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .Modulate(sheet.PrimaryPalette.DisabledElement),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .Modulate(sheet.PrimaryPalette.TextDark),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Modulate(sheet.SecondaryPalette.TextDark),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<Label>())
                .Modulate(sheet.PrimaryPalette.TextDark),

            // Both pressed & disabled styles
            // Note that some of the pressed-only and disabled-only styles do not conflict
            // and will also be used
            E<SwitchButton>()
                .PseudoPressed()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(sheet.PositivePalette.DisabledElement),

            E<SwitchButton>()
                .PseudoPressed()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Modulate(sheet.PositivePalette.Text),
        ];
    }
}
