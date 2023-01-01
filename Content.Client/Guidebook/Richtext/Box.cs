using System.Diagnostics.CodeAnalysis;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Guidebook.Richtext;

public sealed class Box : BoxContainer, ITag
{
    public bool TryParseTag(List<string> args, Dictionary<string, string> param, [NotNullWhen(true)] out Control? control, out bool instant)
    {
        HorizontalExpand = true;
        instant = false;
        control = this;

        if (args.Contains("Vertical"))
            Orientation = LayoutOrientation.Vertical;
        else
            Orientation = LayoutOrientation.Horizontal;

        if (args.Contains("HCenter"))
            HorizontalAlignment = HAlignment.Center;
        else if (args.Contains("HLeft"))
            HorizontalAlignment = HAlignment.Left;
        else if (args.Contains("HRight"))
            HorizontalAlignment = HAlignment.Right;


        return true;
    }
}
