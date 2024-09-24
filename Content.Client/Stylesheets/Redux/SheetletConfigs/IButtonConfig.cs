using System.Numerics;
using Content.Client.Stylesheets.Redux.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfigs;

public interface IButtonConfig : ISheetletConfig
{
    public ResPath BaseButtonPath { get; }
    public ResPath OpenLeftButtonPath { get; }
    public ResPath OpenRightButtonPath { get; }
    public ResPath OpenBothButtonPath { get; }
    public ResPath SmallButtonPath { get; }
    public ResPath RoundedButtonPath { get; }
    public ResPath RoundedButtonBorderedPath { get; }

    public ColorPalette ButtonPalette { get; }
    public ColorPalette PositiveButtonPalette { get; }
    public ColorPalette NegativeButtonPalette { get; }

    public StyleBox ConfigureBaseButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(BaseButtonPath, NanotrasenStylesheet.TextureRoot),
        };
        // TODO: Figure out a nicer way to store/represent this. This is icky.
        b.SetPatchMargin(StyleBox.Margin.All, 10);
        b.SetPadding(StyleBox.Margin.All, 1);
        b.SetContentMarginOverride(StyleBox.Margin.Vertical, 3);
        b.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);
        return b;
    }

    public StyleBox ConfigureOpenRightButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture((StyleBoxTexture) ConfigureBaseButton(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(OpenRightButtonPath, NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        b.SetPatchMargin(StyleBox.Margin.Right, 0);
        b.SetContentMarginOverride(StyleBox.Margin.Right, 8);
        b.SetPadding(StyleBox.Margin.Right, 2);
        return b;
    }

    public StyleBox ConfigureOpenLeftButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture((StyleBoxTexture) ConfigureBaseButton(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(OpenLeftButtonPath, NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
        };
        b.SetPatchMargin(StyleBox.Margin.Left, 0);
        b.SetContentMarginOverride(StyleBox.Margin.Left, 8);
        b.SetPadding(StyleBox.Margin.Left, 1);
        return b;
    }

    public StyleBox ConfigureOpenBothButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture((StyleBoxTexture) ConfigureBaseButton(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(OpenBothButtonPath, NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
        };
        b.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
        b.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        b.SetPadding(StyleBox.Margin.Right, 2);
        b.SetPadding(StyleBox.Margin.Left, 1);
        return b;
    }

    public StyleBox ConfigureOpenSquareButton(IStyleResources sheet)
    {
        return ConfigureOpenBothButton(sheet);
    }

    public StyleBox ConfigureSmallButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(SmallButtonPath, NanotrasenStylesheet.TextureRoot),
        };
        return b;
    }
}
