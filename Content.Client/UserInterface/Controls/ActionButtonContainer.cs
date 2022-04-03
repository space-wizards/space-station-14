using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButtonContainer : ItemSlotUIContainer<ActionButton>
{
    public ActionButtonContainer()
    {
        Orientation = LayoutOrientation.Vertical;
    }
}
