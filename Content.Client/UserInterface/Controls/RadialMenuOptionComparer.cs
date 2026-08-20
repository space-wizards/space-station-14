namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A class to compare radial menu options.
/// </summary>
public sealed class RadialMenuOptionComparer : IComparer<RadialMenuOptionBase>
{
    /// <summary/>
    /// Compares two RadialMenuOptionBase.
    /// Orders them by ascending order, then alphabetical tooltip.
    /// <seealso cref="IComparer{RadialMenuOptionBase}.Compare(RadialMenuOptionBase?, RadialMenuOptionBase?)"/>
    /// </summary>
    public int Compare(RadialMenuOptionBase? x, RadialMenuOptionBase? y)
    {
        if (ReferenceEquals(x, y))
            return 0;

        if (y == null)
            return -1;

        if (x == null)
            return 1;

        // First sort by order, then by tooltip.
        // Any non-null order value comes before null.
        if (y.Order != x.Order)
        {
            if (y?.Order is null)
                return -1;

            if (x?.Order is null)
                return 1;

            return x.Order < y.Order ? -1 : 1;
        }

        // Sort by tooltip: non-null order values come before null ones.
        if (y?.ToolTip is null)
            return -1;

        if (x?.ToolTip is null)
            return 1;

        return string.Compare(x.ToolTip, y.ToolTip, StringComparison.Ordinal);
    }
}
