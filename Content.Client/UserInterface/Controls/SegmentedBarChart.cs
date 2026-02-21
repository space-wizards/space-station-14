using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Content.Client.UserInterface.Controls;


public sealed class SegmentedBarChart : Control
{
    private sealed class Entry
    {
        public float WidthFraction;  // This entry's width as a fraction of the chart's total width (between 0 and 1)
        public float TargetAmount;
        public string Uid; // This UID is used to track entries between frames, for animation.
        public string? Tooltip;
        public Color Color;
        public Label Label;

        public Entry(string uid, Label label)
        {
            Uid = uid;
            Label = label;
        }
    }

    public const string StyleClassClassicSplitBar = "ClassicSplitBar";

    public const string StylePropertyAnimated = "animated";
    public const string StylePropertyShowRuler = "showRuler";
    public const string StylePropertyShowBackground = "showBackground";

    public const string StylePropertyNotchColor = "notchColor";
    public const string StylePropertyBackgroundColor = "backgroundColor";

    public const string StylePropertyGap = "gap";
    public const string StylePropertyMinEntryWidth = "minEntryWidth";

    public const string StylePropertySmallNotchHeight = "smallNotchHeight";
    public const string StylePropertyMediumNotchHeight = "mediumNotchHeight";
    public const string StylePropertyBigNotchHeight = "bigNotchHeight";
    public const string StylePropertyMediumNotchInterval = "mediumNotchInterval";
    public const string StylePropertyBigNotchInterval = "bigNotchInterval";
    public const string StylePropertyMinSmallNotchScreenDistance = "minSmallNotchScreenDistance";

    /// <summary>
    /// When Gap is greater than zero, all segments are separated by empty space. Gap is measured in UI units.
    /// This is incompatible with ShowRuler. If ShowRuler is enabled, Gap is ignored.
    /// </summary>
    public float? Gap { get; set; } = null;

    /// <summary>
    /// The minimum width of a segment in UI units.
    /// </summary>
    public float? MinEntryWidth { get; set; } = null;

    /// <summary>
    /// How many units of "Amount" fit into this chart. For example, when Capacity is 50, an entry with an amount of 5
    /// will take up 10% of the chart. If this is -1, the capacity is flexible and equal to the sum of the entries.
    /// </summary>
    public float Capacity { get; set; } = -1;

    /// <summary>
    /// Whether this chart is animated.
    /// </summary>
    public bool? Animated { get; set; } = null;

    /// <summary>
    /// Whether the ruler is drawn. Check the cryo pod UI to see what the ruler looks like.
    /// The ruler can be further configured through the various "Notch" properties.
    /// </summary>
    public bool? ShowRuler { get; set; } = null;

    /// <summary>
    /// Whether a background will be drawn behind the chart. The background is a simple rectangle and does not have
    /// gaps when Gap is non-zero. The color of the background is determined by the style property "backgroundColor".
    /// </summary>
    public bool? ShowBackground { get; set; } = null;

    // Every `Notch` variable is related to the ruler.
    public int? MediumNotchInterval { get; set; } = null;
    public int? BigNotchInterval { get; set; } = null;

    /// <summary>
    /// For the cryo pod UI, when we have a very large beaker (e.g. a bluespace beaker) we might need to increase the
    /// distance between notches to prevent all the notches from turning into a solid rectangle. When the distance
    /// between notches is less than MinSmallNotchScreenDistance in UI units, the distance is increased by a factor of
    /// ten (repeated as often as necessary).
    /// </summary>
    public int? MinSmallNotchScreenDistance { get; set; } = null;

    public float? SmallNotchHeight { get; set; } = null;
    public float? MediumNotchHeight { get; set; } = null;
    public float? BigNotchHeight { get; set; } = null;

