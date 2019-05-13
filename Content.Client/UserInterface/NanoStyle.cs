using Content.Client.GameObjects.EntitySystems;
using Content.Client.Utility;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

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
            var notoSans16 = resCache.GetFont("/Nano/NotoSans/NotoSans-Regular.ttf", 16);
            var notoSansBold16 = resCache.GetFont("/Nano/NotoSans/NotoSans-Bold.ttf", 16);
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
                Texture = windowBackgroundTex,
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

            var tabContainerPanelTex = resCache.GetTexture("/Nano/tabcontainer_panel.png");
            var tabContainerPanel = new StyleBoxTexture
            {
                Texture = tabContainerPanelTex,
            };
            tabContainerPanel.SetPatchMargin(StyleBox.Margin.All, 2);

            var tabContainerBoxActive = new StyleBoxFlat {BackgroundColor = new Color(64, 64, 64)};
            tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 3);
            var tabContainerBoxInactive = new StyleBoxFlat {BackgroundColor = new Color(32, 32, 32)};
            tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 3);

            var vScrollBarGrabberNormal = new StyleBoxFlat
            {
                BackgroundColor = Color.Gray, ContentMarginLeftOverride = 10
            };
            var vScrollBarGrabberHover = new StyleBoxFlat
            {
                BackgroundColor = new Color(140, 140, 140), ContentMarginLeftOverride = 10
            };
            var vScrollBarGrabberGrabbed = new StyleBoxFlat
            {
                BackgroundColor = new Color(160, 160, 160), ContentMarginLeftOverride = 10
            };

            var hScrollBarGrabberNormal = new StyleBoxFlat
            {
                BackgroundColor = Color.Gray, ContentMarginTopOverride = 10
            };
            var hScrollBarGrabberHover = new StyleBoxFlat
            {
                BackgroundColor = new Color(140, 140, 140), ContentMarginTopOverride = 10
            };
            var hScrollBarGrabberGrabbed = new StyleBoxFlat
            {
                BackgroundColor = new Color(160, 160, 160), ContentMarginTopOverride = 10
            };

            var progressBarBackground = new StyleBoxFlat
            {
                BackgroundColor = new Color(0.25f, 0.25f, 0.25f)
            };
            progressBarBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);

            var progressBarForeground = new StyleBoxFlat
            {
                BackgroundColor = new Color(0.25f, 0.50f, 0.25f)
            };
            progressBarForeground.SetContentMarginOverride(StyleBox.Margin.Vertical, 5);

            // CheckBox
            var checkBoxTextureChecked = resCache.GetTexture("/Nano/checkbox_checked.svg.96dpi.png");
            var checkBoxTextureUnchecked = resCache.GetTexture("/Nano/checkbox_unchecked.svg.96dpi.png");

            // Tooltip box
            var tooltipTexture = resCache.GetTexture("/Nano/tooltip.png");
            var tooltipBox = new StyleBoxTexture
            {
                Texture = tooltipTexture,
            };
            tooltipBox.SetPatchMargin(StyleBox.Margin.All, 2);
            tooltipBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            // Placeholder
            var placeholderTexture = resCache.GetTexture("/Nano/placeholder.png");
            var placeholder = new StyleBoxTexture { Texture = placeholderTexture };
            placeholder.SetPatchMargin(StyleBox.Margin.All, 24);
            placeholder.SetExpandMargin(StyleBox.Margin.All, -5);

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
                        new StyleProperty("font", notoSansBold16),
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

                new StyleRule(
                    new SelectorElement(typeof(LineEdit), new[] {LineEdit.StyleClassLineEditNotEditable}, null, null),
                    new[]
                    {
                        new StyleProperty("font-color", new Color(192, 192, 192)),
                    }),

                new StyleRule(new SelectorElement(typeof(LineEdit), null, null, LineEdit.StylePseudoClassPlaceholder),
                    new[]
                    {
                        new StyleProperty("font-color", Color.Gray),
                    }),

                // TabContainer
                new StyleRule(new SelectorElement(typeof(TabContainer), null, null, null),
                    new[]
                    {
                        new StyleProperty(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBox, tabContainerBoxActive),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBoxInactive, tabContainerBoxInactive),
                    }),

                // Scroll bars
                new StyleRule(new SelectorElement(typeof(VScrollBar), null, null, null),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            vScrollBarGrabberNormal),
                    }),

                new StyleRule(new SelectorElement(typeof(VScrollBar), null, null, ScrollBar.StylePseudoClassHover),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            vScrollBarGrabberHover),
                    }),

                new StyleRule(new SelectorElement(typeof(VScrollBar), null, null, ScrollBar.StylePseudoClassGrabbed),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            vScrollBarGrabberGrabbed),
                    }),

                new StyleRule(new SelectorElement(typeof(HScrollBar), null, null, null),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            hScrollBarGrabberNormal),
                    }),

                new StyleRule(new SelectorElement(typeof(HScrollBar), null, null, ScrollBar.StylePseudoClassHover),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            hScrollBarGrabberHover),
                    }),

                new StyleRule(new SelectorElement(typeof(HScrollBar), null, null, ScrollBar.StylePseudoClassGrabbed),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            hScrollBarGrabberGrabbed),
                    }),

                // ProgressBar
                new StyleRule(new SelectorElement(typeof(ProgressBar), null, null, null),
                    new[]
                    {
                        new StyleProperty(ProgressBar.StylePropertyBackground, progressBarBackground),
                        new StyleProperty(ProgressBar.StylePropertyForeground, progressBarForeground)
                    }),

                // CheckBox
                new StyleRule(new SelectorElement(typeof(CheckBox), null, null, null), new[]
                {
                    new StyleProperty(CheckBox.StylePropertyIcon, checkBoxTextureUnchecked),
                }),

                new StyleRule(new SelectorElement(typeof(CheckBox), null, null, Button.StylePseudoClassPressed), new[]
                {
                    new StyleProperty(CheckBox.StylePropertyIcon, checkBoxTextureChecked),
                }),

                new StyleRule(new SelectorElement(typeof(CheckBox), null, null, null), new[]
                {
                    new StyleProperty(CheckBox.StylePropertyHSeparation, 3),
                }),

                // Tooltip
                new StyleRule(new SelectorElement(typeof(Tooltip), null, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox)
                }),

                // Entity tooltip
                new StyleRule(
                    new SelectorElement(typeof(PanelContainer), new[] {ExamineSystem.StyleClassEntityTooltip}, null,
                        null), new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox)
                    }),

                // ItemList
                new StyleRule(new SelectorElement(typeof(ItemList), null, null, null), new[]
                {
                    new StyleProperty(ItemList.StylePropertyBackground,
                        new StyleBoxFlat {BackgroundColor = new Color(32, 32, 40)}),
                    new StyleProperty(ItemList.StylePropertyItemBackground,
                        new StyleBoxFlat {BackgroundColor = new Color(55, 55, 68)}),
                    new StyleProperty(ItemList.StylePropertyDisabledItemBackground,
                        new StyleBoxFlat {BackgroundColor = new Color(10, 10, 12)}),
                    new StyleProperty(ItemList.StylePropertySelectedItemBackground,
                        new StyleBoxFlat {BackgroundColor = new Color(75, 75, 86)})
                }),

                // Tree
                new StyleRule(new SelectorElement(typeof(Tree), null, null, null), new[]
                {
                    new StyleProperty(Tree.StylePropertyBackground,
                        new StyleBoxFlat {BackgroundColor = new Color(32, 32, 40)}),
                    new StyleProperty(Tree.StylePropertyItemBoxSelected, new StyleBoxFlat
                    {
                        BackgroundColor = new Color(55, 55, 68),
                        ContentMarginLeftOverride = 4
                    })
                }),

                // Placeholder
                new StyleRule(new SelectorElement(typeof(Placeholder), null, null, null), new []
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, placeholder),
                }),

                new StyleRule(new SelectorElement(typeof(Label), new []{Placeholder.StyleClassPlaceholderText}, null, null), new []
                {
                    new StyleProperty(Label.StylePropertyFont, notoSans16),
                    new StyleProperty(Label.StylePropertyFontColor, new Color(103, 103, 103, 128)),
                }),
            });
        }
    }
}
