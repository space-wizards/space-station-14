using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Helper class for building reagent list rows.
/// </summary>
public static class ReagentListHelper
{
    private const int ColorIndicatorWidth = 4;
    private static readonly StyleBoxFlat EvenRowBackground = new() { BackgroundColor = Color.FromHex("#1B1B1E") };
    private static readonly StyleBoxFlat OddRowBackground = new() { BackgroundColor = Color.FromHex("#202025") };

    /// <summary>
    /// Populates a container with reagent rows.
    /// </summary>
    public static void PopulateReagentList(
        BoxContainer container,
        IEnumerable<ReagentQuantity> reagents,
        IPrototypeManager prototypeManager)
    {
        container.Children.Clear();

        var rowIndex = 0;
        foreach (var reagent in reagents)
        {
            prototypeManager.TryIndex(reagent.Reagent.Prototype, out var prototype);

            var name = prototype?.LocalizedName ?? Loc.GetString("reagent-list-helper-unknown-reagent");
            var color = prototype?.SubstanceColor ?? Color.White;

            container.Children.Add(BuildReagentRow(name, reagent.Quantity, color, rowIndex++));
        }
    }

    /// <summary>
    /// Builds a centered placeholder for an empty or unavailable reagent list.
    /// </summary>
    public static Control BuildPlaceholderRow(string text)
    {
        return new PanelContainer
        {
            VerticalExpand = true,
            Children =
            {
                new Label
                {
                    Text = text,
                    StyleClasses = { StyleClass.LabelWeak },
                    HorizontalAlignment = Control.HAlignment.Center,
                    Margin = new Thickness(4, 2)
                }
            }
        };
    }

    /// <summary>
    /// Builds a reagent list row with alternating background colors.
    /// </summary>
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
                    HorizontalExpand = true
                },
                new Label
                {
                    Text = Loc.GetString("reagent-list-helper-quantity-label-text", ("quantity", quantity)),
                    StyleClasses = { StyleClass.LabelWeak }
                },
                new PanelContainer
                {
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
