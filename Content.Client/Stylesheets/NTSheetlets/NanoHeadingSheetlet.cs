using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.StylesheetFactories;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.NTSheetlets;

/// Not NTHeading because NanoHeading is the name of the element
[Sheetlet(typeof(NanotrasenStylesheetFactory))]
public sealed class NanoHeadingSheetlet<T> : ISheetlet<T>
    where T : INanoHeadingConfig
{
    public StyleRule[] GetRules(StylesheetFactory factory, T config)
    {
        var nanoHeadingTex = factory.GetTexture(config.NanoHeadingPath);
        var nanoHeadingBox = new StyleBoxTexture
        {
            Texture = nanoHeadingTex,
            PatchMarginRight = 10,
            PatchMarginTop = 10,
            ContentMarginTopOverride = 2,
            ContentMarginLeftOverride = 10,
            PaddingTop = 4,
        };
        nanoHeadingBox.SetPatchMargin(StyleBox.Margin.Left | StyleBox.Margin.Bottom, 2);

        return
        [
            E<NanoHeading>().ParentOf(E<PanelContainer>()).Panel(nanoHeadingBox),
        ];
    }
}
