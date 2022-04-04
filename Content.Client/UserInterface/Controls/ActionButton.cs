using Content.Client.HUD;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;



public sealed class DummyButton : Control
{
}


public sealed class ActionButton : SlotControl
{
    public ActionType ActionData { get; set; }
    public ActionButton(ActionType actionData,int page = 0)
    {
        ButtonTexturePath = "SlotBackground";
        ActionData = actionData;
    }

    public void UpdateVisuals()
    {
    }

}
