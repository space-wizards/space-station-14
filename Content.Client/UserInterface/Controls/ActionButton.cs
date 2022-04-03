using Content.Shared.Actions.ActionTypes;

namespace Content.Client.UserInterface.Controls;

public sealed class ActionButton : ItemSlotControl
{
    public ActionType ActionData { get; set; }
    public ActionButton(ActionType actionData)
    {
        ButtonTexturePath = "SlotBackground";
        ActionData = actionData;
    }

    public void UpdateVisuals()
    {
    }

}
