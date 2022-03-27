using System.Linq;
using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController
{
    [UISystemDependency] private HandsSystem _handsSystem = default!;
    private List<HandsContainer> _handsContainers = new();
    private readonly Dictionary<string,int> _handContainerIndices = new();
    private readonly Dictionary<string, HandControl> _handLookup = new();
    private HandsComponent? _playerHandsComponent;
    private HandControl? _activeHand = null;
    private int _backupSuffix = 0;//this is used when autogenerating container names if they don't have names
    private Action<string>? _onStorageActivate = null; //called when the user clicks the little activation button in the slot

    private void OnHandsSystemActivate()
    {
        Logger.Debug("HandsActive");
        if (_handsSystem.TryGetPlayerHands(out _playerHandsComponent)) LoadPlayerHands(_playerHandsComponent);
        _onStorageActivate += _handsSystem.UIHandActivate;
        _handsSystem.OnAddHand += AddHand;
        _handsSystem.OnSetActiveHand += SetActiveHand;
        _handsSystem.OnRemoveHand += RemoveHand;
        _handsSystem.OnComponentConnected += LoadPlayerHands;
        _handsSystem.OnComponentDisconnected += UnloadPlayerHands;
    }
    private void OnHandsSystemDeactivate()
    {
        Logger.Debug("HandsInactive");
        _onStorageActivate -= _handsSystem.UIHandActivate;
        _handsSystem.OnAddHand -= AddHand;
        _handsSystem.OnSetActiveHand -= SetActiveHand;
        _handsSystem.OnRemoveHand -= RemoveHand;
        _handsSystem.OnComponentConnected -= LoadPlayerHands;
        _handsSystem.OnComponentDisconnected -= UnloadPlayerHands;
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

    private int GetHandContainerIndex(string containerName)
    {
        if (!_handContainerIndices.TryGetValue(containerName, out var result)) return -1;
        return result;
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

    private HandControl? GetLastHand()//useful for sorting
    {
        TryGetLastHand(out var handButton);
        return handButton;
    }

    private bool TryGetLastHand(out HandControl? handButton)
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
    private void StorageActivate(GUIBoundKeyEventArgs args, ItemSlotButton handButton)
    {
        _onStorageActivate?.Invoke(handButton.SlotName);
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
    private HandControl? GetHand(string handName)
    {
        _handLookup.TryGetValue(handName, out var handControl);
        return handControl;
    }

    private void AddHand(string handName, HandLocation location)
    {
        var newHandButton = new HandControl(this, handName, location);
        newHandButton.OnStoragePressed += StorageActivate;
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

    private bool RemoveHand(string handName, out HandControl? handButton)
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
