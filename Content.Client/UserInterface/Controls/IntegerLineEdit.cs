using System.Text.RegularExpressions;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A line edit which accepts only integers as text input, and can clamp input to a lower or upper bound.
/// </summary>
public sealed class IntegerLineEdit : LineEdit
{
    private static readonly Regex RegNumbers = new("^-?[0-9]*$");

    public IntegerLineEdit()
    {
        IsValid += s => RegNumbers.IsMatch(s);

        OnTextChanged += Clamp;
    }

    /// <summary>
    /// When not null, text entered that's smaller than this will be rewritten to this.
    /// </summary>
    /// <remarks>Becomes annoying when set above 1.</remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MinValue
    {
        get;
        set
        {
            if (value == field)
                return;

            if (value == null)
            {
                field = null;
                return;
            }

            if (value > MaxValue)
            {
                value = MaxValue;
                Log.Warning($"Min value of {this} was set above max.");
            }

            field = value;
        }
    }

    /// <summary>
    /// When not null, text entered that's larger than this will be rewritten to this.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MaxValue
    {
        get;
        set
        {
            if (value == field)
                return;

            if (value == null)
            {
                field = null;
                return;
            }

            if (value < MinValue)
            {
                value = MinValue;
                Log.Warning($"Max value of {this} was set below min.");
            }

            field = value;
        }
    }

    /// <returns>The integer value of the text.</returns>
    [ViewVariables(VVAccess.ReadOnly)]
    public int Value()
    {
        return int.TryParse(Text, out var i) ? i : 0;
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

        MinValue = minimum;
        MaxValue = maximum;
    }

    /// <summary>
    /// Clamps the value of the text between min and max.
    /// </summary>
    private void Clamp(LineEditEventArgs _)
    {
        if (Text is "" or "-")
            return;

        Text = Math.Clamp(
            Value(),
            MinValue ?? int.MinValue,
            MaxValue ?? int.MaxValue
        ).ToString();
    }
}
