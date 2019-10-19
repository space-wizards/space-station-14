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
        public const string StyleClassLabelHeading = "LabelHeading";
        public const string StyleClassLabelHeadingBigger = "LabelHeadingBigger";
        public const string StyleClassLabelSubText = "LabelSubText";
        public const string StyleClassLabelSecondaryColor = "LabelSecondaryColor";
        public const string StyleClassLabelBig = "LabelBig";
        public const string StyleClassButtonBig = "ButtonBig";
        public static readonly Color NanoGold = Color.FromHex("#A88B5E");

        //Used by the APC and SMES menus
        public const string StyleClassPowerStateNone = "PowerStateNone";
        public const string StyleClassPowerStateLow = "PowerStateLow";
        public const string StyleClassPowerStateGood = "PowerStateGood";

        public Stylesheet Stylesheet { get; }

        public NanoStyle()
        {
            var resCache = IoCManager.Resolve<IResourceCache>();
            var notoSans10 = resCache.GetFont("/Nano/NotoSans/NotoSans-Regular.ttf", 10);
            var notoSans12 = resCache.GetFont("/Nano/NotoSans/NotoSans-Regular.ttf", 12);
            var notoSansDisplayBold14 = resCache.GetFont("/Fonts/NotoSansDisplay/NotoSansDisplay-Bold.ttf", 14);
            var notoSans16 = resCache.GetFont("/Nano/NotoSans/NotoSans-Regular.ttf", 16);
            var notoSansBold16 = resCache.GetFont("/Nano/NotoSans/NotoSans-Bold.ttf", 16);
            var notoSansBold20 = resCache.GetFont("/Nano/NotoSans/NotoSans-Bold.ttf", 20);
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

            // Button styles.
            var buttonTex = resCache.GetTexture("/Nano/button.svg.96dpi.png");
            var buttonNormal = new StyleBoxTexture
            {
                Texture = buttonTex,
                Modulate = Color.FromHex("#464966")
            };
            buttonNormal.SetPatchMargin(StyleBox.Margin.All, 10);
            buttonNormal.SetPadding(StyleBox.Margin.All, 1);
            buttonNormal.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            buttonNormal.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);

            var buttonHover = new StyleBoxTexture(buttonNormal)
            {
                Modulate = Color.FromHex("#575b7f")
            };

            var buttonPressed = new StyleBoxTexture(buttonNormal)
            {
                Modulate = Color.FromHex("#3e6c45")
            };

            var buttonDisabled = new StyleBoxTexture(buttonNormal)
            {
                Modulate = Color.FromHex("#30313c")
            };

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
            tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
            var tabContainerBoxInactive = new StyleBoxFlat {BackgroundColor = new Color(32, 32, 32)};
            tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            var vScrollBarGrabberNormal = new StyleBoxFlat
            {
                BackgroundColor = Color.Gray.WithAlpha(0.35f), ContentMarginLeftOverride = 10
            };
            var vScrollBarGrabberHover = new StyleBoxFlat
            {
                BackgroundColor = new Color(140, 140, 140).WithAlpha(0.35f), ContentMarginLeftOverride = 10
            };
            var vScrollBarGrabberGrabbed = new StyleBoxFlat
            {
                BackgroundColor = new Color(160, 160, 160).WithAlpha(0.35f), ContentMarginLeftOverride = 10
            };

            var hScrollBarGrabberNormal = new StyleBoxFlat
            {
                BackgroundColor = Color.Gray.WithAlpha(0.35f), ContentMarginTopOverride = 10
            };
            var hScrollBarGrabberHover = new StyleBoxFlat
            {
                BackgroundColor = new Color(140, 140, 140).WithAlpha(0.35f), ContentMarginTopOverride = 10
            };
            var hScrollBarGrabberGrabbed = new StyleBoxFlat
            {
                BackgroundColor = new Color(160, 160, 160).WithAlpha(0.35f), ContentMarginTopOverride = 10
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
            var placeholder = new StyleBoxTexture {Texture = placeholderTexture};
            placeholder.SetPatchMargin(StyleBox.Margin.All, 19);
            placeholder.SetExpandMargin(StyleBox.Margin.All, -5);
            placeholder.Mode = StyleBoxTexture.StretchMode.Tile;

            var itemListBackgroundSelected = new StyleBoxFlat {BackgroundColor = new Color(75, 75, 86)};
            itemListBackgroundSelected.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            itemListBackgroundSelected.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);
            var itemListItemBackgroundDisabled = new StyleBoxFlat {BackgroundColor = new Color(10, 10, 12)};
            itemListItemBackgroundDisabled.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            itemListItemBackgroundDisabled.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);
            var itemListItemBackground = new StyleBoxFlat {BackgroundColor = new Color(55, 55, 68)};
            itemListItemBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            itemListItemBackground.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

            // NanoHeading
            var nanoHeadingTex = resCache.GetTexture("/Nano/nanoheading.svg.96dpi.png");
            var nanoHeadingBox = new StyleBoxTexture
            {
                Texture = nanoHeadingTex,
                PatchMarginRight = 10,
                PatchMarginTop = 10,
                ContentMarginTopOverride = 2,
                ContentMarginLeftOverride = 10,
                PaddingTop = 4
            };

            nanoHeadingBox.SetPatchMargin(StyleBox.Margin.Left | StyleBox.Margin.Bottom, 2);

            // Stripe background
            var stripeBackTex = resCache.GetTexture("/Nano/stripeback.svg.96dpi.png");
            var stripeBack = new StyleBoxTexture
            {
                Texture = stripeBackTex,
                Mode = StyleBoxTexture.StretchMode.Tile
            };

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
                        new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
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
                        new[] {TextureButton.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#7F3636")),
                    }),
                // Window close button pressed.
                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {SS14Window.StyleClassWindowCloseButton}, null,
                        new[] {TextureButton.StylePseudoClassPressed}),
                    new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#753131")),
                    }),

                // Regular buttons!
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, new[] {Button.StylePseudoClassNormal}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonNormal),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, new[] {Button.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonHover),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, new[] {Button.StylePseudoClassPressed}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonPressed),
                    }),
                new StyleRule(
                    new SelectorElement(typeof(Button), null, null, new[] {Button.StylePseudoClassDisabled}),
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

                new StyleRule(
                    new SelectorElement(typeof(LineEdit), null, null, new[] {LineEdit.StylePseudoClassPlaceholder}),
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

                new StyleRule(
                    new SelectorElement(typeof(VScrollBar), null, null, new[] {ScrollBar.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            vScrollBarGrabberHover),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(VScrollBar), null, null, new[] {ScrollBar.StylePseudoClassGrabbed}),
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

                new StyleRule(
                    new SelectorElement(typeof(HScrollBar), null, null, new[] {ScrollBar.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(ScrollBar.StylePropertyGrabber,
                            hScrollBarGrabberHover),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(HScrollBar), null, null, new[] {ScrollBar.StylePseudoClassGrabbed}),
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

                new StyleRule(new SelectorElement(typeof(CheckBox), null, null, new[] {Button.StylePseudoClassPressed}),
                    new[]
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

                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] {"tooltipBox"}, null, null), new[]
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
                        itemListItemBackground),
                    new StyleProperty(ItemList.StylePropertyDisabledItemBackground,
                        itemListItemBackgroundDisabled),
                    new StyleProperty(ItemList.StylePropertySelectedItemBackground,
                        itemListBackgroundSelected)
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
                new StyleRule(new SelectorElement(typeof(Placeholder), null, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, placeholder),
                }),

                new StyleRule(
                    new SelectorElement(typeof(Label), new[] {Placeholder.StyleClassPlaceholderText}, null, null), new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, notoSans16),
                        new StyleProperty(Label.StylePropertyFontColor, new Color(103, 103, 103, 128)),
                    }),

                // Big Label
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelHeading}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSansBold16),
                    new StyleProperty(Label.StylePropertyFontColor, NanoGold),
                }),

                // Bigger Label
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelHeadingBigger}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSansBold20),
                    new StyleProperty(Label.StylePropertyFontColor, NanoGold),
                }),

                // Small Label
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelSubText}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSans10),
                    new StyleProperty(Label.StylePropertyFontColor, Color.DarkGray),
                }),

                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelSecondaryColor}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, notoSans12),
                        new StyleProperty(Label.StylePropertyFontColor, Color.DarkGray),
                    }),

                // Big Button
                new StyleRule(new SelectorElement(typeof(Button), new[] {StyleClassButtonBig}, null, null), new[]
                {
                    new StyleProperty("font", notoSans16)
                }),

                //APC and SMES power state label colors
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassPowerStateNone}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFontColor, new Color(0.8f, 0.0f, 0.0f))
                }),

                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassPowerStateLow}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFontColor, new Color(0.9f, 0.36f, 0.0f))
                }),

                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassPowerStateGood}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFontColor, new Color(0.024f, 0.8f, 0.0f))
                }),

                // Those top menu buttons.
                new StyleRule(
                    new SelectorElement(typeof(GameHud.TopButton), null, null, new[] {Button.StylePseudoClassNormal}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonNormal),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(GameHud.TopButton), null, null, new[] {Button.StylePseudoClassPressed}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonPressed),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(GameHud.TopButton), null, null, new[] {Button.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, buttonHover),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(Label), new[] {GameHud.TopButton.StyleClassLabelTopButton}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
                    }),

                // Targeting doll

                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {TargetingDoll.StyleClassTargetDollZone}, null,
                        new[] {TextureButton.StylePseudoClassNormal}), new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#F00")),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {TargetingDoll.StyleClassTargetDollZone}, null,
                        new[] {TextureButton.StylePseudoClassHover}), new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#0F0")),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(TextureButton), new[] {TargetingDoll.StyleClassTargetDollZone}, null,
                        new[] {TextureButton.StylePseudoClassPressed}), new[]
                    {
                        new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#00F")),
                    }),

                // NanoHeading

                new StyleRule(
                    new SelectorChild(
                        SelectorElement.Type(typeof(NanoHeading)),
                        SelectorElement.Type(typeof(PanelContainer))),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, nanoHeadingBox),
                    }),

                // StripeBack
                new StyleRule(
                    SelectorElement.Type(typeof(StripeBack)),
                    new []
                    {
                        new StyleProperty(StripeBack.StylePropertyBackground, stripeBack),
                    }),

                // StyleClassLabelBig
                new StyleRule(
                    SelectorElement.Class(StyleClassLabelBig),
                    new []
                    {
                        new StyleProperty("font", notoSans16),
                    }),
            });
        }
    }
}
