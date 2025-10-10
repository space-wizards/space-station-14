using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Lobby.UI;

[CommonSheetlet]
public sealed class HumanoidProfileEditorSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E<TextureButton>()
                .Identifier("SpeciesInfoDefault")
                .Prop(TextureButton.StylePropertyTexture,
                    ResCache.GetTexture("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
            // copied from `StyleNano`, but this is unused
            // E<TextureButton>()
            //     .Identifier("SpeciesInfoWarning")
            //     .Prop(TextureButton.StylePropertyTexture,
            //         ResCache.GetTexture("/Textures/Interface/info.svg.192dpi.png"))
            //     .Prop(Control.StylePropertyModulateSelf, sheet.HighlightPalette[0]),
        ];
    }
}
