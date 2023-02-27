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
            && DesiredSplitCenter != null)
        {
            var secondMin = GetChild(1).MinSize;
            var minSize = Orientation == SplitOrientation.Vertical
                ? secondMin.Y
                : secondMin.X;
            var finalSizeComponent = Orientation == SplitOrientation.Vertical
                ? finalSize.Y
                : finalSize.X;

            var secondMinFractional = minSize / finalSizeComponent;
            DesiredSplitCenter = Math.Round(DesiredSplitCenter.Value, 2, MidpointRounding.ToZero);

            // minimum size of second child must fit into the leftover percentage of DesiredSplitCenter,
            var canSecondFit = DesiredSplitCenter + secondMinFractional <= 1;

            if (DesiredSplitCenter > 1 || DesiredSplitCenter < 0 || !canSecondFit)
            {
                DesiredSplitCenter = 0.5;
            }

            // don't need anything more than two digits of precision for this
            var currentSplitFraction = Math.Round(SplitFraction, 2, MidpointRounding.ToZero);

            // brute force it
            if (currentSplitFraction != DesiredSplitCenter.Value)
            {
                SplitFraction = (float) DesiredSplitCenter.Value;
            }
            else
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

        OnSplitResizeFinish!(first.Size, second.Size);
    }
}
