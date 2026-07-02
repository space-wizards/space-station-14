using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.Creatures.SpaceLeech.UI;

/// <summary>
///     Vertical blood gauge with a red fill level and tick-mark scale.
///     Drop this into a VerticalExpand container; it fills available height.
/// </summary>
public sealed class BloodVialControl : Control
{
    private static readonly Color BorderColor = Color.FromHex("#b62124");
    private static readonly Color BgColor     = Color.FromHex("#0a0607");
    private static readonly Color FillColor   = Color.FromHex("#d8303a");

    private readonly PanelContainer _outer;
    private readonly LayoutContainer _inner;
    private readonly PanelContainer _fill;
    private readonly Label _pctLabel;

    private float _fillFraction;

    public BloodVialControl()
    {
        VerticalExpand = true;
        HorizontalExpand = true;

        _outer = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = BgColor,
                BorderColor = BorderColor,
                BorderThickness = new Thickness(2),
            },
        };

        _inner = new LayoutContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        _fill = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = FillColor,
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

        _inner.AddChild(_fill);
        _inner.AddChild(_pctLabel);

        _outer.AddChild(_inner);
        AddChild(_outer);

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

        _pctLabel.Text = $"{(int)(_fillFraction * 100)}%";
    }
}
