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
        public float Amount;
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

    public float SmallNotchHeight = 0.1f;
    public float MediumNotchHeight = 0.25f;
    public float BigNotchHeight = 1f;

    // We don't animate new entries until this control has been drawn at least once.
    private bool _hasBeenDrawn = false;

    private readonly List<Entry> _entries = new();


    public BeakerBarChart()
    {
        TooltipSupplier = SupplyTooltip;
    }

    public void Clear()
    {
        foreach (var entry in _entries)
        {
            entry.TargetAmount = 0;
        }
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
        var found = _entries.Find(entry => entry.Uid == uid);
        if (found != null)
        {
            found.TargetAmount = amount;
            found.Tooltip = tooltip;
            found.Label.Text = label;
            return;
        }

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

        _entries.Add(new Entry(uid, childLabel)
        {
            Amount = (_hasBeenDrawn ? 0 : amount),
            TargetAmount = amount,
            Tooltip = tooltip,
            Color = color
        });
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        // Tween the amounts to their target amounts.
        bool hasChanged = false;

        foreach (var entry in _entries)
        {
            if (entry.Amount == entry.TargetAmount)
                continue;

            // Tween with lerp abuse interpolation
            entry.Amount = MathHelper.Lerp(entry.Amount, entry.TargetAmount, MathHelper.Clamp01(8 * args.DeltaSeconds));
            hasChanged = true;

            if (MathF.Abs(entry.Amount - entry.TargetAmount) < 0.001f)
                entry.Amount = entry.TargetAmount;
        }

        if (!hasChanged)
            return;

        InvalidateArrange();

        // Remove old entries whose animations have finished.
        foreach (var entry in _entries)
        {
            if (entry.Amount == 0 && entry.TargetAmount == 0)
                RemoveChild(entry.Label);
        }

        _entries.RemoveAll(entry => entry.Amount == 0 && entry.TargetAmount == 0);
    }

    private IEnumerable<(Entry, float xMin, float xMax)> EntryRanges(float? pixelWidth = null)
    {
        pixelWidth ??= PixelWidth;
        var unitWidth = pixelWidth.Value / Capacity;
        var xStart = 0f;

        foreach (var entry in _entries)
        {
            var xEnd = MathF.Min(xStart + entry.Amount * unitWidth, pixelWidth.Value);

            yield return (entry, xStart, xEnd);

            xStart = xEnd;
        }
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

        for (int i = 0; i <= Capacity; i++)
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

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        foreach (var (entry, xMin, xMax) in EntryRanges(finalSize.X))
        {
            entry.Label.ArrangePixel(new((int)xMin, 0, (int)xMax, (int)finalSize.Y));
        }

        return finalSize;
    }
}
