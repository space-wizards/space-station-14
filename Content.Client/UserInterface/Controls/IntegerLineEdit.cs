using System.Text.RegularExpressions;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A line edit which accepts only integers as text input, and can clamp input to a lower or upper bound.
/// </summary>
public sealed class IntegerLineEdit : LineEdit
{
    private static readonly Regex RegNumbers = new("^-?[0-9]*$");
    private int? _min;
    private int? _max;

    /// <summary>
    /// When not null, text entered that's smaller than this will be rewritten to this.
    /// </summary>
    /// <remarks>Becomes annoying when set above 1.</remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MinValue
    {
        get => _min;
        set => SetMin(value);
    }

    /// <summary>
    /// When not null, text entered that's larger than this will be rewritten to this.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MaxValue
    {
        get => _max;
        set => SetMax(value);
    }

    public IntegerLineEdit()
    {
        IsValid += s => RegNumbers.IsMatch(s);

        OnTextChanged += Clamp;
    }

    /// <returns>The integer value of the text.</returns>
    [ViewVariables(VVAccess.ReadOnly)]
    public int Value()
    {
        return int.TryParse(Text, out var i) ? i : 0;
    }

    /// <summary>
    /// Sets the current minimum value.
    /// </summary>
    public void SetMin(int? value)
    {
        if (value == _min)
            return;

        if (value == null)
        {
            _min = null;
            return;
        }

        if (value > _max)
        {
            value = _max;
            Log.Warning($"Min value of { this } was set above max.");
        }

        _min = value;
    }

    /// <summary>
    /// Sets the current maximum value.
    /// </summary>
    public void SetMax(int? value)
    {
        if (value == _max)
            return;

        if (value == null)
        {
            _max = null;
            return;
        }

        if (value < _min)
        {
            value = _min;
            Log.Warning($"Max value of { this } was set below min.");
        }

        _max = value;
    }

    /// <summary>
    /// Sets the minimum and maximum values for clamping.
    /// </summary>
    public void SetBoth(int? minimum, int? maximum)
    {
        if (minimum is { } min && maximum is { } max && min > max)
        {
            Log.Warning($"Min value of { this } was set above max.");
            minimum = maximum;
        }

        _min = minimum;
        _max = maximum;
    }

    /// <summary>
    /// Clamps the value of the text between min and max.
    /// </summary>
    private void Clamp(LineEditEventArgs _)
    {
        if (Text is "" or "-")
            return;

        Text = Math.Clamp(Value(), _min ?? int.MinValue, _max ?? int.MaxValue).ToString();
    }
}
