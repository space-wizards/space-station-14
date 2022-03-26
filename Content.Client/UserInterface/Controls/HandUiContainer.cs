using System.Linq;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Controls;

public sealed class HandsContainer : ItemSlotUIContainer<HandControl>
{
    private readonly GridContainer _grid;
    public int ColumnLimit { get => _grid.Columns; set => _grid.Columns = value; }
    public HandsContainer()
    {
        AddChild(_grid = new GridContainer());
    }
    public override HandControl? AddButton(HandControl newButton)
    {
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
    public void RemoveHandControl(HandControl hand)
    {
        base.RemoveButton(hand);
        _grid.RemoveChild(hand);
    }
    public bool TryGetLastHand(out HandControl? control)
    {
        if (_buttons.Count == 0)
        {
            control = null;
            return false;
        }
        control = _buttons.Values.Last();
        return true;
    }
    public bool TryRemoveLastHand(out HandControl? control)
    {
        var success = TryGetLastHand(out control);
        if (control != null) RemoveHandControl(control);
        return success;
    }

    public int HandCount => _grid.ChildCount;

}
