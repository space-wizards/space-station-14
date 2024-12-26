using System.Numerics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

// This control is not part of engine because I quickly wrote it in 2 hours at 2 AM and don't want to deal with
// API stabilization and/or figuring out relation to GridContainer.
// Grid layout is a complicated problem and I don't want to commit another half-baked thing into the engine.
// It's probably sufficient for its use case (RichTextLabel tables for rules/guidebook).
// Despite that, it's still better comment the shit half of you write on a regular basis.
//
// EMO: thank you PJB i was going to kill myself.

/// <summary>
/// Displays children in a tabular grid. Unlike <see cref="GridContainer"/>,
/// properly handles layout constraints so putting word-wrapping <see cref="RichTextLabel"/> in it should work.
/// </summary>
/// <remarks>
/// All children are automatically laid out in <see cref="Columns"/> columns.
/// The first control is in the top left, laid out per row from there.
/// </remarks>
[Virtual]
public class TableContainer : Container
{
    private int _columns = 1;

    /// <summary>
    /// The absolute minimum width a column can be forced to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If a column *asks* for less width than this (small contents), it can still be smaller.
    /// But if it asks for more it cannot go below this width.
    /// </para>
    /// </remarks>
    public float MinForcedColumnWidth { get; set; } = 50;

    // Scratch space used while calculating layout, cached to avoid regular allocations during layout pass.
    private ColumnData[] _columnDataCache = [];
    private RowData[] _rowDataCache = [];

    /// <summary>
    /// How many columns should be displayed.
    /// </summary>
    public int Columns
    {
        get => _columns;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1, nameof(value));

            _columns = value;
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        ResetCachedArrays();

        // Do a first pass measuring all child controls as if they're given infinite space.
        // This gives us a maximum width the columns want, which we use to proportion them later.
        var columnIdx = 0;
        foreach (var child in Children)
        {
            ref var column = ref _columnDataCache[columnIdx];

            child.Measure(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            column.MaxWidth = Math.Max(column.MaxWidth, child.DesiredSize.X);

            columnIdx += 1;
            if (columnIdx == _columns)
                columnIdx = 0;
        }

        // Calculate Slack and MinWidth for all columns. Also calculate sums for all columns.
        var totalMinWidth = 0f;
        var totalMaxWidth = 0f;
        var totalSlack = 0f;

        for (var c = 0; c < _columns; c++)
        {
            ref var column = ref _columnDataCache[c];
            column.MinWidth = Math.Min(column.MaxWidth, MinForcedColumnWidth);
            column.Slack = column.MaxWidth - column.MinWidth;

            totalMinWidth += column.MinWidth;
            totalMaxWidth += column.MaxWidth;
            totalSlack += column.Slack;
        }

        if (totalMaxWidth <= availableSize.X)
        {
            // We want less horizontal space than we're given. Huh, that's convenient.
            // Just set assigned width to be however much they asked for.
            // We could probably skip the second measure pass in this scenario,
            // but that's just an optimization, so I don't care right now.
            //
            // There's probably a very clever way to make this behavior work with the else block of logic,
            // just by fiddling with the math.
            // I'm dumb, it's 4:30 AM. Yeah, I *started* at 2 AM.
            for (var c = 0; c < _columns; c++)
            {
                ref var column = ref _columnDataCache[c];

                column.AssignedWidth = column.MaxWidth;
            }
        }
        else
        {
            // We don't have enough horizontal space,
            // at least without causing *some* sort of word wrapping (assuming text contents).
            //
            // Assign horizontal space proportional to the wanted maximum size of the columns.
            var assignableWidth =  Math.Max(0, availableSize.X - totalMinWidth);
            for (var c = 0; c < _columns; c++)
            {
                ref var column = ref _columnDataCache[c];

                var slackRatio = column.Slack / totalSlack;
                column.AssignedWidth = column.MinWidth + slackRatio * assignableWidth;
            }
        }

        // Go over controls for a second measuring pass, this time giving them their assigned measure width.
        // This will give us a height to slot into per-row data.
        // We still measure assuming infinite vertical space.
        // This control can't properly handle being constrained on the Y axis.
        columnIdx = 0;
        var rowIdx = 0;
        foreach (var child in Children)
        {
            ref var column = ref _columnDataCache[columnIdx];
            ref var row = ref _rowDataCache[rowIdx];

            child.Measure(new Vector2(column.AssignedWidth, float.PositiveInfinity));
            row.MeasuredHeight = Math.Max(row.MeasuredHeight, child.DesiredSize.Y);

            columnIdx += 1;
            if (columnIdx == _columns)
            {
                columnIdx = 0;
                rowIdx += 1;
            }
        }

        // Sum up height of all rows to get final measured table height.
        var totalHeight = 0f;
        for (var r = 0; r < _rowDataCache.Length; r++)
        {
            ref var row = ref _rowDataCache[r];
            totalHeight += row.MeasuredHeight;
        }

        return new Vector2(Math.Min(availableSize.X, totalMaxWidth), totalHeight);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        // TODO: Expand to fit given vertical space.

        // Calculate MinWidth and Slack sums again from column data.
        // We could've cached these from measure but whatever.
        var totalMinWidth = 0f;
        var totalSlack = 0f;

        for (var c = 0; c < _columns; c++)
        {
            ref var column = ref _columnDataCache[c];
            totalMinWidth += column.MinWidth;
            totalSlack += column.Slack;
        }

        // Calculate new width based on final given size, also assign horizontal positions of all columns.
        var assignableWidth = Math.Max(0, finalSize.X - totalMinWidth);
        var xPos = 0f;
        for (var c = 0; c < _columns; c++)
        {
            ref var column = ref _columnDataCache[c];

            var slackRatio = column.Slack / totalSlack;
            column.ArrangedWidth = column.MinWidth + slackRatio * assignableWidth;
            column.ArrangedX = xPos;

            xPos += column.ArrangedWidth;
        }

        // Do actual arrangement row-by-row.
        var arrangeY = 0f;
        for (var r = 0; r < _rowDataCache.Length; r++)
        {
            ref var row = ref _rowDataCache[r];

            for (var c = 0; c < _columns; c++)
            {
                ref var column = ref _columnDataCache[c];
                var index = c + r * _columns;

                if (index >= ChildCount) // Quit early if we don't actually fill out the row.
                    break;
                var child = GetChild(c + r * _columns);

                child.Arrange(UIBox2.FromDimensions(column.ArrangedX, arrangeY, column.ArrangedWidth, row.MeasuredHeight));
            }

            arrangeY += row.MeasuredHeight;
        }

        return finalSize with { Y = arrangeY };
    }