    // Most properties can either be provided by the stylesheet or overriden through a property.
    // These helper computed properties make the bulk of the code a little less ugly.
    private Color _backgroundColor => TryGetStyleProperty(StylePropertyBackgroundColor, out Color found) ? found : new Color(0.1f, 0.1f, 0.1f);
    private Color _notchColor => TryGetStyleProperty(StylePropertyNotchColor, out Color found) ? found : new Color(0.1f, 0.1f, 0.1f);
    private float _gap => GetOverrideableStyleProperty(Gap, StylePropertyGap, 0);
    private float _minEntryWidth => GetOverrideableStyleProperty(MinEntryWidth, StylePropertyMinEntryWidth, 0);
    private bool _animated => GetOverrideableStyleProperty(Animated, StylePropertyAnimated, false);
    private bool _showRuler => GetOverrideableStyleProperty(ShowRuler, StylePropertyShowRuler, false);
    private bool _showBackground => GetOverrideableStyleProperty(ShowBackground, StylePropertyShowBackground, false);
    private float _mediumNotchInterval => GetOverrideableStyleProperty(MediumNotchInterval, StylePropertyMediumNotchInterval, 5);
    private float _bigNotchInterval => GetOverrideableStyleProperty(BigNotchInterval, StylePropertyBigNotchInterval, 10);
    private float _minSmallNotchScreenDistance => GetOverrideableStyleProperty(MinSmallNotchScreenDistance, StylePropertyMinSmallNotchScreenDistance, 2);
    private float _smallNotchHeight => GetOverrideableStyleProperty(SmallNotchHeight, StylePropertySmallNotchHeight, 0.1f);
    private float _mediumNotchHeight => GetOverrideableStyleProperty(MediumNotchHeight, StylePropertyMediumNotchHeight, 0.25f);
    private float _bigNotchHeight => GetOverrideableStyleProperty(BigNotchHeight, StylePropertyBigNotchHeight, 1f);

    // We don't animate new entries until this control has had at least one update where its width was non-zero.
    private bool _hasHadNonZeroWidth = false;

    // This is used to keep the segments of the chart in the same order as the SetEntry calls.
    // For example: In update 1 we might get cryox, alox, bic (in that order), and in update 2 we get alox, cryox, bic.
    // To keep the order of the entries the same as the order of the SetEntry calls, we let the old cryox entry
    // disappear and create a new cryox entry behind the alox entry.
    private int _nextUpdateableEntry = 0;

    private readonly List<Entry> _entries = new();


    public SegmentedBarChart()
    {
        MouseFilter = MouseFilterMode.Pass;
        TooltipSupplier = SupplyTooltip;
    }

    /// <summary>
    /// Makes the current entries disappear (usually in an animated way, so it takes a while for them to be gone).
    /// An entry won't disappear if a SetEntry call (after the Clear) gives the entry a non-zero `amount`.
    /// </summary>
    public void Clear()
    {
        foreach (var entry in _entries)
        {
            entry.TargetAmount = 0;
        }

        _nextUpdateableEntry = 0;
    }

    /// <summary>
    /// Either adds a new entry to the chart if the UID doesn't appear yet, or updates the amount of an existing entry.
    /// </summary>
    public void SetEntry(
        string uid,
        float amount,
        Color color,
        string? text = null,
        Color? textColor = null,
        string? tooltip = null)
    {
        // If we can find an old entry we're allowed to update, update that one.
        if (TryFindUpdateableEntry(uid, out var index))
        {
            _entries[index].TargetAmount = amount;
            _entries[index].Color = color;
            _entries[index].Tooltip = tooltip;
            _entries[index].Label.Text = text;
            _nextUpdateableEntry = index + 1;
            return;
        }

        // Otherwise create a new entry.
        if (amount <= 0)
            return;

        // If no text color is provided, use either white or black depending on how dark the background is.
        textColor ??= (color.R + color.G + color.B < 1.5f ? Color.White : Color.Black);

        var childLabel = new Label
        {
            Text = text,
            ClipText = true,
            FontColorOverride = textColor,
            Margin = new Thickness(4, 0, 0, 0)
        };
        AddChild(childLabel);

        _entries.Insert(
            _nextUpdateableEntry,
            new Entry(uid, childLabel)
            {
                WidthFraction = 0,
                TargetAmount = amount,
                Tooltip = tooltip,
                Color = color,
                Label = childLabel
            }
        );

        _nextUpdateableEntry += 1;
    }

