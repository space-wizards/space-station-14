using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Displays an integer using a configurable number of seven-segment digits.
/// </summary>
public sealed class SegmentDisplay : Control
{
    private const int MaxDigitCount = 10;
    private const int BlankDigit = -1;
    private const int MinusSign = -2;
    private const float DigitAspectRatio = 0.625f;

    private static readonly Color DefaultActiveColor = Color.FromHex("#94DACA");
    private static readonly Color DefaultInactiveColor = Color.FromHex("#46635C");
    private static readonly Color DefaultBackgroundColor = Color.FromHex("#1E272A");

    /// <summary>
    /// Bit positions for each segment:
    ///
    ///   _        0 (top)
    /// 5|_|1    6 (middle)
    /// 4|_|2    3 (bottom)
    /// </summary>
    private static readonly byte[] DigitPatterns =
    [
        0b0111111,
        0b0000110,
        0b1011011,
        0b1001111,
        0b1100110,
        0b1101101,
        0b1111101,
        0b0000111,
        0b1111111,
        0b1101111
    ];

    private int _value;
    private bool _showDecimalPoint;
    private bool _showLeadingZeroes = true;
    private int _decimalPosition = -1;
    private int _digitCount = 4;

    private byte?[] _bitmaskOverrides = new byte?[4];
    private byte? _globalBitmaskOverride;

    private int[] _cachedDigits = new int[4];

    private readonly Vector2[] _segmentPoints = new Vector2[6];

    [ViewVariables, PublicAPI]
    public Color ActiveColor { get; set; } = DefaultActiveColor;

    [ViewVariables, PublicAPI]
    public Color InactiveColor { get; set; } = DefaultInactiveColor;

    [ViewVariables, PublicAPI]
    public Color BackgroundColor { get; set; } = DefaultBackgroundColor;

    [ViewVariables, PublicAPI]
    public int Value
    {
        get => _value;
        set
        {
            var newValue = ClampValue(value);

            if (_value == newValue)
                return;

            _value = newValue;
            UpdateDigitsCache();
        }
    }

    /// <summary>
    /// Whether unused positions display zeroes instead of remaining blank.
    /// </summary>
    [ViewVariables, PublicAPI]
    public bool ShowLeadingZeroes
    {
        get => _showLeadingZeroes;
        set
        {
            if (_showLeadingZeroes == value)
                return;

            _showLeadingZeroes = value;
            UpdateDigitsCache();
        }
    }

    [ViewVariables, PublicAPI]
    public bool ShowDecimalPoint
    {
        get => _showDecimalPoint;
        set => _showDecimalPoint = value;
    }

    /// <summary>
    /// Position of the decimal point from the right, or -1 to hide it.
    /// </summary>
    [ViewVariables, PublicAPI]
    public int DecimalPosition
    {
        get => _decimalPosition;
        set => _decimalPosition = Math.Clamp(value, -1, _digitCount - 1);
    }

