using System.Numerics;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class MonotoneButtonSheetlet<T> : Sheetlet<T> where T : IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        // Monotone (unfilled)
        var monotoneButton = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(sheet.MonotoneBaseButtonPath, NanotrasenStylesheet.TextureRoot)
        };
        monotoneButton.SetPatchMargin(StyleBox.Margin.All, 11);
        monotoneButton.SetPadding(StyleBox.Margin.All, 1);
        monotoneButton.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        monotoneButton.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);

        var monotoneButtonOpenLeft = new StyleBoxTexture(monotoneButton)
        {
            Texture = sheet.GetTextureOr(sheet.MonotoneOpenLeftButtonPath, NanotrasenStylesheet.TextureRoot)
        };

        var monotoneButtonOpenRight = new StyleBoxTexture(monotoneButton)
        {
            Texture = sheet.GetTextureOr(sheet.MonotoneOpenRightButtonPath, NanotrasenStylesheet.TextureRoot)
        };

        var monotoneButtonOpenBoth = new StyleBoxTexture(monotoneButton)
        {
            Texture = sheet.GetTextureOr(sheet.MonotoneOpenBothButtonPath, NanotrasenStylesheet.TextureRoot)
        };

        // Monotone (filled)
        var buttonTex = sheet.GetTextureOr(sheet.OpenLeftButtonPath, NanotrasenStylesheet.TextureRoot);
        var monotoneFilledButton = new StyleBoxTexture(monotoneButton)
        {
            Texture = buttonTex
        };

        var monotoneFilledButtonOpenLeft = new StyleBoxTexture(monotoneButton)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
        };
        monotoneFilledButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);

        var monotoneFilledButtonOpenRight = new StyleBoxTexture(monotoneButton)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        monotoneFilledButtonOpenRight.SetPatchMargin(StyleBox.Margin.Right, 0);

        var monotoneFilledButtonOpenBoth = new StyleBoxTexture(monotoneButton)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
        };
        monotoneFilledButtonOpenBoth.SetPatchMargin(StyleBox.Margin.Horizontal, 0);


        return
        [
            // Unfilled
            E<MonotoneButton>()
                .Box(monotoneButton),
            E<MonotoneButton>()
                .Class(StyleClass.ButtonOpenLeft)
                .Box(monotoneButtonOpenLeft),
            E<MonotoneButton>()
                .Class(StyleClass.ButtonOpenRight)
                .Box(monotoneButtonOpenRight),
            E<MonotoneButton>()
                .Class(StyleClass.ButtonOpenBoth)
                .Box(monotoneButtonOpenBoth),

            // Filled
            E<MonotoneButton>()
                .PseudoPressed()
                .Box(monotoneFilledButton)
                .Prop(Button.StylePropertyModulateSelf, Color.White),
            E<MonotoneButton>()
                .Class(StyleClass.ButtonOpenLeft)
                .PseudoPressed()
                .Box(monotoneFilledButtonOpenLeft)
                .Prop(Button.StylePropertyModulateSelf, Color.White),
            E<MonotoneButton>()
                .Class(StyleClass.ButtonOpenRight)
                .PseudoPressed()
                .Box(monotoneFilledButtonOpenRight)
                .Prop(Button.StylePropertyModulateSelf, Color.White),
            E<MonotoneButton>()
                .Class(StyleClass.ButtonOpenBoth)
                .PseudoPressed()
                .Box(monotoneFilledButtonOpenBoth)
                .Prop(Button.StylePropertyModulateSelf, Color.White),
        ];
    }
}
