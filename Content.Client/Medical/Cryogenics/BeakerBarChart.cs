using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Content.Client.Medical.Cryogenics;


public sealed class BeakerBarChart : Control
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

    public float Capacity = 50;

    public Color NotchColor = new(1, 1, 1, 0.25f);
    public Color BackgroundColor = new(0.1f, 0.1f, 0.1f);

    public int MediumNotchInterval = 5;
    public int BigNotchInterval = 10;

    // When we have a very large beaker (i.e. bluespace beaker) we might need to increase the distance between notches.
    // The distance between notches is increased by ScaleMultiplier when the distance between notches is less than
    // MinSmallNotchScreenDistance in UI units.
    public int MinSmallNotchScreenDistance = 2;
    public int ScaleMultiplier = 10;

    public float SmallNotchHeight = 0.1f;
    public float MediumNotchHeight = 0.25f;
    public float BigNotchHeight = 1f;

    // We don't animate new entries until this control has been drawn at least once.
    private bool _hasBeenDrawn = false;

    // This is used to keep the segments of the chart in the same order as the SetEntry calls.
    // For example: In update 1 we might get cryox, alox, bic (in that order), and in update 2 we get alox, cryox, bic.
    // To keep the order of the entries the same as the order of the SetEntry calls, we let the old cryox entry
    // disappear and create a new cryox entry behind the alox entry.
    private int _nextUpdateableEntry = 0;

    private readonly List<Entry> _entries = new();


    public BeakerBarChart()
    {
        MouseFilter = MouseFilterMode.Pass;
        TooltipSupplier = SupplyTooltip;
    }

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
        string label,
        float amount,
        Color color,
        Color? textColor = null,
        string? tooltip = null)
    {
        // If we can find an old entry we're allowed to update, update that one.
        if (TryFindUpdateableEntry(uid, out var index))
        {
            _entries[index].TargetAmount = amount;
            _entries[index].Tooltip = tooltip;
            _entries[index].Label.Text = label;
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
            Text = label,
            ClipText = true,
            FontColorOverride = textColor,
            Margin = new Thickness(4, 0, 0, 0)
        };
        AddChild(childLabel);

        _entries.Insert(
            _nextUpdateableEntry,
            new Entry(uid, childLabel)
            {
                WidthFraction = (_hasBeenDrawn ? 0 : amount / Capacity),
                TargetAmount = amount,
                Tooltip = tooltip,
                Color = color
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

    private IEnumerable<(Entry, float xMin, float xMax)> EntryRanges(float? pixelWidth = null)
    {
        float chartWidth = pixelWidth ?? PixelWidth;
        var xStart = 0f;

        foreach (var entry in _entries)
        {
            var entryWidth = entry.WidthFraction * chartWidth;
            var xEnd = MathF.Min(xStart + entryWidth, chartWidth);

            yield return (entry, xStart, xEnd);

            xStart = xEnd;
        }
    }

    private bool TryFindEntry(float x, [NotNullWhen(true)] out Entry? entry)
    {
        foreach (var (currentEntry, xMin, xMax) in EntryRanges())
        {
            if (xMin <= x && x < xMax)
            {
                entry = currentEntry;
                return true;
            }
        }

        entry = null;
        return false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        // Tween the amounts to their target amounts.
        const float tweenInverseHalfLife = 8;  // Half life of tween is 1/n
        var hasChanged = false;

        foreach (var entry in _entries)
        {
            var targetWidthFraction = entry.TargetAmount / Capacity;

            if (entry.WidthFraction == targetWidthFraction)
                continue;

            // Tween with lerp abuse interpolation
            entry.WidthFraction = MathHelper.Lerp(
                entry.WidthFraction,
                targetWidthFraction,
                MathHelper.Clamp01(tweenInverseHalfLife * args.DeltaSeconds)
            );
            hasChanged = true;

            if (MathF.Abs(entry.WidthFraction - targetWidthFraction) < 0.0001f)
                entry.WidthFraction = targetWidthFraction;
        }

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
        handle.DrawRect(PixelSizeBox, BackgroundColor);

        // Draw the entry backgrounds
        foreach (var (entry, xMin, xMax) in EntryRanges())
        {
            if (xMin != xMax)
                handle.DrawRect(new(xMin, 0, xMax, PixelHeight), entry.Color);
        }

        // Draw notches
        var unitWidth = PixelWidth / Capacity;
        var unitsPerNotch = 1;

        while (unitWidth < MinSmallNotchScreenDistance)
        {
            // This is here for 1000u bluespace beakers. If the distance between small notches is so small that it would
            // be very ugly, we reduce the amount of notches by ScaleMultiplier (currently a factor of 10).
            // (I could use an analytical algorithm here, but it would be more difficult to read with pretty much no
            //  performance benefit, since it loops zero times normally and one time for the bluespace beaker)
            unitWidth *= ScaleMultiplier;
            unitsPerNotch *= ScaleMultiplier;
        }

        for (int i = 0; i <= Capacity / unitsPerNotch; i++)
        {
            var x = i * unitWidth;
            var height = (i % BigNotchInterval    == 0 ? BigNotchHeight :
                          i % MediumNotchInterval == 0 ? MediumNotchHeight :
                                                         SmallNotchHeight) * PixelHeight;
            var start = new Vector2(x, PixelHeight);
            var end = new Vector2(x, PixelHeight - height);
            handle.DrawLine(start, end, NotchColor);
        }

        _hasBeenDrawn = true;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
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

        if (!TryFindEntry(mousePos.X, out var entry) || entry.Tooltip == null)
            return null;

        var msg = new FormattedMessage();
        msg.AddText(entry.Tooltip);

        var tooltip = new Tooltip();
        tooltip.SetMessage(msg);
        return tooltip;
    }
}