    private bool TryFindUpdateableEntry(string uid, out int index)
    {
        for (int i = _nextUpdateableEntry; i < _entries.Count; i++)
        {
            if (_entries[i].Uid == uid)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private IEnumerable<(Entry, float xMin, float xMax)> EntryRanges(float chartWidth)
    {
        var xStart = 0f;
        var gapWidth = (_entries.Count > 1
            ? GetTotalGapsWidthFraction(chartWidth) * chartWidth / (_entries.Count - 1)
            : 0);

        foreach (var entry in _entries)
        {
            var entryWidth = entry.WidthFraction * chartWidth;
            var xEnd = MathF.Min(xStart + entryWidth, chartWidth);

            yield return (entry, xStart, xEnd);

            xStart = xEnd + gapWidth;
        }
    }

    private bool TryFindEntry(float x, float chartWidth, [NotNullWhen(true)] out Entry? entry)
    {
        foreach (var (currentEntry, xMin, xMax) in EntryRanges(chartWidth))
        {
            if (x < xMin)
                break;
            if (x > xMax)
                continue;

            entry = currentEntry;
            return true;
        }

        entry = null;
        return false;
    }

    private float GetCapacity()
    {
        // Constant capacity.
        if (Capacity > 0)
            return Capacity;

        // Flexible capacity.
        var amountSum = _entries.Aggregate(0f, (sum, entry) => sum + entry.TargetAmount);
        return MathF.Max(0.001f, amountSum);  // Make sure it's not zero (it's often used as denominator)
    }

    private float GetTotalGapsWidthFraction(float chartWidth)
    {
        var showRuler = GetOverrideableStyleProperty(ShowRuler, StylePropertyShowRuler, true);
        if (showRuler)
            return 0;  // ShowRuler is incompatible with Gap.

        var gapsWidth = (_entries.Count - 1) * _gap;
        var gapsFraction = gapsWidth / MathF.Max(chartWidth, 1f);

        // We limit the gaps to cover max 25% of the chart, to make sure there's always space for entries no matter
        // how many entries you add.
        return MathF.Min(gapsFraction, 0.25f);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        UpdateEntries(Width, args.DeltaSeconds);
    }

    private void UpdateEntries(float chartWidth, float deltaSeconds)
    {
        // Tween the amounts to their target amounts.
        const float tweenInverseHalfLife = 8;  // Half life of tween is 1/n
        var hasChanged = false;
        var animated = _animated;

        // This next series of calculations is somewhat complicated. We're trying to calculate the desired width for
        // each entry, but there's a couple of complicating factors: Gap, MinEntryWidth and constant/flexible capacities
        //
        // The calculations are done in width fractions (i.e. a percentage of this Control's width) because it works
        // better for animations, especially when swapping out a 50u beaker for a 100u beaker.
        //
        // In the calculation the width of an entry is split into two parts:
        // The minWidthFraction is based on MinEntryWidth and is the same for all entries,
        // and the remainder of the space is divided into flexibleWidthFractions (or left as empty space).
        // Normally the great majority of space is taken up by flexibleWidthFractions.

        // The amount of entries we want to have after animations are complete (some entries may disappear).
        var targetEntryCount = 0;
        foreach (var entry in _entries)
        {
            if (entry.TargetAmount > 0)
                targetEntryCount += 1;
        }

        var isCapacityFlexible = (Capacity <= 0);
        var totalAmount = GetCapacity();

        // The width available for entries.
        var totalEntriesWidthFraction = 1 - GetTotalGapsWidthFraction(chartWidth);
        // The min width for all entries can't be wider than the available space per entry.
        var spacePerEntry = totalEntriesWidthFraction / MathF.Max(1, targetEntryCount);
        // Minimum width of an entry.
        var minWidthFraction = MathF.Min(spacePerEntry, _minEntryWidth / MathF.Max(1, chartWidth));
        // The amount of units that `minWidthFraction` covers.
        var minWidthAmount = minWidthFraction * totalAmount;

        // The width that can still be divided among flexible width fractions.
        var remainingWidthFraction = totalEntriesWidthFraction - minWidthFraction * targetEntryCount;
        // The amount of units that can be divided among flexible width fractions.
        var remainingAmount =
            (isCapacityFlexible
            ? _entries.Aggregate(0f, (sum, entry) => sum + MathF.Max(0, entry.TargetAmount - minWidthAmount))
            : totalAmount - minWidthAmount);

        foreach (var entry in _entries)
        {
            // Calculate the target width for this entry.
            var targetWidthFraction = 0f;

            if (entry.TargetAmount != 0)
            {
                var flexibleAmount = MathF.Max(0, entry.TargetAmount - minWidthAmount);
                var flexibleWidthFraction =
                    (remainingAmount != 0
                    ? (flexibleAmount / remainingAmount) * remainingWidthFraction
                    : 0);

                targetWidthFraction = minWidthFraction + flexibleWidthFraction;
            }

            if (entry.WidthFraction == targetWidthFraction)
                continue;

            // Move the entry's width towards its target width.
            hasChanged = true;

            if (animated && _hasHadNonZeroWidth)
            {
                // Tween with lerp abuse interpolation
                entry.WidthFraction = MathHelper.Lerp(
                    entry.WidthFraction,
                    targetWidthFraction,
                    MathHelper.Clamp01(tweenInverseHalfLife * deltaSeconds)
                );

                if (MathF.Abs(entry.WidthFraction - targetWidthFraction) < 0.0001f)
                    entry.WidthFraction = targetWidthFraction;
            }
            else
            {
                // Don't animate, just snap straight to the target.
                entry.WidthFraction = targetWidthFraction;
            }
        }

        _hasHadNonZeroWidth |= (chartWidth > 0);

        if (!hasChanged)
            return;

        InvalidateArrange();

        // Remove old entries whose animations have finished.
        foreach (var entry in _entries)
        {
            if (entry.WidthFraction == 0 && entry.TargetAmount == 0)
                RemoveChild(entry.Label);
        }

        _entries.RemoveAll(entry => entry.WidthFraction == 0 && entry.TargetAmount == 0);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        HideTooltip();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_showBackground)
            handle.DrawRect(PixelSizeBox, _backgroundColor);

        // Draw the entry backgrounds
        foreach (var (entry, xMinUI, xMaxUI) in EntryRanges(Width))
        {
            var xMin = UIScale * xMinUI;
            var xMax = UIScale * xMaxUI;
            if (xMin != xMax)
                handle.DrawRect(new(xMin, 0, xMax, PixelHeight), entry.Color);
        }

        // Draw the ruler
        if (_showRuler)
        {
            // These computed properties are used in the loop. Compute them only once.
            var bigNotchInterval = _bigNotchInterval;
            var mediumNotchInterval = _mediumNotchInterval;
            var bigNotchHeight = _bigNotchHeight;
            var mediumNotchHeight = _mediumNotchHeight;
            var smallNotchHeight = _smallNotchHeight;
            var notchColor = _notchColor;

            var capacity = GetCapacity();
            var unitWidth = PixelWidth / capacity;

            // This math ensures the distance between notches is not less than `MinSmallNotchScreenDistance`.
            // We make sure that `unitsPerNotch` is always a power of ten (normally 1, 10 or 100).
            var maxNotches = Width / _minSmallNotchScreenDistance;
            var exp = MathF.Floor(MathF.Log10(maxNotches / capacity));
            var unitsPerNotch = 1f / MathF.Min(1, MathF.Pow(10, exp));

            var notchCount = (int)MathF.Floor(capacity / unitsPerNotch);
            var notchDistance = unitWidth * unitsPerNotch;

            for (int i = 0; i <= notchCount; i++)
            {
                var x = i * notchDistance;
                var height = (i % bigNotchInterval    == 0 ? bigNotchHeight :
                              i % mediumNotchInterval == 0 ? mediumNotchHeight :
                                                             smallNotchHeight) * PixelHeight;
                var start = new Vector2(x, PixelHeight);
                var end = new Vector2(x, PixelHeight - height);
                handle.DrawLine(start, end, notchColor);
            }
        }
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        // Some features (Gap, MinEntryWidth) depend on the Control's Width. Once the Width is set and before the
        // first draw, make sure that the entries get an opportunity to update their width properly.
        if (!_hasHadNonZeroWidth && finalSize.X > 0)
            UpdateEntries(finalSize.X, 0);

        foreach (var (entry, xMin, xMax) in EntryRanges(finalSize.X))
        {
            entry.Label.Arrange(new((int)xMin, 0, (int)xMax, (int)finalSize.Y));
        }

        return finalSize;
    }

    private Control? SupplyTooltip(Control sender)
    {
        var globalMousePos = UserInterfaceManager.MousePositionScaled.Position;
        var mousePos = globalMousePos - GlobalPosition;

        if (!TryFindEntry(mousePos.X, Width, out var entry) || entry.Tooltip == null)
            return null;

        var msg = new FormattedMessage();
        msg.AddText(entry.Tooltip);

        var tooltip = new Tooltip();
        tooltip.SetMessage(msg);
        return tooltip;
    }

    private T GetOverrideableStyleProperty<T>(T? optionalOverride, string propertyName, T fallback) where T : struct
    {
        if (optionalOverride.HasValue)
            return optionalOverride.Value;
        if (TryGetStyleProperty<T>(propertyName, out var result))
            return result;
        return fallback;
    }
}
