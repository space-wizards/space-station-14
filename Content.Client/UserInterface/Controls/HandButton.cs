using Content.Client.UserInterface.Controllers;
using Content.Shared.Hands.Components;

namespace Content.Client.UserInterface.Controls;

public sealed class HandButton : ItemSlotControl
{
    public HandButton(InventoryUIController parentController,string handName, HandLocation handLocation)
    {
        Name = "hand_" + handName;
        SlotName = handName;
        SetBackground(handLocation);
    }

    private void SetBackground(HandLocation handLoc)
    {
        switch (handLoc)
        {
            case HandLocation.Left:
            {
                ButtonTexturePath = "slots/hand_l";
                break;
            }
            case HandLocation.Middle:
            {
                ButtonTexturePath = "slots/hand_m";
                break;
            }
            case HandLocation.Right:
            {
                ButtonTexturePath = "slots/hand_r";
                break;
            }
        }
    }
}
