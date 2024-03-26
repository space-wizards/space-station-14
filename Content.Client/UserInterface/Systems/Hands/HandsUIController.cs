using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Timing;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Hands;

public sealed class HandsUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<HandsSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly HandsSystem _handsSystem = default!;

    private readonly List<HandsContainer> _handsContainers = new();
    private readonly Dictionary<string, int> _handContainerIndices = new();
    private readonly Dictionary<string, HandButton> _handLookup = new();
    private HandsComponent? _playerHandsComponent;
    private HandButton? _activeHand = null;
    private int _backupSuffix = 0; //this is used when autogenerating container names if they don't have names

    private HotbarGui? HandsGui => UIManager.GetActiveUIWidgetOrNull<HotbarGui>();

    public void OnSystemLoaded(HandsSystem system)
    {
        _handsSystem.OnPlayerAddHand += OnAddHand;
        _handsSystem.OnPlayerItemAdded += OnItemAdded;
        _handsSystem.OnPlayerItemRemoved += OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand += SetActiveHand;
        _handsSystem.OnPlayerRemoveHand += RemoveHand;
        _handsSystem.OnPlayerHandsAdded += LoadPlayerHands;
        _handsSystem.OnPlayerHandsRemoved += UnloadPlayerHands;
        _handsSystem.OnPlayerHandBlocked += HandBlocked;
        _handsSystem.OnPlayerHandUnblocked += HandUnblocked;
    }

    public void OnSystemUnloaded(HandsSystem system)
    {
        _handsSystem.OnPlayerAddHand -= OnAddHand;
        _handsSystem.OnPlayerItemAdded -= OnItemAdded;
        _handsSystem.OnPlayerItemRemoved -= OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand -= SetActiveHand;
        _handsSystem.OnPlayerRemoveHand -= RemoveHand;
        _handsSystem.OnPlayerHandsAdded -= LoadPlayerHands;
        _handsSystem.OnPlayerHandsRemoved -= UnloadPlayerHands;
        _handsSystem.OnPlayerHandBlocked -= HandBlocked;
        _handsSystem.OnPlayerHandUnblocked -= HandUnblocked;
    }

    private void OnAddHand(string name, HandLocation location)
    {
        AddHand(name, location);
    }

    private void HandPressed(GUIBoundKeyEventArgs args, SlotControl hand)
    {
        if (_playerHandsComponent == null)
        {
            return;
        }

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _handsSystem.UIHandClick(_playerHandsComponent, hand.SlotName);
        }
        else if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            _handsSystem.UIHandOpenContextMenu(hand.SlotName);
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _handsSystem.UIHandActivate(hand.SlotName);
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            _handsSystem.UIHandAltActivateItem(hand.SlotName);
        }
        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _handsSystem.UIInventoryExamine(hand.SlotName);
        }
    }

    private void UnloadPlayerHands()
    {
        if (HandsGui != null)
            HandsGui.Visible = false;

        _handContainerIndices.Clear();
        _handLookup.Clear();
        _playerHandsComponent = null;

        foreach (var container in _handsContainers)
        {
            container.Clear();
        }
    }

    private void LoadPlayerHands(HandsComponent handsComp)
    {
        DebugTools.Assert(_playerHandsComponent == null);
        if (HandsGui != null)
            HandsGui.Visible = true;

        _playerHandsComponent = handsComp;
        foreach (var (name, hand) in handsComp.Hands)
        {
            var handButton = AddHand(name, hand.Location);

            if (_entities.TryGetComponent(hand.HeldEntity, out VirtualItemComponent? virt))
            {
                handButton.SpriteView.SetEntity(virt.BlockingEntity);
                handButton.Blocked = true;
            }
            else
            {
                handButton.SpriteView.SetEntity(hand.HeldEntity);
                handButton.Blocked = false;
            }
        }

        var activeHand = handsComp.ActiveHand;
        if (activeHand == null)
            return;
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
        if (!_handContainerIndices.TryGetValue(containerName, out var result))
            return -1;
        return result;
    }

    private void OnItemAdded(string name, EntityUid entity)
    {
        var hand = GetHand(name);
        if (hand == null)
            return;

        if (_entities.TryGetComponent(entity, out VirtualItemComponent? virt))
        {
            hand.SpriteView.SetEntity(virt.BlockingEntity);
            hand.Blocked = true;
        }
        else
        {
            hand.SpriteView.SetEntity(entity);
            hand.Blocked = false;
        }

        if (_playerHandsComponent?.ActiveHand?.Name == name)
            HandsGui?.UpdatePanelEntity(entity);
    }

    private void OnItemRemoved(string name, EntityUid entity)
    {
        var hand = GetHand(name);
        if (hand == null)
            return;

        hand.SpriteView.SetEntity(null);
        if (_playerHandsComponent?.ActiveHand?.Name == name)
            HandsGui?.UpdatePanelEntity(null);
    }

    private HandsContainer GetFirstAvailableContainer()
    {
        if (_handsContainers.Count == 0)
            throw new Exception("Could not find an attached hand hud container");
        foreach (var container in _handsContainers)
        {
            if (container.IsFull)
                continue;
            return container;
        }

        throw new Exception("All attached hand hud containers were full!");
    }

    public bool TryGetHandContainer(string containerName, out HandsContainer? container)
    {
        container = null;
        var containerIndex = GetHandContainerIndex(containerName);
        if (containerIndex == -1)
            return false;
        container = _handsContainers[containerIndex];
        return true;
    }

    //propagate hand activation to the hand system.
    private void StorageActivate(GUIBoundKeyEventArgs args, SlotControl handControl)
    {
        _handsSystem.UIHandActivate(handControl.SlotName);
    }

    private void SetActiveHand(string? handName)
    {
        if (handName == null)
        {
            if (_activeHand != null)
                _activeHand.Highlight = false;

            HandsGui?.UpdatePanelEntity(null);
            return;
        }

        if (!_handLookup.TryGetValue(handName, out var handControl) || handControl == _activeHand)
            return;

        if (_activeHand != null)
            _activeHand.Highlight = false;

        handControl.Highlight = true;
        _activeHand = handControl;

        if (HandsGui != null &&
            _playerHandsComponent != null &&
            _player.LocalSession?.AttachedEntity is { } playerEntity &&
            _handsSystem.TryGetHand(playerEntity, handName, out var hand, _playerHandsComponent))
        {
            HandsGui.UpdatePanelEntity(hand.HeldEntity);
        }
    }

    private HandButton? GetHand(string handName)
    {
        _handLookup.TryGetValue(handName, out var handControl);
        return handControl;
    }

    private HandButton AddHand(string handName, HandLocation location)
    {
        var button = new HandButton(handName, location);
        button.StoragePressed += StorageActivate;
        button.Pressed += HandPressed;

        if (!_handLookup.TryAdd(handName, button))
            throw new Exception("Tried to add hand with duplicate name to UI. Name:" + handName);

        if (HandsGui != null)
        {
            HandsGui.HandContainer.AddButton(button);
        }
        else
        {
            GetFirstAvailableContainer().AddButton(button);
        }

        return button;
    }

    /// <summary>
    ///     Reload all hands.
    /// </summary>
    public void ReloadHands()
    {
        UnloadPlayerHands();
        _handsSystem.ReloadHandButtons();
    }

    /// <summary>
    ///     Swap hands from one container to the other.
    /// </summary>
    /// <param name="other"></param>
    /// <param name="source"></param>
    public void SwapHands(HandsContainer other, HandsContainer? source = null)
    {
        if (HandsGui == null && source == null)
        {
            throw new ArgumentException("Cannot swap hands if no source hand container exists!");
        }

        source ??= HandsGui!.HandContainer;

        var transfer = new List<Control>();
        foreach (var child in source.Children)
        {
            if (child is not HandButton)
            {
                continue;
            }

            transfer.Add(child);
        }

        foreach (var control in transfer)
        {
            source.RemoveChild(control);
            other.AddChild(control);
        }
    }

    private void RemoveHand(string handName)
    {
        RemoveHand(handName, out _);
    }

    private bool RemoveHand(string handName, out HandButton? handButton)
    {
        if (!_handLookup.TryGetValue(handName, out handButton))
            return false;
        if (handButton.Parent is HandsContainer handContainer)
        {
            handContainer.RemoveButton(handButton);
        }

        _handLookup.Remove(handName);
        handButton.Dispose();
        return true;
    }

    public string RegisterHandContainer(HandsContainer handContainer)
    {
        var name = "HandContainer_" + _backupSuffix;

        if (handContainer.Indexer == null)
        {
            handContainer.Indexer = name;
            _backupSuffix++;
        }
        else
        {
            name = handContainer.Indexer;
        }

        _handContainerIndices.Add(name, _handsContainers.Count);
        _handsContainers.Add(handContainer);
        return name;
    }

    public bool RemoveHandContainer(string handContainerName)
    {
        var index = GetHandContainerIndex(handContainerName);
        if (index == -1)
            return false;
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

    public void OnStateEntered(GameplayState state)
    {
        if (HandsGui != null)
            HandsGui.Visible = _playerHandsComponent != null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // TODO this should be event based but 2 systems modify the same component differently for some reason
        foreach (var container in _handsContainers)
        {
            foreach (var hand in container.GetButtons())
            {

                if (!_entities.TryGetComponent(hand.Entity, out UseDelayComponent? useDelay) ||
                    useDelay is not { DelayStartTime: var start, DelayEndTime: var end })
                {
                    hand.CooldownDisplay.Visible = false;
                    continue;
                }

                hand.CooldownDisplay.Visible = true;
                hand.CooldownDisplay.FromTime(start, end);
            }
        }
    }
}
