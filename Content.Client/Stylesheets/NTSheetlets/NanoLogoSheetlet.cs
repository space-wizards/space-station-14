using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.NTSheetlets;

[Sheetlet(typeof(NanotrasenStylesheetFactory))]
public sealed class NanoLogoSheetlet<T> : ISheetlet<T>
    where T : ISheetletConfig
{
    public StyleRule[] GetRules(StylesheetFactory resolver, T config)
    {
        return
        [
            E<TextureRect>()
                .Class("NTLogoDark")
                .Prop(TextureRect.StylePropertyTexture,
                    resolver.GetResource<TextureResource>(new ResPath("ntlogo.svg.png")))
                .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#757575")),
        ];
    }
}
