using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.StylesheetFactories;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Lobby.UI;

[Sheetlet(typeof(CommonStylesheetFactory))]
public sealed class HumanoidProfileEditorSheetlet<T> : ISheetlet<T>
    where T : ISheetletConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        return
        [
            E<TextureButton>()
                .Identifier("SpeciesInfoDefault")
                .Prop(TextureButton.StylePropertyTexture,
                    factory.GetTexture(new ResPath("VerbIcons/information.svg.192dpi.png"))),
            // copied from `StyleNano`, but this is unused
            // E<TextureButton>()
            //     .Identifier("SpeciesInfoWarning")
            //     .Prop(TextureButton.StylePropertyTexture,
            //         ResCache.GetTexture("/Textures/Interface/info.svg.192dpi.png"))
            //     .Prop(Control.StylePropertyModulateSelf, sheet.HighlightPalette[0]),
        ];
    }
}
