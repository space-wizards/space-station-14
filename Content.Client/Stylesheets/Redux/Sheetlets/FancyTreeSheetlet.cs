using Content.Client.UserInterface.Controls.FancyTree;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class FancyTreeSheetlet: Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return new StyleRule[]
        {
            E<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                .Class(TreeItem.StyleClassEvenRow)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat(sheet.SecondaryPalette[2])),

            E<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                .Class(TreeItem.StyleClassOddRow)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat(sheet.SecondaryPalette[3])),

            E<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                .Class(TreeItem.StyleClassSelected)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat(sheet.PrimaryPalette[1])),

            E<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                .Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat(sheet.PrimaryPalette[0])),
        };
    }
}
