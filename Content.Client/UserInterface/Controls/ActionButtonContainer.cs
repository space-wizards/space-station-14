using Content.Client.UserInterface.Controllers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButtonContainer : ItemSlotUIContainer<ActionButton>
{
    public ActionButtonContainer()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.Resolve<IUIControllerManager>().GetController<ActionUIController>().RegisterActionBar(this);
    }
}
