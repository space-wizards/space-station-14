using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

public sealed class SegmentDisplay : Control
{
    private static readonly Color ActiveColor = Color.FromHex("#94daca");
    private static readonly Color InactiveColor = Color.FromHex("#46635c");
    private static readonly Color BackgroundColor = Color.FromHex("#1e272a");

    /// <summary>
    /// 7-segment display patterns for digits 0-9.
    /// Each byte is a bitmask where each bit represents a segment:
    ///
    /// Segment layout (bit positions):
    ///   _        0 (top)
    /// 5|_|1    6 (middle)
    /// 4|_|2    3 (bottom)
    ///
    /// So 0b0111111 for '0' means light up all segments except the middle bar.
    /// </summary>
    private static readonly byte[] DigitPatterns = new byte[]
    {
        0b0111111, // 0
        0b0000110, // 1
        0b1011011, // 2
        0b1001111, // 3
        0b1100110, // 4
        0b1101101, // 5
        0b1111101, // 6
        0b0000111, // 7
        0b1111111, // 8
        0b1101111  // 9
    };

    private int _value;
    private bool _showDecimalPoint;
    private int _decimalPosition = -1; // -1 means no decimal point
    private int _digitCount = 4;

    private byte?[] _bitmaskOverrides = new byte?[4];
    private byte? _globalBitmaskOverride;

    private int[] _cachedDigits = new int[4];
    private bool _digitsCacheDirty = true;

    private readonly Vector2[] _segmentPoints = new Vector2[6];

    [ViewVariables, PublicAPI]
    public int Value
    {
        get => _value;
        set
        {
            // Calculate max value based on digit count
            var maxValue = (int) Math.Pow(10, _digitCount) - 1;
            var newValue = Math.Clamp(value, 0, maxValue);

            if (_value == newValue)
                return;

            _value = newValue;
            _digitsCacheDirty = true;
            InvalidateMeasure();
        }
    }

