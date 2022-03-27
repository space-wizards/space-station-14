using System.Linq;
using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controllers;

public sealed partial class InventoryUIController
{
    [UISystemDependency] private HandsSystem? _handsSystem = default!;
    private List<HandsContainer> _handsContainers = new();
    private readonly Dictionary<string,int> _handContainerIndices = new();
    private HandsComponent? _playerHandsComponent;
    private int _backupSuffix = 0;
    private Action<string>? _onHandActivate = null; //called when the user clicks the little activation button in the slot


    public void SetPlayerHandsComponent(HandsComponent? hands)
    {
        _playerHandsComponent = hands;
    }

    public int GetHandContainerIndex(string containerName)
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



    //propagate hand activation to the hand system.
    public void ActivateHand(HandControl handButton)
    {
        _onHandActivate?.Invoke(handButton.SlotName);
    }

    public void AddHand(Hand handData)
    {
        var newHandButton = new HandControl(this, handData.Name, handData.Location);
        GetFirstAvailableContainer().AddButton(newHandButton);
    }

    public void RemoveHand(Hand handData)
    {

    }


    public string RegisterHandContainer(HandsContainer handContainer)
    {
        var name = "HandContainer_" + _backupSuffix;;
        if (handContainer.Name == null)
        {
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

    public void AttachDelegates(HandsSystem handsSystem)
    {
        _onHandActivate += handsSystem.UIHandActivate;
    }

    public void DetachDelegates(HandsSystem handsSystem)
    {
        _onHandActivate -= handsSystem.UIHandActivate;
    }


}
