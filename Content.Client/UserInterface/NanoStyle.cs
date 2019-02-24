using Content.Client.Utility;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace Content.Client.UserInterface
{
    public sealed class NanoStyle
    {
        private static readonly Color NanoGold = Color.FromHex("#A88B5E");

        public Stylesheet Stylesheet { get; }

        public NanoStyle()
        {
            var resCache = IoCManager.Resolve<IResourceCache>();
            var notoSans12 = resCache.GetFont("/Nano/NotoSans/NotoSans-Regular.ttf", 12);
            var notoSansBold16 = resCache.GetFont("/Nano/NotoSans/NotoSans-Bold.ttf", 16);
            var animalSilence40 = resCache.GetFont("/Fonts/Animal Silence.otf", 40);
            var textureCloseButton = resCache.GetTexture("/Nano/cross.svg.png");
            var windowHeaderTex = resCache.GetTexture("/Nano/window_header.png");
            var windowHeader = new StyleBoxTexture
            {
                Texture = windowHeaderTex,
                PatchMarginBottom = 3,
                ExpandMarginBottom = 3,
            };
            var windowBackgroundTex = resCache.GetTexture("/Nano/window_background.png");
            var windowBackground = new StyleBoxTexture
            {
                Texture =  windowBackgroundTex,
            };
            windowBackground.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
            windowBackground.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);

            var buttonNormalTex = resCache.GetTexture("/Nano/button_normal.png");
            var buttonNormal = new StyleBoxTexture
            {
                Texture = buttonNormalTex,
            };
            buttonNormal.SetPatchMargin(StyleBox.Margin.All, 2);
            buttonNormal.SetContentMarginOverride(StyleBox.Margin.Left | StyleBox.Margin.Right, 4);

            var buttonHoverTex = resCache.GetTexture("/Nano/button_hover.png");
            var buttonHover = new StyleBoxTexture
            {
                Texture = buttonHoverTex,
            };
            buttonHover.SetPatchMargin(StyleBox.Margin.All, 2);
            buttonHover.SetContentMarginOverride(StyleBox.Margin.Left | StyleBox.Margin.Right, 4);

            var buttonPressedTex = resCache.GetTexture("/Nano/button_pressed.png");
            var buttonPressed = new StyleBoxTexture
            {
                Texture = buttonPressedTex,
            };
            buttonPressed.SetPatchMargin(StyleBox.Margin.All, 2);
            buttonPressed.SetContentMarginOverride(StyleBox.Margin.Left | StyleBox.Margin.Right, 4);

            var buttonDisabledTex = resCache.GetTexture("/Nano/button_disabled.png");
            var buttonDisabled = new StyleBoxTexture
            {
                Texture = buttonDisabledTex,
            };
            buttonDisabled.SetPatchMargin(StyleBox.Margin.All, 2);
            buttonDisabled.SetContentMarginOverride(StyleBox.Margin.Left | StyleBox.Margin.Right, 4);

            var lineEditTex = resCache.GetTexture("/Nano/lineedit.png");
            var lineEdit = new StyleBoxTexture
            {
                Texture = lineEditTex,
            };
            lineEdit.SetPatchMargin(StyleBox.Margin.All, 3);
            lineEdit.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            Stylesheet = new Stylesheet(new[]
            {
                // Default font.
                new StyleRule(
                    new SelectorElement(null, null, null, null),
                    new[]
                    {
                        new StyleProperty("font", notoSans12),
                    }),

                // Window title.
                new StyleRule(
                    new SelectorElement(typeof(Label), new[] {SS14Window.StyleClassWindowTitle}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFontColor, NanoGold),
                        new StyleProperty(Label.StylePropertyFont, notoSansBold16),
                    }),
                // Window background.
                new StyleRule(
                    new SelectorElement(null, new[] {SS14Window.StyleClassWindowPanel}, null, null),
                    new[]
                    {
                        new StyleProperty(Panel.StylePropertyPanel, windowBackground),
                    }),
                // Window header.
                new StyleRule(
                    new SelectorElement(typeof(Panel), new[] {SS14Window.StyleClassWindowHeader}, null, null),
                    new[]
                    {
                        new StyleProperty(Panel.StylePropertyPanel, windowHeader),
                    }),
                // Window close button base texture.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        null),
                    new[]
                    {
                        new StyleProperty(TextureButton.StylePropertyTexture, textureCloseButton),
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#4B596A")),
                    }),
                // Window close button hover.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        TextureButton.StylePseudoClassHover),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#7F3636")),
                    }),
                // Window close button pressed.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        TextureButton.StylePseudoClassPressed),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#753131")),
                    }),

                // Regular buttons!
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, Button.StylePseudoClassNormal),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonNormal),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, Button.StylePseudoClassHover),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonHover),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, Button.StylePseudoClassPressed),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonPressed),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, Button.StylePseudoClassDisabled),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonDisabled),
                        new StyleProperty("font-color", Color.FromHex("#E5E5E581")),
                    }),

                // Main menu: Make those buttons bigger.
                new StyleRule(
                    new SelectorChild(
                        new SelectorElement(null, null, "mainMenuVBox", null),
                        new SelectorElement(typeof(Button), null, null, null)),
                    new[]
                    {
                        new StyleProperty("font", animalSilence40),
                    }),

                // Main menu: also make those buttons slightly more separated.
                new StyleRule(new SelectorElement(typeof(BoxContainer), null, "mainMenuVBox", null),
                    new[]
                    {
                        new StyleProperty(BoxContainer.StylePropertySeparation, 2),
                    }),

                // Fancy LineEdit
                new StyleRule(new SelectorElement(typeof(LineEdit), null, null, null),
                    new[]
                    {
                        new StyleProperty(LineEdit.StylePropertyStyleBox, lineEdit),
                    }),
            });
        }
    }
}
