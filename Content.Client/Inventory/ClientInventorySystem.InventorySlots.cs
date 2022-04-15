using Content.Client.Items.UI;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;

namespace Content.Client.Inventory;

public partial class ClientInventorySystem
{
    void InitializeInventorySlots()
    {
        SubscribeLocalEvent<ClientInventorySlotComponent, GotEquippedEvent>(OnInvSlotEquipped);
        SubscribeLocalEvent<ClientInventorySlotComponent, GotUnequippedEvent>(OnInvSlotUnequipped);
    }

    private void OnInvSlotUnequipped(EntityUid uid, ClientInventorySlotComponent component, GotUnequippedEvent args)
    {
        if (TryComp<ClientInventoryComponent>(args.Equipee, out var invComp))
        {
            foreach (var (id, btn) in component.SlotButtons)
            {
                var slotDef = component.Slots[component.SlotDefIndexes[id]];
                switch (slotDef.UIContainer)
                {
                    case SlotUIContainer.BottomLeft:
                        invComp.BottomLeftButtons.RemoveChild(btn.hudButton);
                        break;
                    case SlotUIContainer.BottomRight:
                        invComp.BottomRightButtons.RemoveChild(btn.hudButton);
                        break;
                    case SlotUIContainer.Top:
                        invComp.TopQuickButtons.RemoveChild(btn.hudButton);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                invComp.InventoryWindow.RemoveChild(btn.windowButton);
            }
        }

        foreach (var (_, btn) in component.SlotButtons)
        {
            _itemSlotManager.SetItemSlot(btn.hudButton, null);
            _itemSlotManager.SetItemSlot(btn.windowButton, null);
        }

        component.SlotButtons.Clear();
        component.SlotDefIndexes.Clear();
    }

    private void OnInvSlotEquipped(EntityUid uid, ClientInventorySlotComponent component, GotEquippedEvent args)
    {
        for (var i = 0; i < component.Slots.Length; i++)
        {
            var slotDefinition = component.Slots[i];
            component.SlotButtons.Add(slotDefinition.Name, (GenerateButton(args.Equipee, slotDefinition), GenerateButton(args.Equipee, slotDefinition)));
            component.SlotDefIndexes.Add(slotDefinition.Name, i);
        }

        if(TryComp<ClientInventoryComponent>(args.Equipee, out var invComp))
        {
            if (TryComp<ContainerManagerComponent>(args.Equipee, out var containerManager))
                InitSlotButtons(args.Equipee, component.SlotButtons, invComp, containerManager);

            foreach (var (id, btn) in component.SlotButtons)
            {
                var slotDef = component.Slots[component.SlotDefIndexes[id]];
                switch (slotDef.UIContainer)
                {
                    case SlotUIContainer.BottomLeft:
                        invComp.BottomLeftButtons.AddChild(btn.hudButton);
                        break;
                    case SlotUIContainer.BottomRight:
                        invComp.BottomRightButtons.AddChild(btn.hudButton);
                        break;
                    case SlotUIContainer.Top:
                        invComp.TopQuickButtons.AddChild(btn.hudButton);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                invComp.InventoryWindow.AddChild(btn.windowButton);
                LayoutContainer.SetPosition(btn.windowButton, slotDef.UIWindowPosition * (ButtonSize + ButtonSeparation));
            }
        }
    }
}
