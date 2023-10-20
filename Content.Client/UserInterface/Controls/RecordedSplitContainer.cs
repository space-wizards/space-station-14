using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
///     A split container that performs an action when the split resizing is finished.
/// </summary>
public sealed class RecordedSplitContainer : SplitContainer
{
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
}
