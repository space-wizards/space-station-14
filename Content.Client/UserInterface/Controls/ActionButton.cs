using Content.Shared.Actions.ActionTypes;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButton : ItemSlotControl
{
    public ActionButton(ActionType actionData)
    {
        ButtonTexturePath = "SlotBackground";
    }

    public void UpdateVisuals(ActionType actionData)
    {

    }

}
