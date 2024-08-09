using System.Linq;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Hands.Controls;

public sealed class HandsContainer : ItemSlotUIContainer<HandButton>
{
    private readonly GridContainer _grid;
    public int ColumnLimit { get => _grid.Columns; set => _grid.Columns = value; }
    public int MaxButtonCount { get; set; } = 0;

    public int MaxButtonsPerRow { get; set;  }= 6;

    /// <summary>
    ///     Indexer. This is used to reference a HandsContainer from the
    ///     controller.
    /// </summary>
    public string? Indexer { get; set; }

    public HandsContainer()
    {
        AddChild(_grid = new GridContainer());
        _grid.ExpandBackwards = true;
    }

    public override HandButton? AddButton(HandButton newButton)
    {
        if (MaxButtonCount > 0)
        {
            if (ButtonCount >= MaxButtonCount)
                return null;

            _grid.AddChild(newButton);
        }
        else
        {
            _grid.AddChild(newButton);
        }

        _grid.Columns = Math.Min(_grid.ChildCount, MaxButtonsPerRow);
        return base.AddButton(newButton);
    }

    public override void RemoveButton(string handName)
    {
        var button = GetButton(handName);
        if (button == null)
            return;
        base.RemoveButton(button);
        _grid.RemoveChild(button);
    }

    public bool TryGetLastButton(out HandButton? control)
    {
        if (Buttons.Count == 0)
        {
            control = null;
            return false;
        }

        control = Buttons.Values.Last();
        return true;
    }

    public bool TryRemoveLastHand(out HandButton? control)
    {
        var success = TryGetLastButton(out control);
        if (control != null)
            RemoveButton(control);
        return success;
    }

    public void Clear()
    {
        ClearButtons();
        _grid.DisposeAllChildren();
    }

    public IEnumerable<HandButton> GetButtons()
    {
        foreach (var child in _grid.Children)
        {
            if (child is HandButton hand)
                yield return hand;
        }
    }

    public bool IsFull => (MaxButtonCount != 0 && ButtonCount >= MaxButtonCount);

    public int ButtonCount => _grid.ChildCount;
}
