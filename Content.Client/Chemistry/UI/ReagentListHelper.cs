using Content.Client.Stylesheets;
using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Helper class for building reagent list rows.
/// </summary>
public static class ReagentListHelper
{
    private static readonly StyleBoxFlat EvenRowBackground = new() { BackgroundColor = Color.FromHex("#1B1B1E") };
    private static readonly StyleBoxFlat OddRowBackground = new() { BackgroundColor = Color.FromHex("#202025") };
    private const int ColorIndicatorWidth = 4;

    public static Control BuildPlaceholderRow(string text, bool fill = false)
    {
        return new PanelContainer
        {
            VerticalExpand = fill,
            HorizontalExpand = true,
            Children =
            {
                new Label
                {
                    Text = text,
                    HorizontalAlignment = Control.HAlignment.Center,
                    VerticalAlignment = fill ? Control.VAlignment.Center : Control.VAlignment.Top,
                    Margin = new Thickness(4, 2)
                }
            }
        };
    }

    /// <summary>
    /// Builds a UI row for displaying a reagent's name, quantity, and color indicator.
    /// Alternates row colors for better readability.
    /// </summary>
    /// <param name="name">The name of the reagent.</param>
    /// <param name="quantity">The quantity of the reagent.</param>
    /// <param name="reagentColor">The color associated with the reagent.</param>
    /// <param name="rowIndex">The row index for alternating colors.</param>
    /// <returns>A PanelContainer representing the reagent row.</returns>
    public static Control BuildReagentRow(string name, FixedPoint2 quantity, Color reagentColor, int rowIndex)
    {
        var background = rowIndex % 2 == 0
            ? EvenRowBackground
            : OddRowBackground;

        var rowContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4, 2),
            Children =
            {
                new Label
                {
                    Text = $"{name}: ",
                    ClipText = true,
                    HorizontalExpand = true,
                    VerticalAlignment = Control.VAlignment.Center
                },
                new Label
                {
                    Text = $"{quantity}u",
                    StyleClasses = { StyleClass.LabelWeak },
                    VerticalAlignment = Control.VAlignment.Center
                },
                new PanelContainer
                {
                    VerticalExpand = true,
                    MinWidth = ColorIndicatorWidth,
                    PanelOverride = new StyleBoxFlat { BackgroundColor = reagentColor },
                    Margin = new Thickness(4, 1, 0, 1)
                }
            }
        };

        return new PanelContainer
        {
            PanelOverride = background,
            Children = { rowContainer }
        };
    }
}
