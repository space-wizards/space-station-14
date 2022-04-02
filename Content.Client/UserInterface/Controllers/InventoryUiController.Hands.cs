using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController
{
    [UISystemDependency] private HandsSystem _handsSystem = default!;
    private List<HandsContainer> _handsContainers = new();
    private readonly Dictionary<string,int> _handContainerIndices = new();
    private readonly Dictionary<string, HandButton> _handLookup = new();
    private HandsComponent? _playerHandsComponent;
    private HandButton? _activeHand = null;
    private int _backupSuffix = 0;//this is used when autogenerating container names if they don't have names

    private void OnHandsSystemActivate()
    {
        _handsSystem.OnAddHand += AddHand;
        _handsSystem.OnSpriteUpdate += UpdateButtonSprite;
        _handsSystem.OnSetActiveHand += SetActiveHand;
        _handsSystem.OnRemoveHand += RemoveHand;
        _handsSystem.OnComponentConnected += LoadPlayerHands;
        _handsSystem.OnComponentDisconnected += UnloadPlayerHands;
        _handsSystem.OnHandBlocked += HandBlocked;
        _handsSystem.OnHandUnblocked += HandUnblocked;
    }

    private void OnHandsSystemDeactivate()
    {
        _handsSystem.OnAddHand -= AddHand;
        _handsSystem.OnSpriteUpdate -= UpdateButtonSprite;
        _handsSystem.OnSetActiveHand -= SetActiveHand;
        _handsSystem.OnRemoveHand -= RemoveHand;
        _handsSystem.OnComponentConnected -= LoadPlayerHands;
        _handsSystem.OnComponentDisconnected -= UnloadPlayerHands;
        _handsSystem.OnHandBlocked -= HandBlocked;
        _handsSystem.OnHandUnblocked -= HandUnblocked;
    }

    private void OnHandPressed(GUIBoundKeyEventArgs args, ItemSlotControl hand)
    {
        if (_playerHandsComponent == null)
        {
            return;
        }

        if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _handsSystem.UIInventoryExamine(hand.SlotName);
        }
        else if (args.Function == ContentKeyFunctions.OpenContextMenu)
        {
            _handsSystem.UIHandOpenContextMenu(hand.SlotName);
        }
        else if (args.Function == EngineKeyFunctions.UIClick)
        {
            _handsSystem.UIHandClick(_playerHandsComponent, hand.SlotName);
        }
    }

    private void UnloadPlayerHands()
    {
        foreach (var container in _handsContainers)
        {
            container.Dispose();
        }
        _handsContainers.Clear();
        _handContainerIndices.Clear();
        _handLookup.Clear();
        _playerHandsComponent = null;
    }

    private void LoadPlayerHands(HandsComponent handsComp)
    {
        _playerHandsComponent = handsComp;
        foreach (var (name, hand) in handsComp.Hands)
        {
            AddHand(name, hand.Location);
        }
        var activeHand = handsComp.ActiveHand;
        if (activeHand == null) return;
        SetActiveHand(activeHand.Name);
    }

    private void HandBlocked(string handName)
    {
        if (!_handLookup.TryGetValue(handName, out var hand))
        {
            return;
        }

        hand.Blocked = true;
    }

    private void HandUnblocked(string handName)
    {
        if (!_handLookup.TryGetValue(handName, out var hand))
        {
            return;
        }

        hand.Blocked = false;
    }

    private int GetHandContainerIndex(string containerName)
    {
        if (!_handContainerIndices.TryGetValue(containerName, out var result)) return -1;
        return result;
    }

    private void UpdateButtonSprite(string name, ISpriteComponent? spriteComp)
    {
        var hand = GetHand(name);
        if (hand == null) return;
        hand.SpriteView.Sprite = spriteComp;
    }

    private HandsContainer GetFirstAvailableContainer()
    {
        if (_handsContainers.Count == 0) throw new Exception("Could not find an attached hand hud container");
        foreach (var container in _handsContainers)
        {
            if (container.IsFull) continue;
            return container;
        }
        throw new Exception("All attached hand hud containers were full!");
    }

    public bool TryGetHandContainer(string containerName, out HandsContainer? container)
    {
        container = null;
        var containerIndex = GetHandContainerIndex(containerName);
        if (containerIndex == -1) return false;
        container = _handsContainers[containerIndex];
        return true;
    }

    private HandButton? GetLastHand()//useful for sorting
    {
        TryGetLastHand(out var handButton);
        return handButton;
    }

    private bool TryGetLastHand(out HandButton? handButton)
    {
        handButton = null;
        for (var i = _handsContainers.Count-1; i >= 0; i--)
        {
            var hands = _handsContainers[i];
            if (hands.ButtonCount == 0 || !hands.TryGetLastButton(out handButton)) continue;
            return true;
        }
        return false;
    }
    //propagate hand activation to the hand system.
    private void StorageActivate(GUIBoundKeyEventArgs args, ItemSlotControl handControl)
    {
        _handsSystem.UIHandActivate(handControl.SlotName);
    }

    private void SetActiveHand(string? handName)
    {
        if (handName == null)
        {
            if (_activeHand != null)
            {
                _activeHand.Highlight = false;
            }
            return;
        }
        if (!_handLookup.TryGetValue(handName, out var handControl) || handControl == _activeHand) return;
        if (_activeHand != null)
        {
            _activeHand.Highlight = false;
        }
        handControl.Highlight = true;
        _activeHand = handControl;
    }
    private HandButton? GetHand(string handName)
    {
        _handLookup.TryGetValue(handName, out var handControl);
        return handControl;
    }

    private void AddHand(string handName, HandLocation location)
    {
        var newHandButton = new HandButton(this, handName, location);
        newHandButton.OnStoragePressed += StorageActivate;
        newHandButton.OnPressed += OnHandPressed;
        if (!_handLookup.TryAdd(handName, newHandButton))
            throw new Exception("Tried to add hand with duplicate name to UI. Name:" + handName);
        GetFirstAvailableContainer().AddButton(newHandButton);
    }

    private void BalanceContainers()
    {
        if (_handsContainers.Count <= 1) return;
        //TODO: Actually implement container balancing :P - jezi
        //currently we only use a single container but I want to support this later.
    }

    private void RemoveHand(string handName)
    {
        RemoveHand(handName, out var _);
    }

    private bool RemoveHand(string handName, out HandButton? handButton)
    {
        handButton = null;
        if (!_handLookup.TryGetValue(handName, out handButton)) return false;
        if (handButton.Parent is HandsContainer handContainer)
        {
            handContainer.RemoveButton(handButton);
        }
        _handLookup.Remove(handName);
        handButton.Dispose();
        BalanceContainers();
        return true;
    }
    public string RegisterHandContainer(HandsContainer handContainer)
    {
        var name = "HandContainer_" + _backupSuffix;;
        if (handContainer.Name == null)
        {
            handContainer.Name = name;
            _backupSuffix++;
        }
        else
        {
            name = handContainer.Name;
        }
        _handContainerIndices.Add(name, _handsContainers.Count);
        _handsContainers.Add(handContainer);
        return name;
    }
    public bool RemoveHandContainer(string handContainerName)
    {
        var index = GetHandContainerIndex(handContainerName);
        if (index == -1) return false;
        _handContainerIndices.Remove(handContainerName);
        _handsContainers.RemoveAt(index);
        return true;
    }
    public bool RemoveHandContainer(string handContainerName, out HandsContainer? container)
    {
        var success = _handContainerIndices.TryGetValue(handContainerName, out var index);
        container = _handsContainers[index];
        _handContainerIndices.Remove(handContainerName);
        _handsContainers.RemoveAt(index);
        return success;
    }
}
