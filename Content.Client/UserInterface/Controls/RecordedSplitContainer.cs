using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     A split container that performs an action when the split resizing is finished.
/// </summary>
public sealed class RecordedSplitContainer : SplitContainer
{
    public Action<Vector2, Vector2>? OnSplitResizeFinish;

    public double? DesiredSplitCenter;

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        if (ResizeMode == SplitResizeMode.RespectChildrenMinSize
            && DesiredSplitCenter != null
            && !finalSize.Equals(Vector2.Zero))
        {
            SplitFraction = (float) DesiredSplitCenter.Value;

            if (!Size.Equals(Vector2.Zero))
            {
                DesiredSplitCenter = null;
            }
        }

        return base.ArrangeOverride(finalSize);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
        {
            return;
        }

        if (ChildCount != 2)
        {
            return;
        }

        var first = GetChild(0);
        var second = GetChild(1);

        OnSplitResizeFinish?.Invoke(first.Size, second.Size);
    }
}
