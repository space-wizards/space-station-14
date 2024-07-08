namespace Content.Client.Stylesheets.Redux;

/**
 * A collection of public reusable style classes. These should be general purpose (Not specific to only one element or
 * Ui).
 *
 * It is named `StyleClass` as opposed to `StyleClasses` because `StyleClasses` is a field on `Control` so it made it
 * a pain to reference this class from a `Control`. (Weird name is worth typing `StyleClass.OpenBoth` vs.
 * `Stylesheets.Redux.Styleclasses.OpenBoth`)
 */
public static class StyleClass
{
    // These style classes affect more than one type of element
    public const string Positive = "positive";
    public const string Negative = "negative";

    public const string FontLarge = "font-large";
    public const string FontSmall = "font-small";
    public const string Italic = "italic";
    public const string Monospace = "monospace";

    /**
     * <returns>The style class that will apply `ModulateSelf` with the specified color</returns>
     * <example><code>StyleClass.GetColorClass(StyleClass.PrimaryColor, 0)</code></example>
     */
    public static string GetColorClass(string styleclass, uint index)
    {
        return $"{styleclass}-{index}";
    }

    public const string PrimaryColor = "color-primary";
    public const string SecondaryColor = "color-secondary";
    public const string PositiveColor = "color-positive";
    public const string NegativeColor = "color-negative";
    public const string HighlightColor = "color-highlight";


    public const string BorderedWindowPanel = "BorderedWindowPanel";
    public const string AlertWindowHeader = "windowHeaderAlert";
    public const string WindowContentsContainer = "WindowContentsContainer";

    public const string HighDivider = "HighDivider";
    public const string LowDivider = "LowDivider";

    public const string LabelHeading = "LabelHeading";
    public const string LabelHeadingBigger = "LabelHeadingBigger";
    public const string LabelSubText = "LabelSubText";
    public const string LabelKeyText = "LabelKeyText";
    public const string LabelWeak = "LabelWeak"; // replaces `StyleClassLabelSecondaryColor`

    public const string BackgroundPanel = "AngleRect";
    public const string BackgroundPanelOpenLeft = "BackgroundOpenLeft";
    public const string BackgroundPanelOpenRight = "BackgroundOpenRight";

    public const string PanelDark = "PanelDark";
    public const string PanelLight = "PanelLight";

    public const string ButtonOpenRight = "OpenRight";
    public const string ButtonOpenLeft = "OpenLeft";
    public const string ButtonOpenBoth = "OpenBoth";
    public const string ButtonSquare = "ButtonSquare";
    public const string ButtonSmall = "ButtonSmall";
    public const string ButtonBig = "ButtonBig";

    public const string CrossButtonRed = "CrossButtonRed";

    public const string StyleClassSliderRed = "Red";
    public const string StyleClassSliderGreen = "Green";
    public const string StyleClassSliderBlue = "Blue";
    public const string StyleClassSliderWhite = "White";
    public const string StyleClassItemStatus = "ItemStatus";
    public const string StyleClassItemStatusNotHeld = "ItemStatusNotHeld";
    public const string ChatPanel = "ChatPanel";
    public const string ChatLineEdit = "chatLineEdit";
    public const string ChatChannelSelectorButton = "chatSelectorOptionButton";
    public const string ChatFilterOptionButton = "chatFilterOptionButton";

    public const string TooltipPanel = "tooltipPanel";
    public const string TooltipTitle = "tooltipTitle";
    public const string TooltipDesc = "tooltipDesc";
}
