using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Displays a vertically oriented list of striped rows with an optional empty-state message.
/// </summary>
public abstract class StripedDisplayControl : BoxContainer
{
    /// <summary>
    /// Gets or sets the message displayed when <see cref="ShowEmptyMessage"/> is called.
    /// </summary>
    public string? EmptyMessage { get; set; }

    private bool _showingEmpty;
    private int _rowCount;

    protected StripedDisplayControl()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
    }

    /// <summary>
    /// Removes all displayed rows and resets the striping state.
    /// </summary>
    public void ClearDisplay()
    {
        RemoveAllChildren();
        _showingEmpty = false;
        _rowCount = 0;
    }

    /// <summary>
    /// Removes all displayed rows and shows <see cref="EmptyMessage"/>, when configured.
    /// </summary>
    public void ShowEmptyMessage()
    {
        RemoveAllChildren();
        _showingEmpty = true;
        _rowCount = 0;
        if (EmptyMessage != null)
            AddChild(new Label { Text = EmptyMessage, StyleClasses = { StyleClass.LabelWeak } });
    }

    /// <summary>
    /// Adds a striped row with a name, value, marker, and optional trailing controls.
    /// </summary>
    /// <param name="name">The localized name displayed at the start of the row.</param>
    /// <param name="value">The formatted value displayed after the name.</param>
    /// <param name="markerColor">The marker color, or <see langword="null"/> for the default marker style.</param>
    /// <param name="trailingControls">Optional controls appended after the marker.</param>
    protected void AddRow(string name, string value, Color? markerColor, IEnumerable<Control>? trailingControls = null)
    {
        if (_showingEmpty)
        {
            RemoveAllChildren();
            _showingEmpty = false;
        }

        var rowStyle = _rowCount++ % 2 == 0 ? StyleClass.Panel : StyleClass.PanelDark;
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Children =
            {
                new Label { Text = $"{name}: " },
                new Label
                {
                    Text = value,
                    StyleClasses = { StyleClass.LabelWeak },
                },
                new Control { HorizontalExpand = true },
                CreateMarker(markerColor),
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
            StyleClasses = { rowStyle },
            Children = { row },
        });
    }

    private static PanelContainer CreateMarker(Color? color)
    {
        var marker = new PanelContainer
        {
            VerticalExpand = true,
            MinWidth = 4,
            Margin = new Thickness(0, 1),
        };

        if (color is { } markerColor)
            marker.PanelOverride = new StyleBoxFlat(markerColor);
        else
            marker.AddStyleClass(StyleClass.PanelDark);

        return marker;
    }
}
