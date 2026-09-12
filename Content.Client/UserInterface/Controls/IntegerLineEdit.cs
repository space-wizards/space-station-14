using System.Text.RegularExpressions;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A line edit which accepts only integers as text input, and can clamp input to a lower or upper bound.
/// </summary>
/// <remarks>If <see cref="MaxValue"/> and <see cref="MinValue"/> are in conflict, MinValue takes priority.</remarks>
public sealed class IntegerLineEdit : LineEdit
{
    private static readonly Regex RegNumbers = new("^-*?[0-9]*$");

    /// <summary>
    /// A value entered that's larger than this will be rewritten to this.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MaxValue { get; set; }

    /// <summary>
    /// A value entered that's smaller than this will be rewritten to this.
    /// </summary>
    /// <remarks>This becomes annoying when set above 1.</remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public int? MinValue { get; set; }

    /// <returns>The integer value of the text.</returns>
    [ViewVariables(VVAccess.ReadOnly)]
    public int Value()
    {
        return int.TryParse(Text, out var i) ? i : 0;
    }

    public IntegerLineEdit()
    {
        IsValid += s => RegNumbers.IsMatch(s);

        OnTextChanged += ClampMax;
        OnTextChanged += ClampMin;
    }

    private void ClampMax(LineEditEventArgs _)
    {
        if (!AcceptableNonNumber() && MaxValue is {} max && Value() > max)
            Text = max.ToString();
    }

    private void ClampMin(LineEditEventArgs _)
    {
        if (!AcceptableNonNumber() && MinValue is {} min && Value() < min)
            Text = min.ToString();
    }

    /// <returns>True if the text is blank or only a negative sign.</returns>
    private bool AcceptableNonNumber()
    {
        return Text is "" or "-";
    }
}
