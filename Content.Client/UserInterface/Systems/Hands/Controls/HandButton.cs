using Content.Client.UserInterface.Controls;
using Content.Shared.Hands.Components;

namespace Content.Client.UserInterface.Systems.Hands.Controls;

public sealed class HandButton : SlotControl
{
    public HandButton(string handName, HandLocation handLocation)
    {
        Name = "hand_" + handName;
        SlotName = handName;
        SetBackground(handLocation);
    }

    private void SetBackground(HandLocation handLoc)
    {
        ButtonTexturePath = handLoc switch
        {
            HandLocation.Left => "slots/hand_l",
            HandLocation.Middle => "slots/hand_m",
            HandLocation.Right => "slots/hand_r",
            _ => ButtonTexturePath
        };
    }
}
