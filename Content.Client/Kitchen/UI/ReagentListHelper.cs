using Content.Client.Stylesheets;
using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.Kitchen.UI;

/// <summary>
/// Helper class for building reagent list rows.
/// </summary>
public static class ReagentListHelper
{
    private static readonly StyleBoxFlat Background1 = new() { BackgroundColor = Color.FromHex("#1B1B1E") };
    private static readonly StyleBoxFlat Background2 = new() { BackgroundColor = Color.FromHex("#202025") };
    private const int ColorIndicatorWidth = 4;

    /// <summary>
    /// Builds a UI row for displaying a reagent's name, quantity, and color indicator.
    /// Alternates row colors for better readability.
    /// </summary>
    /// <param name="name">The name of the reagent.</param>
    /// <param name="quantity">The quantity of the reagent.</param>
    /// <param name="reagentColor">The color associated with the reagent.</param>
    /// <param name="rowCount">The row index for alternating colors.</param>
    /// <returns>A PanelContainer representing the reagent row.</returns>
    public static Control BuildReagentRow(string name, FixedPoint2 quantity, Color reagentColor, int rowCount)
    {
        var currentBackground = (rowCount % 2 == 0) ? Background1 : Background2;
        var colorToShow = reagentColor == default ? Color.White : reagentColor;

        var rowContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Children =
            {
                new Label 
                { 
                    Text = $"{name}: ",
                    VerticalAlignment = Control.VAlignment.Center 
                },
                new Label
                {
                    Text = $"{quantity}u",
                    StyleClasses = { StyleClass.LabelWeak },
                    VerticalAlignment = Control.VAlignment.Center
                },
                new Control { HorizontalExpand = true },
                new PanelContainer
                {
                    VerticalExpand = true,
                    MinWidth = ColorIndicatorWidth,
                    PanelOverride = new StyleBoxFlat { BackgroundColor = colorToShow },
                    Margin = new Thickness(0, 1)
                }
            }
        };

        return new PanelContainer
        {
            PanelOverride = currentBackground,
            Children = { rowContainer }
        };
    }
}
