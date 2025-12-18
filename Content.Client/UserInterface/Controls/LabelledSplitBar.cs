using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
namespace Content.Client.UserInterface.Controls;

public sealed class LabelledSplitBar : BoxContainer
{
    public Thickness LabelMargin = new Thickness(2);
    public float? MinBarWidth;

    public LabelledSplitBar()
    {
        MouseFilter = MouseFilterMode.Stop;
        HorizontalExpand = true;
        Orientation = LayoutOrientation.Horizontal;
        MinSize = new Vector2(60, 20);
        SeparationOverride = 0;
    }

    public void Clear()
    {
        RemoveAllChildren();
    }

    public void AddEntry(string label, float amount, Color color, Color? textColor = null, string? tooltip = null)
    {
        if (amount <= 0)
            return;

        textColor ??= (color.R + color.G + color.B < 1.5f ? Color.White : Color.Black);

        var panel = new PanelContainer
        {
            ToolTip = tooltip,
            HorizontalExpand = true,
            VerticalExpand = true,
            SizeFlagsStretchRatio = amount,
            MouseFilter = MouseFilterMode.Stop,
            RectClipContent = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color
            }
        };

        if (MinBarWidth != null)
            panel.MinWidth = MinBarWidth.Value;

        var text = new Label
        {
            Text = label,
            Margin = LabelMargin,
            FontColorOverride = textColor,
            ClipText = true
        };

        panel.AddChild(text);
        AddChild(panel);
    }

    public void AddEmptySpace(float amount)
    {
        if (amount <= 0)
            return;

        AddChild(new Control
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            SizeFlagsStretchRatio = amount,
            MouseFilter = MouseFilterMode.Stop
        });
    }
}
