using Content.Client.Stylesheets;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Chemistry.UI;

/// <summary>
/// Displays a striped reagent list with reagent color markers.
/// </summary>
public sealed partial class ReagentDisplayControl : BoxContainer
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private int _rowCount;

    public ReagentDisplayControl()
    {
        IoCManager.InjectDependencies(this);
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
    }

    /// <summary>
    /// Removes all displayed reagent rows.
    /// </summary>
    public void ClearDisplay()
    {
        RemoveAllChildren();
        _rowCount = 0;
    }

    /// <summary>
    /// Adds a reagent row using the localized reagent name and color.
    /// </summary>
    /// <param name="reagent">The reagent to display.</param>
    /// <param name="quantity">The quantity of reagent to display.</param>
    /// <param name="trailingControls">Optional controls appended to the row.</param>
    public void AddReagent(ReagentId reagent, FixedPoint2 quantity, IEnumerable<Control>? trailingControls = null)
    {
        _prototypeManager.TryIndex(reagent.Prototype, out var prototype);
        var name = prototype?.LocalizedName ?? Loc.GetString("chem-master-window-unknown-reagent-text");
        AddRow(name, quantity, prototype?.SubstanceColor, trailingControls);
    }

    /// <summary>
    /// Adds a row for a non-reagent entity.
    /// </summary>
    /// <param name="name">The name to display.</param>
    /// <param name="quantity">The quantity to display.</param>
    /// <param name="trailingControls">Optional controls appended to the row.</param>
    public void AddEntity(string name, FixedPoint2 quantity, IEnumerable<Control>? trailingControls = null)
    {
        AddRow(name, quantity, null, trailingControls);
    }

    private void AddRow(string name, FixedPoint2 quantity, Color? markerColor, IEnumerable<Control>? trailingControls)
    {
        var rowColor = Color.FromHex(_rowCount++ % 2 == 0 ? "#202025" : "#1B1B1E");
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Children =
            {
                new Label { Text = $"{name}: " },
                new Label
                {
                    Text = $"{quantity}u",
                    StyleClasses = { StyleClass.LabelWeak },
                },
                new Control { HorizontalExpand = true },
                new PanelContainer
                {
                    VerticalExpand = true,
                    MinWidth = 4,
                    PanelOverride = new StyleBoxFlat(markerColor ?? rowColor),
                    Margin = new Thickness(0, 1),
                },
            },
        };

        if (trailingControls != null)
        {
            foreach (var control in trailingControls)
            {
                row.AddChild(control);
            }
        }

        AddChild(new PanelContainer
        {
            PanelOverride = new StyleBoxFlat(rowColor),
            Children = { row },
        });
    }
}
