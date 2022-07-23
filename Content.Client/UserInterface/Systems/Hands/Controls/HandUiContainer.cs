using System.Linq;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Hands.Controls;

public sealed class HandsContainer : ItemSlotUIContainer<HandButton>
{
    private readonly GridContainer _grid;
    public int ColumnLimit { get => _grid.Columns; set => _grid.Columns = value; }
    public int MaxButtonCount { get; set; } = 0;

    public HandsContainer()
    {
        AddChild(_grid = new GridContainer());
    }

    public override HandButton? AddButton(HandButton newButton)
    {
        if (MaxButtonCount > 0)
        {
            if (ButtonCount >= MaxButtonCount) return null;
            _grid.AddChild(newButton);
            return base.AddButton(newButton);
        }

        _grid.AddChild(newButton);
        return base.AddButton(newButton);
    }

    public override void RemoveButton(string handName)
    {
        var button = GetButton(handName);
        if (button == null) return;
        base.RemoveButton(button);
        _grid.RemoveChild(button);
    }

    public bool TryGetLastButton(out HandButton? control)
    {
        if (_buttons.Count == 0)
        {
            control = null;
            return false;
        }

        control = _buttons.Values.Last();
        return true;
    }

    public bool TryRemoveLastHand(out HandButton? control)
    {
        var success = TryGetLastButton(out control);
        if (control != null) RemoveButton(control);
        return success;
    }

    public void Clear()
    {
        ClearButtons();
        _grid.DisposeAllChildren();
    }

    public bool IsFull => (MaxButtonCount != 0 && ButtonCount >= MaxButtonCount);

    public int ButtonCount => _grid.ChildCount;
}
