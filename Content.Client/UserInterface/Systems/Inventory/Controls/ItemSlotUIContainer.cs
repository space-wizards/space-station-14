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
    protected readonly Dictionary<string, T> Buttons = new();

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
        foreach (var button in Buttons.Values)
        {
            button.Dispose();
        }

        Buttons.Clear();
    }

    public bool TryRegisterButton(SlotControl control, string newSlotName)
    {
        if (newSlotName == "")
            return false;
        if (!(control is T slotButton))
            return false;
        if (Buttons.TryGetValue(newSlotName, out var foundButton))
        {
            if (control == foundButton)
                return true; //if the slotName is already set do nothing
            throw new Exception("Could not update button to slot:" + newSlotName + " slot already assigned!");
        }

        Buttons.Remove(slotButton.SlotName);
        AddButton(slotButton);
        return true;
    }

    public bool TryAddButton(SlotControl control)
    {
        if (control is not T newButton)
            return false;
        return AddButton(newButton) != null;
    }

    public virtual T? AddButton(T newButton)
    {
        if (!Children.Contains(newButton) && newButton.Parent == null && newButton.SlotName != "")
            AddChild(newButton);
        Columns = ChildCount;
        return AddButtonToDict(newButton);
    }

    protected virtual T? AddButtonToDict(T newButton)
    {
        if (newButton.SlotName == "")
        {
            Logger.Warning("Could not add button " + newButton.Name + "No slotname");
        }

        return !Buttons.TryAdd(newButton.SlotName, newButton) ? null : newButton;
    }

    public virtual void RemoveButton(string slotName)
    {
        if (!Buttons.TryGetValue(slotName, out var button))
            return;
        RemoveButton(button);
    }

    public virtual void RemoveButtons(params string[] slotNames)
    {
        foreach (var slotName in slotNames)
        {
            RemoveButton(slotName);
        }
    }

    public virtual void RemoveButtons(params T?[] buttons)
    {
        foreach (var button in buttons)
        {
            if (button != null)
                RemoveButton(button);
        }
    }

    protected virtual void RemoveButtonFromDict(T button)
    {
        Buttons.Remove(button.SlotName);
    }

    public virtual void RemoveButton(T button)
    {
        RemoveButtonFromDict(button);
        Children.Remove(button);
        button.Dispose();
    }

    public virtual T? GetButton(string slotName)
    {
        return !Buttons.TryGetValue(slotName, out var button) ? null : button;
    }

    public virtual bool TryGetButton(string slotName, [NotNullWhen(true)] out T? button)
    {
        return (button = GetButton(slotName)) != null;
    }
}
