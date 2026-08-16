using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetDefinitions;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[Sheetlet(typeof(CommonStylesheetDefinition))]
public sealed class SwitchButtonSheetlet<T> : ISheetlet<T> where T : ISwitchButtonConfig, IPaletteConfig
{
    public StyleRule[] GetRules(StylesheetDefinition sheet, T config)
    {
        var trackFillTex = sheet.GetTexture(config.SwitchButtonTrackFillPath);
        var trackOutlineTex = sheet.GetTexture(config.SwitchButtonTrackOutlinePath);
        var thumbFillTex = sheet.GetTexture(config.SwitchButtonThumbFillPath);
        var thumbOutlineTex = sheet.GetTexture(config.SwitchButtonThumbOutlinePath);
        var symbolOffTex = sheet.GetTexture(config.SwitchButtonSymbolOffPath);
        var symbolOnTex = sheet.GetTexture(config.SwitchButtonSymbolOnPath);

        return
        [
            // SwitchButton
            E<SwitchButton>().Prop(SwitchButton.StylePropertySeparation, 10),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Prop(TextureRect.StylePropertyTexture, trackFillTex)
                .Modulate(config.SecondaryPalette.BackgroundDark),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackOutline))
                .Prop(TextureRect.StylePropertyTexture, trackOutlineTex)
                .Modulate(config.SecondaryPalette.Text),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .Prop(TextureRect.StylePropertyTexture, thumbFillTex)
                .Modulate(config.PrimaryPalette.Element)
                .HorizontalAlignment(Control.HAlignment.Left),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .Prop(TextureRect.StylePropertyTexture, thumbOutlineTex)
                .Modulate(config.PrimaryPalette.Text)
                .HorizontalAlignment(Control.HAlignment.Left),

            E<SwitchButton>()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Prop(TextureRect.StylePropertyTexture, symbolOffTex)
                .Modulate(config.SecondaryPalette.Text),

            // Pressed styles
            E<SwitchButton>()
                .PseudoPressed()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(config.PositivePalette.Text),

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
                .Modulate(config.SecondaryPalette.DisabledElement),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackOutline))
                .Modulate(config.SecondaryPalette.DisabledElement),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .Modulate(config.PrimaryPalette.DisabledElement),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .Modulate(config.PrimaryPalette.TextDark),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Modulate(config.SecondaryPalette.TextDark),

            E<SwitchButton>()
                .PseudoDisabled()
                .ParentOf(E<Label>())
                .Modulate(config.PrimaryPalette.TextDark),

            // Both pressed & disabled styles
            // Note that some of the pressed-only and disabled-only styles do not conflict
            // and will also be used
            E<SwitchButton>()
                .PseudoPressed()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(config.PositivePalette.DisabledElement),

            E<SwitchButton>()
                .PseudoPressed()
                .PseudoDisabled()
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Modulate(config.PositivePalette.Text),
        ];
    }
}
