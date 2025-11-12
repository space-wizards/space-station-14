using Content.Client.Resources;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SwitchButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, ICheckboxConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        // TODO: make config
        // ICheckboxConfig checkboxCfg = sheet;

        // SwitchButton
        var trackFillTex = ResCache.GetTexture("/Textures/Interface/Nano/switchbutton_track_fill.svg.96dpi.png");
        var trackOutlineTex = ResCache.GetTexture("/Textures/Interface/Nano/switchbutton_track_outline.svg.96dpi.png");
        var thumbFillTex = ResCache.GetTexture("/Textures/Interface/Nano/switchbutton_thumb_fill.svg.96dpi.png");
        var thumbOutlineTex = ResCache.GetTexture("/Textures/Interface/Nano/switchbutton_thumb_outline.svg.96dpi.png");
        var symbolOffTex = ResCache.GetTexture("/Textures/Interface/Nano/switchbutton_symbol_off.svg.96dpi.png");
        var symbolOnTex = ResCache.GetTexture("/Textures/Interface/Nano/switchbutton_symbol_on.svg.96dpi.png");

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
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(sheet.PositivePalette.Text),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Prop(TextureRect.StylePropertyTexture, symbolOnTex)
                .Modulate(Color.White), // Same color as text, not yet in any of the palettes

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .HorizontalAlignment(Control.HAlignment.Right),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .HorizontalAlignment(Control.HAlignment.Right),

            // Disabled styles
            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(sheet.SecondaryPalette.DisabledElement),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackOutline))
                .Modulate(sheet.SecondaryPalette.DisabledElement),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbFill))
                .Modulate(sheet.PrimaryPalette.DisabledElement),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassThumbOutline))
                .Modulate(sheet.PrimaryPalette.TextDark),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Modulate(sheet.SecondaryPalette.TextDark),

            // Both pressed & disabled styles
            // Note that some of the pressed-only and disabled-only styles do not conflict
            // and will also be used
            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassTrackFill))
                .Modulate(sheet.PositivePalette.DisabledElement),

            E<SwitchButton>()
                .Pseudo(SwitchButton.StylePseudoClassPressed)
                .Pseudo(SwitchButton.StylePseudoClassDisabled)
                .ParentOf(E<TextureRect>().Class(SwitchButton.StyleClassSymbol))
                .Modulate(sheet.PositivePalette.Text),
        ];
    }
}
