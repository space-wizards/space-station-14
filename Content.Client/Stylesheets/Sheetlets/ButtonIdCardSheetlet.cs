using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ButtonIdCardSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonConfig = sheet;

        var baseBox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(buttonConfig.BaseButtonIdCardPath, NanotrasenStylesheet.TextureRoot),
        };
        baseBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        baseBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

        return
        [
            E<Button>().Class(StyleClass.ButtonIdCard).Box(baseBox),
        ];
    }
}