    /// <summary>
    /// Number of displayed digits, from 1 to 10.
    /// </summary>
    [ViewVariables, PublicAPI]
    public int DigitCount
    {
        get => _digitCount;
        set
        {
            var newValue = Math.Clamp(value, 1, MaxDigitCount);
            if (_digitCount == newValue)
                return;

            _digitCount = newValue;

            Array.Resize(ref _bitmaskOverrides, _digitCount);
            Array.Resize(ref _cachedDigits, _digitCount);

            _value = ClampValue(_value);
            UpdateDigitsCache();

            if (_decimalPosition >= _digitCount)
                _decimalPosition = _digitCount - 1;

            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Overrides one digit with a raw segment bitmask.
    /// </summary>
    /// <param name="position">Position from the right.</param>
    /// <param name="bitmask">Seven-segment bitmask.</param>
    [PublicAPI]
    public void SetBitmaskOverrideAtPosition(int position, byte bitmask)
    {
        if (position < 0 || position >= _digitCount)
            return;

        _bitmaskOverrides[_digitCount - 1 - position] = bitmask;
    }

    /// <summary>
    /// Clears the bitmask override at a position.
    /// </summary>
    /// <param name="position">Position from the right.</param>
    [PublicAPI]
    public void ClearBitmaskOverrideAtPosition(int position)
    {
        if (position < 0 || position >= _digitCount)
            return;

        _bitmaskOverrides[_digitCount - 1 - position] = null;
    }

    [PublicAPI]
    public void SetGlobalBitmaskOverride(byte bitmask)
    {
        _globalBitmaskOverride = bitmask;
    }

    [PublicAPI]
    public void ClearGlobalBitmaskOverride()
    {
        _globalBitmaskOverride = null;
    }

    [PublicAPI]
    public void ClearAllBitmaskOverrides()
    {
        Array.Fill(_bitmaskOverrides, null);
        _globalBitmaskOverride = null;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        handle.DrawRect(PixelSizeBox, BackgroundColor);

        var digitWidth = PixelWidth / _digitCount;
        var segmentHeight = PixelHeight * 0.9f;
        var segmentWidth = digitWidth * 0.7f;
        var spacing = digitWidth * 0.1f;
        var yOffset = PixelHeight * 0.05f;

        for (var i = 0; i < _digitCount; i++)
        {
            var x = i * digitWidth + spacing;

            var pattern = GetPattern(i);
            DrawSevenSegmentPattern(handle, pattern, x, yOffset, segmentWidth, segmentHeight);

            if (!_showDecimalPoint || _decimalPosition != _digitCount - 1 - i)
                continue;

            var dpSize = segmentHeight * 0.08f;
            var dpX = x + segmentWidth + spacing * 0.5f;
            var dpY = yOffset + segmentHeight - dpSize;
            handle.DrawRect(new UIBox2(dpX, dpY, dpX + dpSize, dpY + dpSize), ActiveColor);
        }
    }

    private byte GetPattern(int position)
    {
        var digit = _cachedDigits[position];
        return _bitmaskOverrides[position]
            ?? _globalBitmaskOverride
            ?? digit switch
            {
                MinusSign => 0b1000000,
                >= 0 and <= 9 => DigitPatterns[digit],
                _ => 0
            };
    }

    private void DrawSevenSegmentPattern(DrawingHandleScreen handle, byte pattern, float x, float y, float width, float height)
    {
        var segmentThickness = height * 0.1f;
        var gap = segmentThickness * 0.01f;

        var verticalSegmentHeight = (height - 3 * segmentThickness - 4 * gap) / 2;

        var effectiveWidth = width * 1.1f;
        var horSegmentWidth = effectiveWidth - segmentThickness * 2;
        var horSegmentX = x + (width - effectiveWidth) / 2 + segmentThickness;

        var leftEdge = x + (width - effectiveWidth) / 2;
        var rightEdge = leftEdge + effectiveWidth - segmentThickness;

        // Extend horizontal segments so their beveled ends meet the vertical segments.
        var extension = segmentThickness * 0.5f;

        DrawSegment(handle, (pattern & 0b0000001) != 0, horSegmentX - extension / 2, y, horSegmentWidth + extension, segmentThickness, true);

        var topLeftY = y + segmentThickness + gap;
        DrawSegment(handle,
            (pattern & 0b0100000) != 0,
            leftEdge,
            topLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        DrawSegment(handle,
            (pattern & 0b0000010) != 0,
            rightEdge,
            topLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        var middleY = y + segmentThickness + verticalSegmentHeight + gap;
        DrawSegment(handle,
            (pattern & 0b1000000) != 0,
            horSegmentX - extension / 2,
            middleY,
            horSegmentWidth + extension,
            segmentThickness,
            true);

        var bottomLeftY = middleY + segmentThickness + gap;
        DrawSegment(handle,
            (pattern & 0b0010000) != 0,
            leftEdge,
            bottomLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

        DrawSegment(handle,
            (pattern & 0b0000100) != 0,
            rightEdge,
            bottomLeftY - extension / 2,
            segmentThickness,
            verticalSegmentHeight + extension,
            false);

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
            _segmentPoints[0] = new(x + endBevel, y);
            _segmentPoints[1] = new(x + width - endBevel, y);
            _segmentPoints[2] = new(x + width, y + height * 0.5f);
            _segmentPoints[3] = new(x + width - endBevel, y + height);
            _segmentPoints[4] = new(x + endBevel, y + height);
            _segmentPoints[5] = new(x, y + height * 0.5f);
        }
        else
        {
            var endBevel = width * 0.5f;
            _segmentPoints[0] = new(x + width * 0.5f, y);
            _segmentPoints[1] = new(x + width, y + endBevel);
            _segmentPoints[2] = new(x + width, y + height - endBevel);
            _segmentPoints[3] = new(x + width * 0.5f, y + height);
            _segmentPoints[4] = new(x, y + height - endBevel);
            _segmentPoints[5] = new(x, y + endBevel);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, _segmentPoints, color);
    }

    private void UpdateDigitsCache()
    {
        Array.Fill(_cachedDigits, BlankDigit);

        var value = Math.Abs((long) _value);
        var index = _digitCount - 1;
        do
        {
            _cachedDigits[index--] = (int) (value % 10);
            value /= 10;
        } while (value > 0 && index >= 0);

        if (_value < 0)
        {
            if (_showLeadingZeroes)
            {
                while (index > 0)
                {
                    _cachedDigits[index--] = 0;
                }
            }

            _cachedDigits[index] = MinusSign;
            return;
        }

        if (!_showLeadingZeroes)
            return;

        while (index >= 0)
        {
            _cachedDigits[index--] = 0;
        }
    }

    private int ClampValue(int value)
    {
        var maximum = GetMaximumValue(_digitCount);
        var minimum = -GetMaximumValue(_digitCount - 1);
        return Math.Clamp(value, minimum, maximum);
    }

    private static int GetMaximumValue(int digits)
    {
        if (digits <= 0)
            return 0;

        if (digits >= MaxDigitCount)
            return int.MaxValue;

        var maximum = 1;
        for (var i = 0; i < digits; i++)
        {
            maximum *= 10;
        }

        return maximum - 1;
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        var height = availableSize.Y;
        var width = height * _digitCount * DigitAspectRatio;

        if (width <= availableSize.X)
            return new Vector2(width, height);

        width = availableSize.X;
        height = width / (_digitCount * DigitAspectRatio);

        return new Vector2(width, height);
    }
}
