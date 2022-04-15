using Content.Client.Items.UI;
using Content.Shared.Inventory;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;

namespace Content.Client.Inventory;

public partial class ClientInventorySystem
{
    void InitializeInventorySlots()
    {
        SubscribeLocalEvent<ClientInventorySlotComponent, ComponentInit>(OnInvSlotCompInit);
        SubscribeLocalEvent<ClientInventorySlotComponent, ComponentAdd>(OnInvSlotCompAdd);
        SubscribeLocalEvent<ClientInventorySlotComponent, ComponentRemove>(OnInvSlotCompRemove);
    }

    private void OnInvSlotCompRemove(EntityUid uid, ClientInventorySlotComponent component, ComponentRemove args)
    {
        if (TryComp<ClientInventoryComponent>(uid, out var invComp))
        {
            foreach (var (id, btn) in component.SlotButtons)
            {
                _itemSlotManager.SetItemSlot(btn, null);
                var slotDef = component.Slots[component.SlotDefIndexes[id]];
                switch (slotDef.UIContainer)
                {
                    case SlotUIContainer.BottomLeft:
                        invComp.BottomLeftButtons.RemoveChild(btn);
                        break;
                    case SlotUIContainer.BottomRight:
                        invComp.BottomRightButtons.RemoveChild(btn);
                        break;
                    case SlotUIContainer.Top:
                        invComp.TopQuickButtons.RemoveChild(btn);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                invComp.InventoryWindow.RemoveChild(btn);
            }
        }
    }

    private void OnInvSlotCompAdd(EntityUid uid, ClientInventorySlotComponent component, ComponentAdd args)
    {
        if(TryComp<ClientInventoryComponent>(uid, out var invComp))
        {
            if (TryComp<ContainerManagerComponent>(uid, out var containerManager))
                InitSlotButtons(uid, component.SlotButtons, invComp, containerManager);

            foreach (var (id, btn) in component.SlotButtons)
            {
                var slotDef = component.Slots[component.SlotDefIndexes[id]];
                switch (slotDef.UIContainer)
                {
                    case SlotUIContainer.BottomLeft:
                        invComp.BottomLeftButtons.AddChild(btn);
                        break;
                    case SlotUIContainer.BottomRight:
                        invComp.BottomRightButtons.AddChild(btn);
                        break;
                    case SlotUIContainer.Top:
                        invComp.TopQuickButtons.AddChild(btn);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                invComp.InventoryWindow.AddChild(btn);
                LayoutContainer.SetPosition(btn, slotDef.UIWindowPosition * (ButtonSize + ButtonSeparation));
            }
        }
    }

    private void OnInvSlotCompInit(EntityUid uid, ClientInventorySlotComponent component, ComponentInit args)
    {
        for (var i = 0; i < component.Slots.Length; i++)
        {
            var slotDefinition = component.Slots[i];
            component.SlotButtons.Add(slotDefinition.Name, GenerateButton(uid, slotDefinition));
            component.SlotDefIndexes.Add(slotDefinition.Name, i);
        }
    }
}
