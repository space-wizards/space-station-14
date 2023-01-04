using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System.Diagnostics.CodeAnalysis;

namespace Content.Client.Guidebook.Richtext;

/// <summary>
/// A document, containing arbitrary text and UI elements.
/// </summary>
public sealed class Document : BoxContainer
{
    public Document()
    {
        Orientation = LayoutOrientation.Vertical;
    }

    public Document(IEnumerable<Control> controls) : this()
    {
        foreach (var control in controls)
        {
            AddChild(control);
        }
    }
}

public interface ITag
{
    public bool TryParseTag(List<string> args, Dictionary<string, string> param, [NotNullWhen(true)] out Control? control)
        => TryParseTag(args, param, out control, out _);
    public bool TryParseTag(List<string> args, Dictionary<string, string> param, [NotNullWhen(true)] out Control? control, out bool instant);
}