    [ViewVariables, PublicAPI]
    public bool ShowDecimalPoint
    {
        get => _showDecimalPoint;
        set
        {
            if (_showDecimalPoint == value)
                return;

            _showDecimalPoint = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Position of decimal point from right (0 to DigitCount-1).
    /// E.g. value=123 with DecimalPosition=1 shows "12.3"
    /// </summary>
    [ViewVariables, PublicAPI]
    public int DecimalPosition
    {
        get => _decimalPosition;
        set
        {
            if (_decimalPosition == value)
                return;

            _decimalPosition = Math.Clamp(value, -1, _digitCount - 1);
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Number of digits to display
    /// </summary>
    [ViewVariables, PublicAPI]
    public int DigitCount
    {
        get => _digitCount;
        set
        {
            if (_digitCount == value || value < 1)
                return;

            _digitCount = value;

            Array.Resize(ref _bitmaskOverrides, _digitCount);
            Array.Resize(ref _cachedDigits, _digitCount);
            _digitsCacheDirty = true;

            // Reclamp the value to fit within the new digit count
            Value = _value;

            if (_decimalPosition >= _digitCount)
                _decimalPosition = _digitCount - 1;

            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Set a custom bitmask pattern for a specific position.
    /// </summary>
    /// <param name="position">Position from right (0 to DigitCount-1)</param>
    /// <param name="bitmask">7-segment bitmask pattern</param>
    [PublicAPI]
    public void SetBitmaskOverrideAtPosition(int position, byte bitmask)
    {
        if (position < 0 || position >= _digitCount)
            return;

        _bitmaskOverrides[_digitCount - 1 - position] = bitmask;
        InvalidateMeasure();
    }

    /// <summary>
    /// Clear a bitmask override at a specific position.
    /// </summary>
    /// <param name="position">Position from right (0 to DigitCount-1)</param>
    [PublicAPI]
    public void ClearBitmaskOverrideAtPosition(int position)
    {
        if (position < 0 || position >= _digitCount)
            return;

        _bitmaskOverrides[_digitCount - 1 - position] = null;
        InvalidateMeasure();
    }

    /// <summary>
    /// Set the same bitmask pattern for all digit positions
    /// </summary>
    /// <param name="bitmask">7-segment bitmask pattern</param>
    [PublicAPI]
    public void SetGlobalBitmaskOverride(byte bitmask)
    {
        _globalBitmaskOverride = bitmask;
        InvalidateMeasure();
    }

    /// <summary>
    /// Clear the global bitmask override
    /// </summary>
    [PublicAPI]
    public void ClearGlobalBitmaskOverride()
    {
        _globalBitmaskOverride = null;
        InvalidateMeasure();
    }

    /// <summary>
    /// Clear all bitmask overrides (both position-specific and global)
    /// </summary>
    [PublicAPI]
    public void ClearAllBitmaskOverrides()
    {
        for (var i = 0; i < _bitmaskOverrides.Length; i++)
        {
            _bitmaskOverrides[i] = null;
        }

        _globalBitmaskOverride = null;
        InvalidateMeasure();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        handle.DrawRect(PixelSizeBox, BackgroundColor);

        // Update cached digits if needed
        if (_digitsCacheDirty)
        {
            UpdateDigitsCache();
            _digitsCacheDirty = false;
        }

        var digitWidth = PixelWidth / _digitCount;
        var segmentHeight = PixelHeight * 0.9f;
        var segmentWidth = digitWidth * 0.7f;
        var spacing = digitWidth * 0.1f;
        var yOffset = PixelHeight * 0.05f;

        // Draw each digit
        for (var i = 0; i < _digitCount; i++)
        {
            var x = i * digitWidth + spacing;

            // Determine which pattern to use (priority: position override > global override > digit value)
            byte pattern;
            if (_bitmaskOverrides[i] != null)
            {
                pattern = _bitmaskOverrides[i]!.Value;
            }
            else if (_globalBitmaskOverride != null)
            {
                pattern = _globalBitmaskOverride.Value;
            }
            else
            {
                var digit = _cachedDigits[i];
                pattern = digit is >= 0 and <= 9
                    ? DigitPatterns[digit]
                    : (byte)0;
            }

            DrawSevenSegmentPattern(handle, pattern, x, yOffset, segmentWidth, segmentHeight);

            // Draw decimal point if enabled for this position
            if (_showDecimalPoint && _decimalPosition == (_digitCount - 1 - i))
            {
                var dpSize = segmentHeight * 0.08f;
                var dpX = x + segmentWidth + spacing * 0.5f;
                var dpY = yOffset + segmentHeight - dpSize;
                handle.DrawRect(new UIBox2(dpX, dpY, dpX + dpSize, dpY + dpSize), ActiveColor);
            }
        }
    }

    /// <summary>
    /// Draws a single 7-segment pattern using the bitmask.
    /// Segments are drawn as hexagons for that nice beveled look.
    /// </summary>
    private void DrawSevenSegmentPattern(DrawingHandleScreen handle, byte pattern, float x, float y, float width, float height)
    {
        var segmentThickness = height * 0.1f;
        var gap = segmentThickness * 0.01f;

        // Math time! Figure out vertical segment height based on total height
        var verticalSegmentHeight = (height - 3 * segmentThickness - 4 * gap) / 2;

        // Horizontal segment dimensions
        var effectiveWidth = width * 1.1f;
        var horSegmentWidth = effectiveWidth - segmentThickness * 2;
        var horSegmentX = x + (width - effectiveWidth) / 2 + segmentThickness;

        // Vertical segment positions
        var leftEdge = x + (width - effectiveWidth) / 2;
        var rightEdge = leftEdge + effectiveWidth - segmentThickness;

        // This is an arbitrary number used for extension because the bevels will cut off the corners
        var extension = segmentThickness * 0.5f;

        // Top horizontal segment
        DrawSegment(handle, (pattern & 0b0000001) != 0, horSegmentX - extension/2, y, horSegmentWidth + extension, segmentThickness, true);

        // Top left vertical segment
        var topLeftY = y + segmentThickness + gap;
        DrawSegment(handle,
            (pattern & 0b0100000) != 0,
            leftEdge,
            topLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        // Top right vertical segment
        DrawSegment(handle,
            (pattern & 0b0000010) != 0,
            rightEdge,
            topLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        // Middle horizontal segment
        var middleY = y + segmentThickness + verticalSegmentHeight + gap;
        DrawSegment(handle,
            (pattern & 0b1000000) != 0,
            horSegmentX - extension / 2,
            middleY,
            horSegmentWidth + extension,
            segmentThickness,
            true);

        // Bottom left vertical segment
        var bottomLeftY = middleY + segmentThickness + gap;
        DrawSegment(handle,
            (pattern & 0b0010000) != 0,
            leftEdge,
            bottomLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        // Bottom right vertical segment
        DrawSegment(handle,
            (pattern & 0b0000100) != 0,
            rightEdge,
            bottomLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        // Bottom horizontal segment
        var bottomY = bottomLeftY + verticalSegmentHeight + gap;
        DrawSegment(handle,
            (pattern & 0b0001000) != 0,
            horSegmentX - extension / 2,
            bottomY,
            horSegmentWidth + extension,
            segmentThickness,
            true);
    }

    private void DrawSegment(DrawingHandleScreen handle, bool active, float x, float y, float width, float height, bool horizontal)
    {
        var color = active ? ActiveColor : InactiveColor;

        if (horizontal)
        {
            var endBevel = height * 0.5f;
            _segmentPoints[0] = new(x + endBevel, y); // Top left
            _segmentPoints[1] = new(x + width - endBevel, y); // Top right
            _segmentPoints[2] = new(x + width, y + height * 0.5f); // Mid right point
            _segmentPoints[3] = new(x + width - endBevel, y + height); // Bottom right
            _segmentPoints[4] = new(x + endBevel, y + height); // Bottom left
            _segmentPoints[5] = new(x, y + height * 0.5f); // Mid left point

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _segmentPoints, color);
        }
        else
        {
            var endBevel = width * 0.5f;
            _segmentPoints[0] = new(x + width * 0.5f, y); // Top mid point
            _segmentPoints[1] = new(x + width, y + endBevel); // Top right
            _segmentPoints[2] = new(x + width, y + height - endBevel); // Bottom right
            _segmentPoints[3] = new(x + width * 0.5f, y + height); // Bottom mid point
            _segmentPoints[4] = new(x, y + height - endBevel); // Bottom left
            _segmentPoints[5] = new(x, y + endBevel); // Top left

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _segmentPoints, color);
        }
    }

    private void UpdateDigitsCache()
    {
        var value = _value;
        for (var i = 0; i < _digitCount; i++)
        {
            _cachedDigits[_digitCount - 1 - i] = value % 10;
            value /= 10;
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var height = availableSize.Y;
        var width = height * (_digitCount * 0.625f); // Each digit is approximately 0.625 times height

        if (width > availableSize.X)
        {
            width = availableSize.X;
            height = width / (_digitCount * 0.625f);
        }

        return new Vector2(width, height);
    }
}
