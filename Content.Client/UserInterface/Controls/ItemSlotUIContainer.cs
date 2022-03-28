using Content.Client.Hands;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Controls;

public interface IItemslotUIContainer
{
    public bool TryRegisterButton(ItemSlotControl control, string newSlotName);

    public bool TryAddButton(ItemSlotControl control);
}

[Virtual]
public abstract class ItemSlotUIContainer<T> : BoxContainer, IItemslotUIContainer where T : ItemSlotControl
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

    public void ClearButtons()
    {
        foreach (var button in _buttons.Values)
        {
            button.Dispose();
        }
        _buttons.Clear();
    }


    public bool TryRegisterButton(ItemSlotControl control, string newSlotName)
    {
        if (newSlotName == "") return false;
        if (!(control is T slotButton)) return false;
        if (_buttons.TryGetValue(newSlotName, out var foundButton))
        {
            if (control == foundButton) return true; //if the slotName is already set do nothing
            throw new Exception("Could not update button to slot:" + newSlotName + " slot already assigned!");
        }
        _buttons.Remove(slotButton.SlotName);
        _buttons.Add(newSlotName, slotButton);
        if (!Children.Contains(control)&& slotButton.Parent == null)  AddChild(control);
        return true;
    }

    public bool TryAddButton(ItemSlotControl control)
    {
        if (control is not T newButton) return false;
        return AddButton(newButton) != null;
    }

    public virtual T? AddButton(T newButton)
    {
        if (newButton.SlotName == "")
        {
            Logger.Warning("Could not add button "+newButton.Name+"No slotname");
        }

        if (!Children.Contains(newButton) && newButton.Parent == null) AddChild(newButton);
        return !_buttons.TryAdd(newButton.SlotName, newButton) ? null : newButton;
    }

    public virtual void RemoveButton(string slotName)
    {
        _buttons.Remove(slotName);
        if (!_buttons.TryGetValue(slotName, out var button)) return;
        Children.Remove(button);
        button.Dispose();
    }

    public virtual void RemoveButton(T button)
    {
        Children.Remove(button);
        _buttons.Remove(button.SlotName);
        button.Dispose();
    }

    public virtual T? GetButton(string slotName)
    {
        return !_buttons.TryGetValue(slotName, out var button) ? null : button;
    }
}