    /// <summary>
    /// Ensure cached array space is allocated to correct size and is reset to a clean slate.
    /// </summary>
    private void ResetCachedArrays()
    {
        // 1-argument Array.Clear() is not currently available in sandbox (added in .NET 6).

        if (_columnDataCache.Length != _columns)
            _columnDataCache = new ColumnData[_columns];

        Array.Clear(_columnDataCache, 0, _columnDataCache.Length);

        var rowCount = ChildCount / _columns;
        if (ChildCount % _columns != 0)
            rowCount += 1;

        if (rowCount != _rowDataCache.Length)
            _rowDataCache = new RowData[rowCount];

        Array.Clear(_rowDataCache, 0, _rowDataCache.Length);
    }

    /// <summary>
    /// Per-column data used during layout.
    /// </summary>
    private struct ColumnData
    {
        // Measure data.

        /// <summary>
        /// The maximum width any control in this column wants, if given infinite space.
        /// Maximum of all controls on the column.
        /// </summary>
        public float MaxWidth;

        /// <summary>
        /// The minimum width this column may be given.
        /// This is either <see cref="MaxWidth"/> or <see cref="TableContainer.MinForcedColumnWidth"/>.
        /// </summary>
        public float MinWidth;

        /// <summary>
        /// Difference between max and min width; how much this column can expand from its minimum.
        /// </summary>
        public float Slack;

        /// <summary>
        /// How much horizontal space this column was assigned at measure time.
        /// </summary>
        public float AssignedWidth;

        // Arrange data.

        /// <summary>
        /// How much horizontal space this column was assigned at arrange time.
        /// </summary>
        public float ArrangedWidth;

        /// <summary>
        /// The horizontal position this column was assigned at arrange time.
        /// </summary>
        public float ArrangedX;
    }

    private struct RowData
    {
        // Measure data.

        /// <summary>
        /// How much height the tallest control on this row was measured at,
        /// measuring for infinite vertical space but assigned column width.
        /// </summary>
        public float MeasuredHeight;
    }
}
