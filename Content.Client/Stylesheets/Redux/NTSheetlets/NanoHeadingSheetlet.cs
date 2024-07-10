using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

/// Not NTHeading because NanoHeading is the name of the element
public sealed class NanoHeadingSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var nanoHeadingTex = sheet.GetTexture("nanoheading.svg.96dpi.png");
        var nanoHeadingBox = new StyleBoxTexture
        {
            Texture = nanoHeadingTex,
            PatchMarginRight = 10,
            PatchMarginTop = 10,
            ContentMarginTopOverride = 2,
            ContentMarginLeftOverride = 10,
            PaddingTop = 4
        };
        nanoHeadingBox.SetPatchMargin(StyleBox.Margin.Left | StyleBox.Margin.Bottom, 2);

        return
        [
            E<NanoHeading>().ParentOf(E<PanelContainer>()).Panel(nanoHeadingBox),
        ];
    }
}
