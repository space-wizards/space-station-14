using System.Linq;
using System.Numerics;
using Content.Client.ContextMenu.UI;
using Content.Client.Examine;
using Content.Client.Resources;
using Content.Client.Stylesheets.Redux;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
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
        public const string StyleClassHandSlotHighlight = "HandSlotHighlight";
        public const string StyleClassChatSubPanel = "ChatSubPanel";
        public const string StyleClassTooltipPanel = "tooltipBox";
        public const string StyleClassTooltipAlertTitle = "tooltipAlertTitle";
        public const string StyleClassTooltipAlertDescription = "tooltipAlertDesc";
        public const string StyleClassTooltipAlertCooldown = "tooltipAlertCooldown";

        public const string StyleClassActionMenuItemRevoked = "actionMenuItemRevoked";
        public const string StyleClassChatChannelSelectorButton = "chatSelectorOptionButton";
        public const string StyleClassChatFilterOptionButton = "chatFilterOptionButton";
        public const string StyleClassStorageButton = "storageButton";

        // public const string StyleClassButtonBig = "ButtonBig";

        public const string StyleClassButtonHelp = "HelpButton";

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
        //NavMap
        public static readonly Color PointRed = Color.FromHex("#B02E26");
        public static readonly Color PointGreen = Color.FromHex("#38b026");
        public static readonly Color PointMagenta = Color.FromHex("#FF00FF");

        //Used by the APC and SMES menus
        public const string StyleClassPowerStateNone = "PowerStateNone";
        public const string StyleClassPowerStateLow = "PowerStateLow";
        public const string StyleClassPowerStateGood = "PowerStateGood";

        public static readonly Color ItemStatusNotHeldColor = Color.Gray;

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

            Stylesheet = new Stylesheet(BaseRules.Concat(new[]
            {
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

                new StyleRule(new SelectorElement(typeof(PanelContainer), new []{StyleClass.HighDivider}, null, null), new []
                {
                    new StyleProperty(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = NanoGold, ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2}),
                }),

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
            }).ToList());
        }
    }
}
