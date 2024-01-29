using System.Linq;
using System.Numerics;
using Content.Client.ContextMenu.UI;
using Content.Client.Examine;
using Content.Client.PDA;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Controls.FancyTree;
using Content.Client.Verbs.UI;
using Content.Shared.Verbs;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets
{
    public static class ResCacheExtension
    {
        public static Font NotoStack(this IResourceCache resCache, string variation = "Regular", int size = 10, bool display = false)
        {
            var ds = display ? "Display" : "";
            var sv = variation.StartsWith("Bold", StringComparison.Ordinal) ? "Bold" : "Regular";
            return resCache.GetFont
            (
                // Ew, but ok
                new[]
                {
                    $"/Fonts/NotoSans{ds}/NotoSans{ds}-{variation}.ttf",
                    $"/Fonts/NotoSans/NotoSansSymbols-{sv}.ttf",
                    "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf"
                },
                size
            );

        }

    }
    // STLYE SHEETS WERE A MISTAKE. KILL ALL OF THIS WITH FIRE
    public sealed class StyleNano : StyleBase
    {
        public const string StyleClassBorderedWindowPanel = "BorderedWindowPanel";
        public const string StyleClassInventorySlotBackground = "InventorySlotBackground";
        public const string StyleClassHandSlotHighlight = "HandSlotHighlight";
        public const string StyleClassChatSubPanel = "ChatSubPanel";
        public const string StyleClassTransparentBorderedWindowPanel = "TransparentBorderedWindowPanel";
        public const string StyleClassHotbarPanel = "HotbarPanel";
        public const string StyleClassTooltipPanel = "tooltipBox";
        public const string StyleClassTooltipAlertTitle = "tooltipAlertTitle";
        public const string StyleClassTooltipAlertDescription = "tooltipAlertDesc";
        public const string StyleClassTooltipAlertCooldown = "tooltipAlertCooldown";
        public const string StyleClassTooltipActionTitle = "tooltipActionTitle";
        public const string StyleClassTooltipActionDescription = "tooltipActionDesc";
        public const string StyleClassTooltipActionCooldown = "tooltipActionCooldown";
        public const string StyleClassTooltipActionRequirements = "tooltipActionCooldown";
        public const string StyleClassTooltipActionCharges = "tooltipActionCharges";
        public const string StyleClassHotbarSlotNumber = "hotbarSlotNumber";
        public const string StyleClassActionSearchBox = "actionSearchBox";
        public const string StyleClassActionMenuItemRevoked = "actionMenuItemRevoked";
        public const string StyleClassChatLineEdit = "chatLineEdit";
        public const string StyleClassChatChannelSelectorButton = "chatSelectorOptionButton";
        public const string StyleClassChatFilterOptionButton = "chatFilterOptionButton";
        public const string StyleClassStorageButton = "storageButton";

        public const string StyleClassSliderRed = "Red";
        public const string StyleClassSliderGreen = "Green";
        public const string StyleClassSliderBlue = "Blue";
        public const string StyleClassSliderWhite = "White";

        public const string StyleClassLabelHeadingBigger = "LabelHeadingBigger";
        public const string StyleClassLabelKeyText = "LabelKeyText";
        public const string StyleClassLabelSecondaryColor = "LabelSecondaryColor";
        public const string StyleClassLabelBig = "LabelBig";
        public const string StyleClassButtonBig = "ButtonBig";

        public const string StyleClassPopupMessageSmall = "PopupMessageSmall";
        public const string StyleClassPopupMessageSmallCaution = "PopupMessageSmallCaution";
        public const string StyleClassPopupMessageMedium = "PopupMessageMedium";
        public const string StyleClassPopupMessageMediumCaution = "PopupMessageMediumCaution";
        public const string StyleClassPopupMessageLarge = "PopupMessageLarge";
        public const string StyleClassPopupMessageLargeCaution = "PopupMessageLargeCaution";

        public static readonly Color PanelDark = Color.FromHex("#1E1E22");

        public static readonly Color NanoGold = Color.FromHex("#A88B5E");
        public static readonly Color GoodGreenFore = Color.FromHex("#31843E");
        public static readonly Color ConcerningOrangeFore = Color.FromHex("#A5762F");
        public static readonly Color DangerousRedFore = Color.FromHex("#BB3232");
        public static readonly Color DisabledFore = Color.FromHex("#5A5A5A");

        public static readonly Color ButtonColorDefault = Color.FromHex("#464966");
        public static readonly Color ButtonColorDefaultRed = Color.FromHex("#D43B3B");
        public static readonly Color ButtonColorHovered = Color.FromHex("#575b7f");
        public static readonly Color ButtonColorHoveredRed = Color.FromHex("#DF6B6B");
        public static readonly Color ButtonColorPressed = Color.FromHex("#3e6c45");
        public static readonly Color ButtonColorDisabled = Color.FromHex("#30313c");

        public static readonly Color ButtonColorCautionDefault = Color.FromHex("#ab3232");
        public static readonly Color ButtonColorCautionHovered = Color.FromHex("#cf2f2f");
        public static readonly Color ButtonColorCautionPressed = Color.FromHex("#3e6c45");
        public static readonly Color ButtonColorCautionDisabled = Color.FromHex("#602a2a");

        public static readonly Color ButtonColorGoodDefault = Color.FromHex("#3E6C45");
        public static readonly Color ButtonColorGoodHovered = Color.FromHex("#31843E");

        //NavMap
        public static readonly Color PointRed = Color.FromHex("#B02E26");
        public static readonly Color PointGreen = Color.FromHex("#38b026");
        public static readonly Color PointMagenta = Color.FromHex("#FF00FF");

        // Context menu button colors
        public static readonly Color ButtonColorContext = Color.FromHex("#1119");
        public static readonly Color ButtonColorContextHover = Color.DarkSlateGray;
        public static readonly Color ButtonColorContextPressed = Color.LightSlateGray;
        public static readonly Color ButtonColorContextDisabled = Color.Black;

        // Examine button colors
        public static readonly Color ExamineButtonColorContext = Color.Transparent;
        public static readonly Color ExamineButtonColorContextHover = Color.DarkSlateGray;
        public static readonly Color ExamineButtonColorContextPressed = Color.LightSlateGray;
        public static readonly Color ExamineButtonColorContextDisabled = Color.FromHex("#5A5A5A");

        // Fancy Tree elements
        public static readonly Color FancyTreeEvenRowColor = Color.FromHex("#25252A");
        public static readonly Color FancyTreeOddRowColor = FancyTreeEvenRowColor * new Color(0.8f, 0.8f, 0.8f);
        public static readonly Color FancyTreeSelectedRowColor = new Color(55, 55, 68);

        //Used by the APC and SMES menus
        public const string StyleClassPowerStateNone = "PowerStateNone";
        public const string StyleClassPowerStateLow = "PowerStateLow";
        public const string StyleClassPowerStateGood = "PowerStateGood";

        public const string StyleClassItemStatus = "ItemStatus";

        //Background
        public const string StyleClassBackgroundBaseDark = "PanelBackgroundBaseDark";

        //Buttons
        public const string StyleClassCrossButtonRed = "CrossButtonRed";
        public const string StyleClassButtonColorRed = "ButtonColorRed";
        public const string StyleClassButtonColorGreen = "ButtonColorGreen";

        public override Stylesheet Stylesheet { get; }

        public StyleNano(IResourceCache resCache) : base(resCache)
        {
            var notoSans8 = resCache.NotoStack(size: 8);
            var notoSans10 = resCache.NotoStack(size: 10);
            var notoSansItalic10 = resCache.NotoStack(variation: "Italic", size: 10);
            var notoSans12 = resCache.NotoStack(size: 12);
            var notoSansItalic12 = resCache.NotoStack(variation: "Italic", size: 12);
            var notoSansBold12 = resCache.NotoStack(variation: "Bold", size: 12);
            var notoSansBoldItalic12 = resCache.NotoStack(variation: "BoldItalic", size: 12);
            var notoSansBoldItalic14 = resCache.NotoStack(variation: "BoldItalic", size: 14);
            var notoSansBoldItalic16 = resCache.NotoStack(variation: "BoldItalic", size: 16);
            var notoSansDisplayBold14 = resCache.NotoStack(variation: "Bold", display: true, size: 14);
            var notoSansDisplayBold16 = resCache.NotoStack(variation: "Bold", display: true, size: 16);
            var notoSans15 = resCache.NotoStack(variation: "Regular", size: 15);
            var notoSans16 = resCache.NotoStack(variation: "Regular", size: 16);
            var notoSansBold16 = resCache.NotoStack(variation: "Bold", size: 16);
            var notoSansBold18 = resCache.NotoStack(variation: "Bold", size: 18);
            var notoSansBold20 = resCache.NotoStack(variation: "Bold", size: 20);
            var notoSansMono = resCache.GetFont("/EngineFonts/NotoSans/NotoSansMono-Regular.ttf", size: 12);
            var windowHeaderTex = resCache.GetTexture("/Textures/Interface/Nano/window_header.png");
            var windowHeader = new StyleBoxTexture
            {
                Texture = windowHeaderTex,
                PatchMarginBottom = 3,
                ExpandMarginBottom = 3,
                ContentMarginBottomOverride = 0
            };
            var windowHeaderAlertTex = resCache.GetTexture("/Textures/Interface/Nano/window_header_alert.png");
            var windowHeaderAlert = new StyleBoxTexture
            {
                Texture = windowHeaderAlertTex,
                PatchMarginBottom = 3,
                ExpandMarginBottom = 3,
                ContentMarginBottomOverride = 0
            };
            var windowBackgroundTex = resCache.GetTexture("/Textures/Interface/Nano/window_background.png");
            var windowBackground = new StyleBoxTexture
            {
                Texture = windowBackgroundTex,
            };
            windowBackground.SetPatchMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);
            windowBackground.SetExpandMargin(StyleBox.Margin.Horizontal | StyleBox.Margin.Bottom, 2);

            var borderedWindowBackgroundTex = resCache.GetTexture("/Textures/Interface/Nano/window_background_bordered.png");
            var borderedWindowBackground = new StyleBoxTexture
            {
                Texture = borderedWindowBackgroundTex,
            };
            borderedWindowBackground.SetPatchMargin(StyleBox.Margin.All, 2);

            var contextMenuBackground = new StyleBoxTexture
            {
                Texture = borderedWindowBackgroundTex,
            };
            contextMenuBackground.SetPatchMargin(StyleBox.Margin.All, ContextMenuElement.ElementMargin);

            var invSlotBgTex = resCache.GetTexture("/Textures/Interface/Inventory/inv_slot_background.png");
            var invSlotBg = new StyleBoxTexture
            {
                Texture = invSlotBgTex,
            };
            invSlotBg.SetPatchMargin(StyleBox.Margin.All, 2);
            invSlotBg.SetContentMarginOverride(StyleBox.Margin.All, 0);

            var handSlotHighlightTex = resCache.GetTexture("/Textures/Interface/Inventory/hand_slot_highlight.png");
            var handSlotHighlight = new StyleBoxTexture
            {
                Texture = handSlotHighlightTex,
            };
            handSlotHighlight.SetPatchMargin(StyleBox.Margin.All, 2);

            var borderedTransparentWindowBackgroundTex = resCache.GetTexture("/Textures/Interface/Nano/transparent_window_background_bordered.png");
            var borderedTransparentWindowBackground = new StyleBoxTexture
            {
                Texture = borderedTransparentWindowBackgroundTex,
            };
            borderedTransparentWindowBackground.SetPatchMargin(StyleBox.Margin.All, 2);

            var hotbarBackground = new StyleBoxTexture
            {
                Texture = borderedWindowBackgroundTex,
            };
            hotbarBackground.SetPatchMargin(StyleBox.Margin.All, 2);
            hotbarBackground.SetExpandMargin(StyleBox.Margin.All, 4);

            var buttonStorage = new StyleBoxTexture(BaseButton);
            buttonStorage.SetPatchMargin(StyleBox.Margin.All, 10);
            buttonStorage.SetPadding(StyleBox.Margin.All, 0);
            buttonStorage.SetContentMarginOverride(StyleBox.Margin.Vertical, 0);
            buttonStorage.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

            var buttonContext = new StyleBoxTexture { Texture = Texture.White };

            var buttonRectTex = resCache.GetTexture("/Textures/Interface/Nano/light_panel_background_bordered.png");
            var buttonRect = new StyleBoxTexture(BaseButton)
            {
                Texture = buttonRectTex
            };
            buttonRect.SetPatchMargin(StyleBox.Margin.All, 2);
            buttonRect.SetPadding(StyleBox.Margin.All, 2);
            buttonRect.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            buttonRect.SetContentMarginOverride(StyleBox.Margin.Horizontal, 2);

            var buttonRectHover = new StyleBoxTexture(buttonRect)
            {
                Modulate = ButtonColorHovered
            };

            var buttonRectPressed = new StyleBoxTexture(buttonRect)
            {
                Modulate = ButtonColorPressed
            };

            var buttonRectDisabled = new StyleBoxTexture(buttonRect)
            {
                Modulate = ButtonColorDisabled
            };

            var buttonRectActionMenuItemTex = resCache.GetTexture("/Textures/Interface/Nano/black_panel_light_thin_border.png");
            var buttonRectActionMenuRevokedItemTex = resCache.GetTexture("/Textures/Interface/Nano/black_panel_red_thin_border.png");
            var buttonRectActionMenuItem = new StyleBoxTexture(BaseButton)
            {
                Texture = buttonRectActionMenuItemTex
            };
            buttonRectActionMenuItem.SetPatchMargin(StyleBox.Margin.All, 2);
            buttonRectActionMenuItem.SetPadding(StyleBox.Margin.All, 2);
            buttonRectActionMenuItem.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            buttonRectActionMenuItem.SetContentMarginOverride(StyleBox.Margin.Horizontal, 2);
            var buttonRectActionMenuItemRevoked = new StyleBoxTexture(buttonRectActionMenuItem)
            {
                Texture = buttonRectActionMenuRevokedItemTex
            };
            var buttonRectActionMenuItemHover = new StyleBoxTexture(buttonRectActionMenuItem)
            {
                Modulate = ButtonColorHovered
            };
            var buttonRectActionMenuItemPressed = new StyleBoxTexture(buttonRectActionMenuItem)
            {
                Modulate = ButtonColorPressed
            };

            var buttonTex = resCache.GetTexture("/Textures/Interface/Nano/button.svg.96dpi.png");
            var topButtonBase = new StyleBoxTexture
            {
             Texture = buttonTex,
            };
            topButtonBase.SetPatchMargin(StyleBox.Margin.All, 10);
            topButtonBase.SetPadding(StyleBox.Margin.All, 0);
            topButtonBase.SetContentMarginOverride(StyleBox.Margin.All, 0);

            var topButtonOpenRight = new StyleBoxTexture(topButtonBase)
            {
             Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
            };
            topButtonOpenRight.SetPatchMargin(StyleBox.Margin.Right, 0);

            var topButtonOpenLeft = new StyleBoxTexture(topButtonBase)
            {
             Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(14, 24))),
            };
            topButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);

            var topButtonSquare = new StyleBoxTexture(topButtonBase)
            {
             Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(10, 0), new Vector2(3, 24))),
            };
            topButtonSquare.SetPatchMargin(StyleBox.Margin.Horizontal, 0);

            var chatChannelButtonTex = resCache.GetTexture("/Textures/Interface/Nano/rounded_button.svg.96dpi.png");
            var chatChannelButton = new StyleBoxTexture
            {
                Texture = chatChannelButtonTex,
            };
            chatChannelButton.SetPatchMargin(StyleBox.Margin.All, 5);
            chatChannelButton.SetPadding(StyleBox.Margin.All, 2);

            var chatFilterButtonTex = resCache.GetTexture("/Textures/Interface/Nano/rounded_button_bordered.svg.96dpi.png");
            var chatFilterButton = new StyleBoxTexture
            {
                Texture = chatFilterButtonTex,
            };
            chatFilterButton.SetPatchMargin(StyleBox.Margin.All, 5);
            chatFilterButton.SetPadding(StyleBox.Margin.All, 2);

            var textureInvertedTriangle = resCache.GetTexture("/Textures/Interface/Nano/inverted_triangle.svg.png");

            var lineEditTex = resCache.GetTexture("/Textures/Interface/Nano/lineedit.png");
            var lineEdit = new StyleBoxTexture
            {
                Texture = lineEditTex,
            };
            lineEdit.SetPatchMargin(StyleBox.Margin.All, 3);
            lineEdit.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            var chatSubBGTex = resCache.GetTexture("/Textures/Interface/Nano/chat_sub_background.png");
            var chatSubBG = new StyleBoxTexture
            {
                Texture = chatSubBGTex,
            };
            chatSubBG.SetPatchMargin(StyleBox.Margin.All, 2);

            var actionSearchBoxTex = resCache.GetTexture("/Textures/Interface/Nano/black_panel_dark_thin_border.png");
            var actionSearchBox = new StyleBoxTexture
            {
                Texture = actionSearchBoxTex,
            };
            actionSearchBox.SetPatchMargin(StyleBox.Margin.All, 3);
            actionSearchBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            var tabContainerPanelTex = resCache.GetTexture("/Textures/Interface/Nano/tabcontainer_panel.png");
            var tabContainerPanel = new StyleBoxTexture
            {
                Texture = tabContainerPanelTex,
            };
            tabContainerPanel.SetPatchMargin(StyleBox.Margin.All, 2);

            var tabContainerBoxActive = new StyleBoxFlat {BackgroundColor = new Color(64, 64, 64)};
            tabContainerBoxActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);
            var tabContainerBoxInactive = new StyleBoxFlat {BackgroundColor = new Color(32, 32, 32)};
            tabContainerBoxInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 5);

            var progressBarBackground = new StyleBoxFlat
            {
                BackgroundColor = new Color(0.25f, 0.25f, 0.25f)
            };
            progressBarBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);

            var progressBarForeground = new StyleBoxFlat
            {
                BackgroundColor = new Color(0.25f, 0.50f, 0.25f)
            };
            progressBarForeground.SetContentMarginOverride(StyleBox.Margin.Vertical, 14.5f);

            // CheckBox
            var checkBoxTextureChecked = resCache.GetTexture("/Textures/Interface/Nano/checkbox_checked.svg.96dpi.png");
            var checkBoxTextureUnchecked = resCache.GetTexture("/Textures/Interface/Nano/checkbox_unchecked.svg.96dpi.png");

            // Tooltip box
            var tooltipTexture = resCache.GetTexture("/Textures/Interface/Nano/tooltip.png");
            var tooltipBox = new StyleBoxTexture
            {
                Texture = tooltipTexture,
            };
            tooltipBox.SetPatchMargin(StyleBox.Margin.All, 2);
            tooltipBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

            // Whisper box
            var whisperTexture = resCache.GetTexture("/Textures/Interface/Nano/whisper.png");
            var whisperBox = new StyleBoxTexture
            {
                Texture = whisperTexture,
            };
            whisperBox.SetPatchMargin(StyleBox.Margin.All, 2);
            whisperBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 7);

            // Placeholder
            var placeholderTexture = resCache.GetTexture("/Textures/Interface/Nano/placeholder.png");
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
            var itemListItemBackgroundTransparent = new StyleBoxFlat {BackgroundColor = Color.Transparent};
            itemListItemBackgroundTransparent.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
            itemListItemBackgroundTransparent.SetContentMarginOverride(StyleBox.Margin.Horizontal, 4);

            var squareTex = resCache.GetTexture("/Textures/Interface/Nano/square.png");
            var listContainerButton = new StyleBoxTexture
            {
                Texture = squareTex,
                ContentMarginLeftOverride = 10
            };

            // NanoHeading
            var nanoHeadingTex = resCache.GetTexture("/Textures/Interface/Nano/nanoheading.svg.96dpi.png");
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
            var stripeBackTex = resCache.GetTexture("/Textures/Interface/Nano/stripeback.svg.96dpi.png");
            var stripeBack = new StyleBoxTexture
            {
                Texture = stripeBackTex,
                Mode = StyleBoxTexture.StretchMode.Tile
            };

            // Slider
            var sliderOutlineTex = resCache.GetTexture("/Textures/Interface/Nano/slider_outline.svg.96dpi.png");
            var sliderFillTex = resCache.GetTexture("/Textures/Interface/Nano/slider_fill.svg.96dpi.png");
            var sliderGrabTex = resCache.GetTexture("/Textures/Interface/Nano/slider_grabber.svg.96dpi.png");

            var sliderFillBox = new StyleBoxTexture
            {
                Texture = sliderFillTex,
                Modulate = Color.FromHex("#3E6C45")
            };

            var sliderBackBox = new StyleBoxTexture
            {
                Texture = sliderFillTex,
                Modulate = PanelDark,
            };

            var sliderForeBox = new StyleBoxTexture
            {
                Texture = sliderOutlineTex,
                Modulate = Color.FromHex("#494949")
            };

            var sliderGrabBox = new StyleBoxTexture
            {
                Texture = sliderGrabTex,
            };

            sliderFillBox.SetPatchMargin(StyleBox.Margin.All, 12);
            sliderBackBox.SetPatchMargin(StyleBox.Margin.All, 12);
            sliderForeBox.SetPatchMargin(StyleBox.Margin.All, 12);
            sliderGrabBox.SetPatchMargin(StyleBox.Margin.All, 12);

            var sliderFillGreen = new StyleBoxTexture(sliderFillBox) {Modulate = Color.LimeGreen};
            var sliderFillRed = new StyleBoxTexture(sliderFillBox) {Modulate = Color.Red};
            var sliderFillBlue = new StyleBoxTexture(sliderFillBox) {Modulate = Color.Blue};
            var sliderFillWhite = new StyleBoxTexture(sliderFillBox) { Modulate = Color.White };

            var boxFont13 = resCache.GetFont("/Fonts/Boxfont-round/Boxfont Round.ttf", 13);

            var insetBack = new StyleBoxTexture
            {
                Texture = buttonTex,
                Modulate = Color.FromHex("#202023"),
            };
            insetBack.SetPatchMargin(StyleBox.Margin.All, 10);

            // Default paper background:
            var paperBackground = new StyleBoxTexture
            {
                Texture = resCache.GetTexture("/Textures/Interface/Paper/paper_background_default.svg.96dpi.png"),
                Modulate = Color.FromHex("#eaedde"), // A light cream
            };
            paperBackground.SetPatchMargin(StyleBox.Margin.All, 16.0f);

            var contextMenuExpansionTexture = resCache.GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png");
            var verbMenuConfirmationTexture = resCache.GetTexture("/Textures/Interface/VerbIcons/group.svg.192dpi.png");

            // south-facing arrow:
            var directionIconArrowTex = resCache.GetTexture("/Textures/Interface/VerbIcons/drop.svg.192dpi.png");
            var directionIconQuestionTex = resCache.GetTexture("/Textures/Interface/VerbIcons/information.svg.192dpi.png");
            var directionIconHereTex = resCache.GetTexture("/Textures/Interface/VerbIcons/dot.svg.192dpi.png");

            Stylesheet = new Stylesheet(BaseRules.Concat(new[]
            {
                Element().Class("monospace")
                    .Prop("font", notoSansMono),
                // Window title.
                new StyleRule(
                    new SelectorElement(typeof(Label), new[] {DefaultWindow.StyleClassWindowTitle}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFontColor, NanoGold),
                        new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
                    }),
                // Alert (white) window title.
                new StyleRule(
                    new SelectorElement(typeof(Label), new[] {"windowTitleAlert"}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFontColor, Color.White),
                        new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
                    }),
                // Window background.
                new StyleRule(
                    new SelectorElement(null, new[] {DefaultWindow.StyleClassWindowPanel}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, windowBackground),
                    }),
                // bordered window background
                new StyleRule(
                    new SelectorElement(null, new[] {StyleClassBorderedWindowPanel}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, borderedWindowBackground),
                    }),
                new StyleRule(
                    new SelectorElement(null, new[] {StyleClassTransparentBorderedWindowPanel}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, borderedTransparentWindowBackground),
                    }),
                // inventory slot background
                new StyleRule(
                    new SelectorElement(null, new[] {StyleClassInventorySlotBackground}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, invSlotBg),
                    }),
                // hand slot highlight
                new StyleRule(
                    new SelectorElement(null, new[] {StyleClassHandSlotHighlight}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, handSlotHighlight),
                    }),
                // Hotbar background
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] {StyleClassHotbarPanel}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, hotbarBackground),
                    }),
                // Window header.
                new StyleRule(
                    new SelectorElement(typeof(PanelContainer), new[] {DefaultWindow.StyleClassWindowHeader}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, windowHeader),
                    }),
                // Alert (red) window header.
                new StyleRule(
                    new SelectorElement(typeof(PanelContainer), new[] {"windowHeaderAlert"}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, windowHeaderAlert),
                    }),

                // Shapes for the buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButton),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenRight)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenRight),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenLeft)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenLeft),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonOpenBoth)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonOpenBoth),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Class(ButtonSquare)
                    .Prop(ContainerButton.StylePropertyStyleBox, BaseButtonSquare),

                new StyleRule(new SelectorElement(typeof(Label), new[] { Button.StyleClassButton }, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyAlignMode, Label.AlignMode.Center),
                }),

                // Colors for the buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefault),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorHovered),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDisabled),

                // Colors for the caution buttons.
                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDefault),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionHovered),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionPressed),

                Element<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(ButtonCaution)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDisabled),

                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), null, null, new[] {ContainerButton.StylePseudoClassDisabled}),
                    new SelectorElement(typeof(Label), null, null, null)),
                    new[]
                    {
                        new StyleProperty("font-color", Color.FromHex("#E5E5E581")),
                    }),

                // Context Menu window
                Element<PanelContainer>().Class(ContextMenuPopup.StyleClassContextMenuPopup)
                    .Prop(PanelContainer.StylePropertyPanel, contextMenuBackground),

                // Context menu buttons
                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),

                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorContext),

                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorContextHover),

                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorContextPressed),

                Element<ContextMenuElement>().Class(ContextMenuElement.StyleClassContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorContextDisabled),

                // Context Menu Labels
                Element<RichTextLabel>().Class(InteractionVerb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, notoSansBoldItalic12),

                Element<RichTextLabel>().Class(ActivationVerb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, notoSansBold12),

                Element<RichTextLabel>().Class(AlternativeVerb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, notoSansItalic12),

                Element<RichTextLabel>().Class(Verb.DefaultTextStyleClass)
                    .Prop(Label.StylePropertyFont, notoSans12),

                Element<TextureRect>().Class(ContextMenuElement.StyleClassContextMenuExpansionTexture)
                    .Prop(TextureRect.StylePropertyTexture, contextMenuExpansionTexture),

                Element<TextureRect>().Class(VerbMenuElement.StyleClassVerbMenuConfirmationTexture)
                    .Prop(TextureRect.StylePropertyTexture, verbMenuConfirmationTexture),

                // Context menu confirm buttons
                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),

                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDefault),

                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionHovered),

                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionPressed),

                Element<ContextMenuElement>().Class(ConfirmationMenuElement.StyleClassConfirmationContextMenuButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorCautionDisabled),

                // Examine buttons
                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonContext),

                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContext),

                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContextHover),

                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContextPressed),

                Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ExamineButtonColorContextDisabled),

                // Direction / arrow icon
                Element<DirectionIcon>().Class(DirectionIcon.StyleClassDirectionIconArrow)
                    .Prop(TextureRect.StylePropertyTexture, directionIconArrowTex),

                Element<DirectionIcon>().Class(DirectionIcon.StyleClassDirectionIconUnknown)
                    .Prop(TextureRect.StylePropertyTexture, directionIconQuestionTex),

                Element<DirectionIcon>().Class(DirectionIcon.StyleClassDirectionIconHere)
                    .Prop(TextureRect.StylePropertyTexture, directionIconHereTex),

                // Thin buttons (No padding nor vertical margin)
                Element<ContainerButton>().Class(StyleClassStorageButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, buttonStorage),

                Element<ContainerButton>().Class(StyleClassStorageButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefault),

                Element<ContainerButton>().Class(StyleClassStorageButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorHovered),

                Element<ContainerButton>().Class(StyleClassStorageButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorPressed),

                Element<ContainerButton>().Class(StyleClassStorageButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDisabled),
