using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls.FancyTree;

/// <summary>
///     This is a basic control that draws the lines connecting parents & children in a tree.
/// </summary>
/// <remarks>
///     Ideally this would just be a draw method in <see cref="TreeItem"/>, but sadly the draw override gets called BEFORE children are drawn.
/// </remarks>
public sealed class TreeLine : Control
{
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // This is basically just a shitty hack to call Draw() after children get drawn.
        if (Parent is not TreeItem parent)
            return;

        if (!parent.Expanded || !parent.Tree.DrawLines || parent.Body.ChildCount == 0)
            return;

        var width = Math.Max(1, (int) (parent.Tree.LineWidth * UIScale));
        var w1 = width / 2;
        var w2 = width - w1;

        var global = parent.GlobalPixelPosition;

        var iconPos = parent.Icon.GlobalPixelPosition - global;
        var iconSize = parent.Icon.PixelSize;
        var x = iconPos.X + iconSize.X / 2;
        DebugTools.Assert(parent.Icon.Visible);

        var buttonPos = parent.Button.GlobalPixelPosition - global;
        var buttonSize = parent.Button.PixelSize;
        var y1 = buttonPos.Y + buttonSize.Y;

        var lastItem = (TreeItem) parent.Body.GetChild(parent.Body.ChildCount - 1);

        var childPos = lastItem.Button.GlobalPixelPosition - global;
        var y2 = childPos.Y + lastItem.Button.PixelSize.Y / 2;

        // Vertical line
        var rect = new UIBox2i((x - w1, y1), (x + w2, y2));
        handle.DrawRect(rect, parent.Tree.LineColor);

        // Horizontal lines
        var dx = Math.Max(1, (int) (FancyTree.Indentation * UIScale / 2));
        foreach (var child in parent.Body.Children)
        {
            var item = (TreeItem) child;
            var pos = item.Button.GlobalPixelPosition - global;
            var y = pos.Y + item.Button.PixelSize.Y / 2;
            rect = new UIBox2i((x - w1, y - w1), (x + dx, y + w2));
            handle.DrawRect(rect, parent.Tree.LineColor);
        }
    }
}
