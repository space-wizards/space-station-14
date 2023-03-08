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
            var secondMin = GetChild(1).DesiredSize;
            double minSize = Orientation == SplitOrientation.Vertical
                ? secondMin.Y
                : secondMin.X;
            double finalSizeComponent = Orientation == SplitOrientation.Vertical
                ? finalSize.Y
                : finalSize.X;

            var firstTotalFractional = (finalSizeComponent - minSize - SplitWidth - SplitEdgeSeparation) / finalSizeComponent;
            DesiredSplitCenter = Math.Round(DesiredSplitCenter.Value, 2, MidpointRounding.ToZero);

            // total space the split center takes up must fit the available space percentage given to the first child
            var canFirstFit = DesiredSplitCenter <= firstTotalFractional;

            if (DesiredSplitCenter > 1 || DesiredSplitCenter < 0 || !canFirstFit)
            {
                DesiredSplitCenter = Math.Round(firstTotalFractional, 2, MidpointRounding.ToZero);
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

        OnSplitResizeFinish?.Invoke(first.Size, second.Size);
    }
}