// ListContainer
                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Prop(ContainerButton.StylePropertyStyleBox, listContainerButton),

                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, new Color(55, 55, 68)),

                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, new Color(75, 75, 86)),

                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(Control.StylePropertyModulateSelf, new Color(75, 75, 86)),

                Element<ContainerButton>().Class(ListContainer.StyleClassListContainerButton)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(Control.StylePropertyModulateSelf, new Color(10, 10, 12)),

                // Main menu: Make those buttons bigger.
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), null, "mainMenu", null),
                    new SelectorElement(typeof(Label), null, null, null)),
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

                Element<TextEdit>().Pseudo(TextEdit.StylePseudoClassPlaceholder)
                    .Prop("font-color", Color.Gray),

                // Chat lineedit - we don't actually draw a stylebox around the lineedit itself, we put it around the
                // input + other buttons, so we must clear the default stylebox
                new StyleRule(new SelectorElement(typeof(LineEdit), new[] {StyleClassChatLineEdit}, null, null),
                    new[]
                    {
                        new StyleProperty(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
                    }),

                // chat subpanels (chat lineedit backing, popup backings)
                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] {StyleClassChatSubPanel}, null, null),
                    new[]
                    {
                        new StyleProperty(PanelContainer.StylePropertyPanel, chatSubBG),
                    }),

                // Action searchbox lineedit
                new StyleRule(new SelectorElement(typeof(LineEdit), new[] {StyleClassActionSearchBox}, null, null),
                    new[]
                    {
                        new StyleProperty(LineEdit.StylePropertyStyleBox, actionSearchBox),
                    }),

                // TabContainer
                new StyleRule(new SelectorElement(typeof(TabContainer), null, null, null),
                    new[]
                    {
                        new StyleProperty(TabContainer.StylePropertyPanelStyleBox, tabContainerPanel),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBox, tabContainerBoxActive),
                        new StyleProperty(TabContainer.StylePropertyTabStyleBoxInactive, tabContainerBoxInactive),
                    }),

                // ProgressBar
                new StyleRule(new SelectorElement(typeof(ProgressBar), null, null, null),
                    new[]
                    {
                        new StyleProperty(ProgressBar.StylePropertyBackground, progressBarBackground),
                        new StyleProperty(ProgressBar.StylePropertyForeground, progressBarForeground)
                    }),

                // CheckBox
                new StyleRule(new SelectorElement(typeof(TextureRect), new [] { CheckBox.StyleClassCheckBox }, null, null), new[]
                {
                    new StyleProperty(TextureRect.StylePropertyTexture, checkBoxTextureUnchecked),
                }),

                new StyleRule(new SelectorElement(typeof(TextureRect), new [] { CheckBox.StyleClassCheckBox, CheckBox.StyleClassCheckBoxChecked }, null, null), new[]
                {
                    new StyleProperty(TextureRect.StylePropertyTexture, checkBoxTextureChecked),
                }),

                new StyleRule(new SelectorElement(typeof(BoxContainer), new [] { CheckBox.StyleClassCheckBox }, null, null), new[]
                {
                    new StyleProperty(BoxContainer.StylePropertySeparation, 10),
                }),

                // Tooltip
                new StyleRule(new SelectorElement(typeof(Tooltip), null, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox)
                }),

                new StyleRule(new SelectorElement(typeof(PanelContainer), new [] { StyleClassTooltipPanel }, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox)
                }),

                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] {"speechBox", "sayBox"}, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, tooltipBox)
                }),

                new StyleRule(new SelectorElement(typeof(PanelContainer), new[] {"speechBox", "whisperBox"}, null, null), new[]
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, whisperBox)
                }),

                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(PanelContainer), new[] {"speechBox", "emoteBox"}, null, null),
                    new SelectorElement(typeof(RichTextLabel), null, null, null)),
                    new[]
                {
                    new StyleProperty("font", notoSansItalic12),
                }),

                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassLabelKeyText}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSansBold12),
                    new StyleProperty( Control.StylePropertyModulateSelf, NanoGold)
                }),

                // alert tooltip
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipAlertTitle}, null, null), new[]
                {
                    new StyleProperty("font", notoSansBold18)
                }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipAlertDescription}, null, null), new[]
                {
                    new StyleProperty("font", notoSans16)
                }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipAlertCooldown}, null, null), new[]
                {
                    new StyleProperty("font", notoSans16)
                }),

                // action tooltip
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipActionTitle}, null, null), new[]
                {
                    new StyleProperty("font", notoSansBold16)
                }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipActionDescription}, null, null), new[]
                {
                    new StyleProperty("font", notoSans15)
                }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipActionCooldown}, null, null), new[]
                {
                    new StyleProperty("font", notoSans15)
                }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipActionRequirements}, null, null), new[]
                {
                    new StyleProperty("font", notoSans15)
                }),
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassTooltipActionCharges}, null, null), new[]
                {
                    new StyleProperty("font", notoSans15)
                }),

                // small number for the entity counter in the entity menu
                new StyleRule(new SelectorElement(typeof(Label), new[] {ContextMenuElement.StyleClassEntityMenuIconLabel}, null, null), new[]
                {
                    new StyleProperty("font", notoSans10),
                    new StyleProperty(Label.StylePropertyAlignMode, Label.AlignMode.Right),
                }),

                // hotbar slot
                new StyleRule(new SelectorElement(typeof(RichTextLabel), new[] {StyleClassHotbarSlotNumber}, null, null), new[]
                {
                    new StyleProperty("font", notoSansDisplayBold16)
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

                new StyleRule(new SelectorElement(typeof(ItemList), new[] {"transparentItemList"}, null, null), new[]
                {
                    new StyleProperty(ItemList.StylePropertyBackground,
                        new StyleBoxFlat {BackgroundColor = Color.Transparent}),
                    new StyleProperty(ItemList.StylePropertyItemBackground,
                        itemListItemBackgroundTransparent),
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
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelHeadingBigger}, null, null),
                    new[]
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

                // Label Key
                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelKeyText}, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyFont, notoSansBold12),
                    new StyleProperty(Label.StylePropertyFontColor, NanoGold)
                }),

                new StyleRule(new SelectorElement(typeof(Label), new[] {StyleClassLabelSecondaryColor}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, notoSans12),
                        new StyleProperty(Label.StylePropertyFontColor, Color.DarkGray),
                    }),

                // Big Button
                new StyleRule(new SelectorChild(
                    new SelectorElement(typeof(Button), new[] {StyleClassButtonBig}, null, null),
                    new SelectorElement(typeof(Label), null, null, null)),
                    new[]
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
                // these use slight variations on the various BaseButton styles so that the content within them appears centered,
                // which is NOT the case for the default BaseButton styles (OpenLeft/OpenRight adds extra padding on one of the sides
                // which makes the TopButton icons appear off-center, which we don't want).
                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] {ButtonSquare}, null, null),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, topButtonSquare),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] {ButtonOpenLeft}, null, null),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, topButtonOpenLeft),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] {ButtonOpenRight}, null, null),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyStyleBox, topButtonOpenRight),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] {Button.StylePseudoClassNormal}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyModulateSelf, ButtonColorDefault),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] {MenuButton.StyleClassRedTopButton}, null, new[] {Button.StylePseudoClassNormal}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyModulateSelf, ButtonColorDefaultRed),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] {Button.StylePseudoClassNormal}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyModulateSelf, ButtonColorDefault),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] {Button.StylePseudoClassPressed}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyModulateSelf, ButtonColorPressed),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), null, null, new[] {Button.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyModulateSelf, ButtonColorHovered),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(MenuButton), new[] {MenuButton.StyleClassRedTopButton}, null, new[] {Button.StylePseudoClassHover}),
                    new[]
                    {
                        new StyleProperty(Button.StylePropertyModulateSelf, ButtonColorHoveredRed),
                    }),

                new StyleRule(
                    new SelectorElement(typeof(Label), new[] {MenuButton.StyleClassLabelTopButton}, null, null),
                    new[]
                    {
                        new StyleProperty(Label.StylePropertyFont, notoSansDisplayBold14),
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
                    new[]
                    {
                        new StyleProperty(StripeBack.StylePropertyBackground, stripeBack),
                    }),

                // StyleClassLabelBig
                new StyleRule(
                    SelectorElement.Class(StyleClassLabelBig),
                    new[]
                    {
                        new StyleProperty("font", notoSans16),
                    }),

                // StyleClassItemStatus
                new StyleRule(SelectorElement.Class(StyleClassItemStatus), new[]
                {
                    new StyleProperty("font", notoSans10),
                }),

                // Slider
                new StyleRule(SelectorElement.Type(typeof(Slider)), new []
                {
                    new StyleProperty(Slider.StylePropertyBackground, sliderBackBox),
                    new StyleProperty(Slider.StylePropertyForeground, sliderForeBox),
                    new StyleProperty(Slider.StylePropertyGrabber, sliderGrabBox),
                    new StyleProperty(Slider.StylePropertyFill, sliderFillBox),
                }),

                new StyleRule(SelectorElement.Type(typeof(ColorableSlider)), new []
                {
                    new StyleProperty(ColorableSlider.StylePropertyFillWhite, sliderFillWhite),
                    new StyleProperty(ColorableSlider.StylePropertyBackgroundWhite, sliderFillWhite),
                }),

                new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderRed}, null, null), new []
                {
                    new StyleProperty(Slider.StylePropertyFill, sliderFillRed),
                }),

                new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderGreen}, null, null), new []
                {
                    new StyleProperty(Slider.StylePropertyFill, sliderFillGreen),
                }),

                new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderBlue}, null, null), new []
                {
                    new StyleProperty(Slider.StylePropertyFill, sliderFillBlue),
                }),

                new StyleRule(new SelectorElement(typeof(Slider), new []{StyleClassSliderWhite}, null, null), new []
                {
                    new StyleProperty(Slider.StylePropertyFill, sliderFillWhite),
                }),

                // chat channel option selector
                new StyleRule(new SelectorElement(typeof(Button), new[] {StyleClassChatChannelSelectorButton}, null, null), new[]
                {
                    new StyleProperty(Button.StylePropertyStyleBox, chatChannelButton),
                }),
                // chat filter button
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] {StyleClassChatFilterOptionButton}, null, null), new[]
                {
                    new StyleProperty(ContainerButton.StylePropertyStyleBox, chatFilterButton),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] {StyleClassChatFilterOptionButton}, null, new[] {ContainerButton.StylePseudoClassNormal}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorDefault),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] {StyleClassChatFilterOptionButton}, null, new[] {ContainerButton.StylePseudoClassHover}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorHovered),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] {StyleClassChatFilterOptionButton}, null, new[] {ContainerButton.StylePseudoClassPressed}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorPressed),
                }),
                new StyleRule(new SelectorElement(typeof(ContainerButton), new[] {StyleClassChatFilterOptionButton}, null, new[] {ContainerButton.StylePseudoClassDisabled}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorDisabled),
                }),

                // OptionButton
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, null), new[]
                {
                    new StyleProperty(ContainerButton.StylePropertyStyleBox, BaseButton),
                }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] {ContainerButton.StylePseudoClassNormal}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorDefault),
                }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] {ContainerButton.StylePseudoClassHover}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorHovered),
                }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] {ContainerButton.StylePseudoClassPressed}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorPressed),
                }),
                new StyleRule(new SelectorElement(typeof(OptionButton), null, null, new[] {ContainerButton.StylePseudoClassDisabled}), new[]
                {
                    new StyleProperty(Control.StylePropertyModulateSelf, ButtonColorDisabled),
                }),

                new StyleRule(new SelectorElement(typeof(TextureRect), new[] {OptionButton.StyleClassOptionTriangle}, null, null), new[]
                {
                    new StyleProperty(TextureRect.StylePropertyTexture, textureInvertedTriangle),
                    //new StyleProperty(Control.StylePropertyModulateSelf, Color.FromHex("#FFFFFF")),
                }),

                new StyleRule(new SelectorElement(typeof(Label), new[] { OptionButton.StyleClassOptionButton }, null, null), new[]
                {
                    new StyleProperty(Label.StylePropertyAlignMode, Label.AlignMode.Center),
                }),

                new StyleRule(new SelectorElement(typeof(PanelContainer), new []{ ClassHighDivider}, null, null), new []
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = NanoGold, ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2}),
                }),

                Element<PanelContainer>().Class(ClassAngleRect)
                    .Prop(PanelContainer.StylePropertyPanel, BaseAngleRect)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#25252A")),

                Element<PanelContainer>().Class(ClassLowDivider)
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                    {
                        BackgroundColor = Color.FromHex("#444"),
                        ContentMarginLeftOverride = 2,
                        ContentMarginBottomOverride = 2
                    }),

                // Window Headers
                Element<Label>().Class("FancyWindowTitle")
                    .Prop("font", boxFont13)
                    .Prop("font-color", NanoGold),

                Element<PanelContainer>().Class("WindowHeadingBackground")
                    .Prop("panel", new StyleBoxTexture(BaseButtonOpenLeft) { Padding = default })
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#1F1F23")),

                Element<PanelContainer>().Class("WindowHeadingBackgroundLight")
                    .Prop("panel", new StyleBoxTexture(BaseButtonOpenLeft) { Padding = default }),

                //The lengths you have to go through to change a background color smh
                Element<PanelContainer>().Class("PanelBackgroundBaseDark")
                    .Prop("panel", new StyleBoxTexture(BaseButtonOpenBoth) { Padding = default })
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#1F1F23")),

                Element<PanelContainer>().Class("PanelBackgroundLight")
                    .Prop("panel", new StyleBoxTexture(BaseButtonOpenBoth) { Padding = default })
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#2F2F3B")),

                // Window Footer
                Element<TextureRect>().Class("NTLogoDark")
                    .Prop(TextureRect.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/ntlogo.svg.png"))
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#757575")),

                Element<Label>().Class("WindowFooterText")
                    .Prop(Label.StylePropertyFont, notoSans8)
                    .Prop(Label.StylePropertyFontColor, Color.FromHex("#757575")),

                // X Texture button ---
                Element<TextureButton>().Class("CrossButtonRed")
                    .Prop(TextureButton.StylePropertyTexture, resCache.GetTexture("/Textures/Interface/Nano/cross.svg.png"))
                    .Prop(Control.StylePropertyModulateSelf, DangerousRedFore),

                Element<TextureButton>().Class("CrossButtonRed").Pseudo(TextureButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#7F3636")),

                Element<TextureButton>().Class("CrossButtonRed").Pseudo(TextureButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#753131")),
                // ---

                // The default look of paper in UIs. Pages can have components which override this
                Element<PanelContainer>().Class("PaperDefaultBorder")
                    .Prop(PanelContainer.StylePropertyPanel, paperBackground),
                Element<RichTextLabel>().Class("PaperWrittenText")
                    .Prop(Label.StylePropertyFont, notoSans12)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#111111")),

                Element<RichTextLabel>().Class("LabelSubText")
                    .Prop(Label.StylePropertyFont, notoSans10)
                    .Prop(Label.StylePropertyFontColor, Color.DarkGray),

                Element<LineEdit>().Class("PaperLineEdit")
                    .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),

                // Red Button ---
                Element<Button>().Class("ButtonColorRed")
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefaultRed),

                Element<Button>().Class("ButtonColorRed").Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorDefaultRed),

                Element<Button>().Class("ButtonColorRed").Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorHoveredRed),
                // ---

                // Green Button ---
                Element<Button>().Class("ButtonColorGreen")
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorGoodDefault),

                Element<Button>().Class("ButtonColorGreen").Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorGoodDefault),

                Element<Button>().Class("ButtonColorGreen").Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(Control.StylePropertyModulateSelf, ButtonColorGoodHovered),
                // ---

                Element<Label>().Class("StatusFieldTitle")
                    .Prop("font-color", NanoGold),

                Element<Label>().Class("Good")
                    .Prop("font-color", GoodGreenFore),

                Element<Label>().Class("Caution")
                    .Prop("font-color", ConcerningOrangeFore),

                Element<Label>().Class("Danger")
                    .Prop("font-color", DangerousRedFore),

                Element<Label>().Class("Disabled")
                    .Prop("font-color", DisabledFore),

                //PDA - Backgrounds
                Element<PanelContainer>().Class("PdaContentBackground")
                    .Prop(PanelContainer.StylePropertyPanel, BaseButtonOpenBoth)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#25252a")),

                Element<PanelContainer>().Class("PdaBackground")
                    .Prop(PanelContainer.StylePropertyPanel, BaseButtonOpenBoth)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#000000")),

                Element<PanelContainer>().Class("PdaBackgroundRect")
                    .Prop(PanelContainer.StylePropertyPanel, BaseAngleRect)
                    .Prop(Control.StylePropertyModulateSelf, Color.FromHex("#717059")),

                Element<PanelContainer>().Class("PdaBorderRect")
                    .Prop(PanelContainer.StylePropertyPanel, AngleBorderRect),

                Element<PanelContainer>().Class("BackgroundDark")
                    .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat(Color.FromHex("#25252A"))),

                //PDA - Buttons
                Element<PdaSettingsButton>().Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.NormalBgColor))
                    .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

                Element<PdaSettingsButton>().Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.HoverColor))
                    .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

                Element<PdaSettingsButton>().Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.PressedColor))
                    .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.EnabledFgColor)),

                Element<PdaSettingsButton>().Pseudo(ContainerButton.StylePseudoClassDisabled)
                    .Prop(PdaSettingsButton.StylePropertyBgColor, Color.FromHex(PdaSettingsButton.NormalBgColor))
                    .Prop(PdaSettingsButton.StylePropertyFgColor, Color.FromHex(PdaSettingsButton.DisabledFgColor)),

                Element<PdaProgramItem>().Pseudo(ContainerButton.StylePseudoClassNormal)
                    .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.NormalBgColor)),

                Element<PdaProgramItem>().Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.HoverColor)),

                Element<PdaProgramItem>().Pseudo(ContainerButton.StylePseudoClassPressed)
                    .Prop(PdaProgramItem.StylePropertyBgColor, Color.FromHex(PdaProgramItem.HoverColor)),

                //PDA - Text
                Element<Label>().Class("PdaContentFooterText")
                    .Prop(Label.StylePropertyFont, notoSans10)
                    .Prop(Label.StylePropertyFontColor, Color.FromHex("#757575")),

                Element<Label>().Class("PdaWindowFooterText")
                    .Prop(Label.StylePropertyFont, notoSans10)
                    .Prop(Label.StylePropertyFontColor, Color.FromHex("#333d3b")),

                // Fancy Tree
                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Class(TreeItem.StyleClassEvenRow)
                    .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                    {
                        BackgroundColor = FancyTreeEvenRowColor,
                    }),

                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Class(TreeItem.StyleClassOddRow)
                    .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                    {
                        BackgroundColor = FancyTreeOddRowColor,
                    }),

                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Class(TreeItem.StyleClassSelected)
                    .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                    {
                        BackgroundColor = FancyTreeSelectedRowColor,
                    }),

                Element<ContainerButton>().Identifier(TreeItem.StyleIdentifierTreeButton)
                    .Pseudo(ContainerButton.StylePseudoClassHover)
                    .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat
                    {
                        BackgroundColor = FancyTreeSelectedRowColor,
                    }),

            }).ToList());
        }
    }
}
