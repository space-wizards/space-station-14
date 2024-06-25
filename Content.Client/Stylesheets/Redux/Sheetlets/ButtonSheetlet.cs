using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.Redux.StylesheetHelpers;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets.Redux.Sheetlets;

public abstract class ButtonSheetlet : Sheetlet<PalettedStylesheet>
{
    // this is hardcoded, but the other option is adding another field to the palette class. doesn't seem worth it
    private readonly Color[] _textureButtonPalette = new[]
    {
        Color.FromHex("#ffffff"), Color.FromHex("#d0d0d0"), Color.FromHex("#b0b0b0"), Color.FromHex("#909090"),
        Color.FromHex("#707070"), Color.FromHex("#505050"),
    };

    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var cfg = (IButtonConfig) sheet;
        var rules =  new List<StyleRule>
        {
            // Set textures for the kinds of buttons
            CButton()
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureBaseButton(sheet)),
            CButton().Class(StyleClass.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenLeftButton(sheet)),
            CButton().Class(StyleClass.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenRightButton(sheet)),
            CButton().Class(StyleClass.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenBothButton(sheet)),
            CButton().Class(StyleClass.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureOpenSquareButton(sheet)),
            CButton().Class(StyleClass.ButtonSmall)
                .Prop(ContainerButton.StylePropertyStyleBox, cfg.ConfigureSmallButton(sheet)),
            CButton().Class(StyleClass.ButtonSmall)
                .ParentOf(Element<Label>())
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(8)),

            // Ensure labels in buttons are aligned.
            E<Label>().Class(Button.StyleClassButton)
                .Prop(Label.StylePropertyAlignMode, Label.AlignMode.Center),
        };
        // Texture button modulation
        MakeButtonRules<TextureButton>(cfg, rules, _textureButtonPalette, null);

        return rules.ToArray();
    }

    public static void MakeButtonRules<T>(IButtonConfig _, List<StyleRule> rules, IReadOnlyList<Color> palette, string? styleclass)
        where T: Control
    {
        rules.AddRange(new StyleRule[]
        {
            E<T>().MaybeClass(styleclass).ButtonNormal().Modulate(palette[1]),
            E<T>().MaybeClass(styleclass).ButtonHovered().Modulate(palette[0]),
            E<T>().MaybeClass(styleclass).ButtonPressed().Modulate(palette[2]),
            E<T>().MaybeClass(styleclass).ButtonDisabled().Modulate(palette[4])
        });
    }

    public static void MakeButtonRules(IButtonConfig _, List<StyleRule> rules, IReadOnlyList<Color> palette, string? styleclass)
    {
        rules.AddRange(new StyleRule[]
        {
            Element().MaybeClass(styleclass).ButtonNormal().Prop(Button.StylePropertyModulateSelf, palette[1]),
            Element().MaybeClass(styleclass).ButtonHovered().Prop(Button.StylePropertyModulateSelf, palette[0]),
            Element().MaybeClass(styleclass).ButtonPressed().Prop(Button.StylePropertyModulateSelf, palette[2]),
            Element().MaybeClass(styleclass).ButtonDisabled().Prop(Button.StylePropertyModulateSelf, palette[4])
        });
    }

    internal static MutableSelectorElement CButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton);
    }
}

public interface IButtonConfig : ISheetletConfig
{
    public ResPath BaseButtonTexturePath { get; }
    public ResPath OpenLeftButtonTexturePath { get; }
    public ResPath OpenRightButtonTexturePath { get; }
    public ResPath OpenBothButtonTexturePath { get; }
    public ResPath SmallButtonTexturePath { get;  }

    /// <summary>
    ///     A lightest-to-darkest five color palette, for use by buttons.
    /// </summary>
    public Color[] ButtonPalette { get; }

    /// <summary>
    ///     A lightest-to-darkest five color palette, for use by "positive" buttons.
    /// </summary>
    public Color[] PositiveButtonPalette { get; }

    /// <summary>
    ///     A lightest-to-darkest five color palette, for use by "negative" buttons.
    /// </summary>
    public Color[] NegativeButtonPalette { get; }

    public virtual StyleBox ConfigureBaseButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture
        {
            Texture = sheet.GetTexture(BaseButtonTexturePath),
        };
        // TODO: Figure out a nicer way to store/represent this. This is icky.
        b.SetPatchMargin(StyleBox.Margin.All, 10);
        b.SetPadding(StyleBox.Margin.All, 1);
        b.SetContentMarginOverride(StyleBox.Margin.Vertical, 3);
        b.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);
        return b;
    }

    public virtual StyleBox ConfigureOpenRightButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture((StyleBoxTexture)ConfigureBaseButton(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTexture(OpenRightButtonTexturePath), UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        b.SetPatchMargin(StyleBox.Margin.Right, 0);
        b.SetContentMarginOverride(StyleBox.Margin.Right, 8);
        b.SetPadding(StyleBox.Margin.Right, 2);
        return b;
    }

    public virtual StyleBox ConfigureOpenLeftButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture((StyleBoxTexture)ConfigureBaseButton(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTexture(OpenLeftButtonTexturePath), UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
        };
        b.SetPatchMargin(StyleBox.Margin.Left, 0);
        b.SetContentMarginOverride(StyleBox.Margin.Left, 8);
        b.SetPadding(StyleBox.Margin.Left, 1);
        return b;
    }

    public virtual StyleBox ConfigureOpenBothButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture((StyleBoxTexture)ConfigureBaseButton(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTexture(OpenBothButtonTexturePath), UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
        };
        b.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
        b.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        b.SetPadding(StyleBox.Margin.Right, 2);
        b.SetPadding(StyleBox.Margin.Left, 1);
        return b;
    }

    public virtual StyleBox ConfigureOpenSquareButton(IStyleResources sheet)
    {
        return ConfigureOpenBothButton(sheet);
    }

    public virtual StyleBox ConfigureSmallButton(IStyleResources sheet)
    {
        var b = new StyleBoxTexture
        {
            Texture =  sheet.GetTexture(SmallButtonTexturePath),
        };
        return b;
    }
}

