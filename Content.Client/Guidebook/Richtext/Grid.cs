using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Guidebook.Richtext;

public sealed class Grid : GridContainer, IDocumentTag
{
    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
    }
}
