using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     Vertical gauge with a colored fill level and centered percentage label.
///     Drop this into a VerticalExpand container; it fills available height.
/// </summary>
public sealed class VerticalGaugeControl : Control
{
    private readonly PanelContainer _fill;
    private readonly Label _pctLabel;

    private float _fillFraction;

    /// <summary>Whether the centered percentage label is shown.</summary>
    public bool ShowPercent
    {
        get => _pctLabel.Visible;
        set => _pctLabel.Visible = value;
    }

    public VerticalGaugeControl(Color fillColor, Color backgroundColor, Color borderColor)
    {
        VerticalExpand = true;
        HorizontalExpand = true;

        var outer = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = backgroundColor,
                BorderColor = borderColor,
                BorderThickness = new Thickness(2),
            },
        };

        var inner = new LayoutContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        _fill = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = fillColor,
            },
        };

        _pctLabel = new Label
        {
            FontColorOverride = new Color(1f, 1f, 1f, 0.6f),
            StyleClasses = { "LabelSubText" },
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };

        LayoutContainer.SetAnchorPreset(_fill, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorPreset(_pctLabel, LayoutContainer.LayoutPreset.Wide);

        inner.AddChild(_fill);
        inner.AddChild(_pctLabel);

        outer.AddChild(inner);
        AddChild(outer);

        SetFill(0f);
    }

    /// <summary>Update the fill level. <paramref name="fraction"/> is 0–1.</summary>
    public void SetFill(float fraction)
    {
        _fillFraction = Math.Clamp(fraction, 0f, 1f);
        LayoutContainer.SetAnchorTop(_fill, 1f - _fillFraction);
        LayoutContainer.SetAnchorBottom(_fill, 1f);
        LayoutContainer.SetAnchorLeft(_fill, 0f);
        LayoutContainer.SetAnchorRight(_fill, 1f);

        _pctLabel.Text = Loc.GetString("vertical-gauge-percent", ("percent", (int) (_fillFraction * 100)));
    }
}
