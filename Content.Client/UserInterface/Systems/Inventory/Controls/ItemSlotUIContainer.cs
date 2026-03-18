using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Inventory.Controls;

public interface IItemslotUIContainer
{
    public bool TryRegisterButton(SlotControl control, string newSlotName);

    public bool TryAddButton(SlotControl control);
}

[Virtual]
public abstract class ItemSlotUIContainer<T> : GridContainer, IItemslotUIContainer where T : SlotControl
{
    private readonly Dictionary<string, T> _buttons = new();

    public int? MaxColumns { get; set; }

    public virtual void ClearButtons()
    {
        foreach (var button in _buttons.Values)
        {
            button.Orphan();
        }

        _buttons.Clear();
    }

    public bool TryRegisterButton(SlotControl control, string newSlotName)
    {
        if (newSlotName == "")
            return false;
        if (control is not T slotButton)
            return false;

        if (_buttons.TryGetValue(newSlotName, out var foundButton))
        {
            if (control == foundButton)
                return true; //if the slotName is already set do nothing
            throw new Exception("Could not update button to slot:" + newSlotName + " slot already assigned!");
        }

        _buttons.Remove(slotButton.SlotName);
        TryAddButton(slotButton);
        return true;
    }

    public bool TryAddButton(SlotControl control)
    {
        if (control is not T newButton)
            return false;
        return TryAddButton(newButton) != null;
    }

    public T? TryAddButton(T newButton)
    {
        if (newButton.SlotName == "")
        {
            Log.Warning($"{newButton.Name} because it has no slot name");
            return null;
        }

        if (Children.Contains(newButton) || newButton.Parent != null)
            return null;

        if (!_buttons.TryAdd(newButton.SlotName, newButton))
            return null;

        AddButton(newButton);
        return newButton;
    }

    protected virtual void AddButton(T newButton)
    {
        AddChild(newButton);
        Columns = MaxColumns ?? ChildCount;
    }

    public bool TryRemoveButton(string slotName, [NotNullWhen(true)] out T? button)
    {
        if (!_buttons.TryGetValue(slotName, out button))
            return false;

        _buttons.Remove(button.SlotName);
        RemoveButton(button);
        return true;
    }

    protected virtual void RemoveButton(T button)
    {
        Children.Remove(button);
    }

    public T? GetButton(string slotName)
    {
        return _buttons.GetValueOrDefault(slotName);
    }

    public bool TryGetButton(string slotName, [NotNullWhen(true)] out T? button)
    {
        return (button = GetButton(slotName)) != null;
    }
}
