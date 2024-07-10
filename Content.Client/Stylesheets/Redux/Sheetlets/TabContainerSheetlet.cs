using Content.Client.Stylesheets.Redux.SheetletConfigs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

[CommonSheetlet]
public sealed class TabContainerSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var tabCfg = (ITabContainerConfig) sheet;

        var tabContainerPanel = sheet.GetTexture(tabCfg.TabContainerPanelPath).IntoPatch(StyleBox.Margin.All, 2);

        var tabContainerBoxActive = new StyleBoxFlat(sheet.SecondaryPalette.Element);
        tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
        var tabContainerBoxInactive = new StyleBoxFlat(sheet.SecondaryPalette.Background);
        tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

        return new StyleRule[]
        {
            E<TabContainer>()
                .Prop(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel)
                .Prop(TabContainer.StylePropertyTabStyleBox, tabContainerBoxActive)
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, tabContainerBoxInactive)
        };
    }
}
