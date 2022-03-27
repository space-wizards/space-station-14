using Content.Client.Hands;
using Content.Shared.Inventory;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Controls;

public sealed class ItemSlotUIContainer : ItemSlotUIContainer<ItemSlotButton> {}

[Virtual]
public abstract class ItemSlotUIContainer<T> : BoxContainer where T : ItemSlotButton
{
    protected readonly Dictionary<string, T> _buttons = new();

    public virtual bool TryAddButton(T newButton, out T button)
    {
        var tempButton = AddButton(newButton);
        if (tempButton == null)
        {
            button = newButton;
            return false;
        }
        button = newButton;
        return true;
    }

    public virtual T? AddButton(T newButton)
    {
        return !_buttons.TryAdd(newButton.SlotName, newButton) ? null : newButton;
    }

    public virtual void RemoveButton(string slotName)
    {
        _buttons.Remove(slotName);
    }

    public virtual void RemoveButton(T buttonRef)
    {
        _buttons.Remove(buttonRef.SlotName);
    }

    public virtual T? GetButton(string slotName)
    {
        return !_buttons.TryGetValue(slotName, out var button) ? null : button;
    }
}
