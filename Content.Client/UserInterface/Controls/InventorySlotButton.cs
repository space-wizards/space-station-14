using Content.Client.Items.Managers;
using Content.Shared.Input;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls;

public sealed class InventorySlotButton : ItemSlotButton
{

    private IItemSlotManager _itemSlotManager;
    public string SlotName { get;}

    public string SlotDisplayName { get; }

    //I could use dep-injection here but the slotcontainer already has these deps stored so it's easier just to pass them in
    public InventorySlotButton(IItemSlotManager itemSlotManager,IEntityManager entityManager ,SlotDefinition slotDef)
    {
        Name = slotDef.Name + "InvSlot";//set the name of the control so that we know what the fuck this is in debug mode
        Visible = false; //hide the button by default
        _itemSlotManager = itemSlotManager;
        SlotName = slotDef.Name;
        SlotDisplayName = slotDef.DisplayName;
    }


    /*
 *
 *
 * {
 */



}
