using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Stylesheets.Redux.SheetletConfig
{
    public interface IButtonConfig : ISheetletConfig
    {
        public ResPath BaseButtonTexturePath { get; }
        public ResPath OpenLeftButtonTexturePath { get; }
        public ResPath OpenRightButtonTexturePath { get; }
        public ResPath OpenBothButtonTexturePath { get; }
        public ResPath SmallButtonTexturePath { get; }

        /// <summary>
        ///     A lightest-to-darkest five color palette, for use by buttons.
        /// </summary>
        public ColorPalette ButtonPalette { get; }

        /// <summary>
        ///     A lightest-to-darkest five color palette, for use by "positive" buttons.
        /// </summary>
        public ColorPalette PositiveButtonPalette { get; }

        /// <summary>
        ///     A lightest-to-darkest five color palette, for use by "negative" buttons.
        /// </summary>
        public ColorPalette NegativeButtonPalette { get; }

        public StyleBox ConfigureBaseButton(IStyleResources sheet)
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

        public StyleBox ConfigureOpenRightButton(IStyleResources sheet)
        {
            var b = new StyleBoxTexture((StyleBoxTexture) ConfigureBaseButton(sheet))
            {
                Texture = new AtlasTexture(sheet.GetTexture(OpenRightButtonTexturePath),
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
                Texture = new AtlasTexture(sheet.GetTexture(OpenLeftButtonTexturePath),
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
                Texture = new AtlasTexture(sheet.GetTexture(OpenBothButtonTexturePath),
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
                Texture = sheet.GetTexture(SmallButtonTexturePath),
            };
            return b;
        }
    }
}
