using Content.Client.Stylesheets.Redux.Sheetlets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.NTSheetlets;

public sealed class NTFancyWindowSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;
        var leftPanel = cfg.ConfigureOpenLeftButton(sheet);
        leftPanel.SetPadding(StyleBox.Margin.All, 0.0f);
        return
        [
            E<PanelContainer>()
                .Class("WindowHeadingBackground")
                .Prop("panel", leftPanel)
                .Prop(Control.StylePropertyModulateSelf, sheet.SecondaryPalette[4]),
            E<PanelContainer>()
                .Class("WindowHeadingBackgroundLight")
                .Prop("panel", leftPanel)
                .Prop(Control.StylePropertyModulateSelf, sheet.SecondaryPalette[3]),
            E()
                .Class(StyleClass.WindowContentsContainer)
                .Margin(new Thickness(0, 2)),
        ];
    }
}
