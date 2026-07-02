using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Hands.Controls;

public sealed class HandsContainer : ItemSlotUIContainer<HandButton>
{
    private readonly GridContainer _grid;
    private readonly List<HandButton> _orderedButtons = new();
    public HandsComponent? PlayerHandsComponent;

    public int ColumnLimit { get; set; } = 6;

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

    protected override void AddButton(HandButton newButton)
    {
        _orderedButtons.Add(newButton);

        _grid.RemoveAllChildren();
        var enumerable = PlayerHandsComponent?.SortedHands is { } sortedHands
            ? _orderedButtons.OrderBy(it => sortedHands.IndexOf(it.SlotName))
            : _orderedButtons.OrderBy(it => it.HandLocation);
        foreach (var button in enumerable)
        {
            _grid.AddChild(button);
        }

        _grid.Columns = Math.Min(_grid.ChildCount, ColumnLimit);
    }

    protected override void RemoveButton(HandButton button)
    {
        _orderedButtons.Remove(button);
        _grid.RemoveChild(button);
    }

    public override void ClearButtons()
    {
        base.ClearButtons();
        _orderedButtons.Clear();
    }

    public IEnumerable<HandButton> GetButtons()
    {
        foreach (var child in _grid.Children)
        {
            if (child is HandButton hand)
                yield return hand;
        }
    }
}
