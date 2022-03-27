using Content.Client.UserInterface.Controllers;
using Content.Shared.Hands.Components;

namespace Content.Client.UserInterface.Controls;

public sealed class HandControl : ItemSlotButton
{
    private HandLocation _location;
    public HandControl(InventoryUIController parentController,string handName, HandLocation handLocation)
    {
        Name = "hand_" + handName;
        SlotName = handName;
        SetBackground(_location = handLocation);
    }

    private void SetBackground(HandLocation handLoc)
    {
        switch (handLoc)
        {
            case HandLocation.Left:
            {
                ButtonTexturePath = "hand_l.png";
                break;
            }
            case HandLocation.Middle:
            {
                ButtonTexturePath = "hand_m.png";
                break;
            }
            case HandLocation.Right:
            {
                ButtonTexturePath = "hand_r.png";
                break;
            }
        }
    }
}
