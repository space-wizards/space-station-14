using System.Text.RegularExpressions;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// A line edit which only accepts whole numbers as input.
/// </summary>
public sealed class IntegerLineEdit : LineEdit
{
    private static readonly Regex RegNumbers = new("^[0-9]*$");

    public IntegerLineEdit()
    {
        IsValid += s => RegNumbers.IsMatch(s);
    }

    public int Value()
    {
        return int.TryParse(Text, out var i) ? i : -1;
    }
}
