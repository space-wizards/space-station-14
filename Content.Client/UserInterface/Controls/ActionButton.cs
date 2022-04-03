using Content.Client.HUD;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;



public sealed class DummyButton : Control, IThemeableUI
{
    public DummyButton()
    {
        Theme = UITheme.Default;
    }
    public UITheme Theme { get; set; }
    public void UpdateTheme(UITheme newTheme)
    {

    }
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
