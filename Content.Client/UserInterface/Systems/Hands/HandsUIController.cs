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
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Hands;

public sealed class HandsUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<HandsSystem>
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly HandsSystem _handsSystem = default!;
    [UISystemDependency] private readonly UseDelaySystem _useDelay = default!;

    private HandsComponent? _playerHandsComponent;
    private HandButton? _activeHand;

    // We only have two item status controls (left and right hand),
    // but we may have more than two hands.
    // We handle this by having the item status be the *last active* hand of that side.
    // These variables store which that is.
    // ("middle" hands are hardcoded as right, whatever)
    private HandButton? _statusHandLeft;
    private HandButton? _statusHandRight;

    private HotbarGui? HandsGui => UIManager.GetActiveUIWidgetOrNull<HotbarGui>();

    public void OnSystemLoaded(HandsSystem system)
    {
        _handsSystem.OnPlayerAddHand += OnAddHand;
        _handsSystem.OnPlayerItemAdded += OnItemAdded;
        _handsSystem.OnPlayerItemRemoved += OnItemRemoved;
        _handsSystem.OnPlayerSetActiveHand += SetActiveHand;
        _handsSystem.OnPlayerRemoveHand += OnRemoveHand;
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
        _handsSystem.OnPlayerRemoveHand -= OnRemoveHand;
        _handsSystem.OnPlayerHandsAdded -= LoadPlayerHands;
        _handsSystem.OnPlayerHandsRemoved -= UnloadPlayerHands;
        _handsSystem.OnPlayerHandBlocked -= HandBlocked;
        _handsSystem.OnPlayerHandUnblocked -= HandUnblocked;
    }

    private void OnAddHand(Entity<HandsComponent> entity, string name, HandLocation location)
    {
        if (entity.Owner != _player.LocalEntity)
            return;
        if (_handsSystem.TryGetHand((entity.Owner, entity.Comp), name, out var hand))
            AddHand(name, hand.Value);
    }

    private void OnRemoveHand(Entity<HandsComponent> entity, string name)
    {
        if (entity.Owner != _player.LocalEntity)
            return;
        RemoveHand(name);
    }

    private void HandPressed(GUIBoundKeyEventArgs args, SlotControl hand)
    {
        if (!_handsSystem.TryGetPlayerHands(out var hands))
            return;

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _handsSystem.UIHandClick(hands.Value, hand.SlotName);
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UseSecondary)
        {
            _handsSystem.UIHandOpenContextMenu(hand.SlotName);
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ActivateItemInWorld)
        {
            _handsSystem.UIHandActivate(hand.SlotName);
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            _handsSystem.UIHandAltActivateItem(hand.SlotName);
            args.Handle();
        }
        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            _handsSystem.UIInventoryExamine(hand.SlotName);
            args.Handle();
        }
    }

    private void UnloadPlayerHands()
    {
        HandsGui?.Visible = false;
        HandsGui?.HandContainer.ClearButtons();
        _playerHandsComponent = null;
    }

    private void LoadPlayerHands(Entity<HandsComponent> handsComp)
    {
        DebugTools.Assert(_playerHandsComponent == null);
        HandsGui?.Visible = true;
        HandsGui?.HandContainer.PlayerHandsComponent = handsComp;
        _playerHandsComponent = handsComp;
        foreach (var (name, hand) in handsComp.Comp.Hands)
        {
            var handButton = AddHand(name, hand);

            if (_handsSystem.TryGetHeldItem(handsComp.AsNullable(), name, out var held) &&
                _entities.TryGetComponent(held, out VirtualItemComponent? virt))
            {
                handButton.SetEntity(virt.BlockingEntity);
                handButton.Blocked = true;
            }
            else if (held != null)
            {
                handButton.SetEntity(held);
                handButton.Blocked = false;
            }
            else
            {
                if (hand.EmptyRepresentative is { } representative)
                {
                    // placeholder, view it
                    SetRepresentative(handButton, representative);
                }
                else
                {
                    // otherwise empty
                    handButton.SetEntity(null);
                }
                handButton.Blocked = false;
            }
        }

        if (handsComp.Comp.ActiveHandId == null)
            return;
        SetActiveHand(handsComp.Comp.ActiveHandId);
    }

    private void SetRepresentative(HandButton handButton, EntProtoId prototype)
    {
        handButton.SetPrototype(prototype, true);
    }

    private void HandBlocked(string handName)
    {
        if (HandsGui?.HandContainer.TryGetButton(handName, out var hand) != true)
            return;

        hand!.Blocked = true;
    }

    private void HandUnblocked(string handName)
    {
        if (HandsGui?.HandContainer.TryGetButton(handName, out var hand) != true)
            return;

        hand!.Blocked = false;
    }

    private void OnItemAdded(string name, EntityUid entity)
    {
        var hand = GetHand(name);
        if (hand == null)
            return;

        if (_entities.TryGetComponent(entity, out VirtualItemComponent? virt))
        {
            hand.SetEntity(virt.BlockingEntity);
            hand.Blocked = true;
        }
        else
        {
            hand.SetEntity(entity);
            hand.Blocked = false;
        }

        if (_playerHandsComponent != null &&
            _player.LocalSession?.AttachedEntity is { } playerEntity &&
            _handsSystem.TryGetHand((playerEntity, _playerHandsComponent), name, out var handData))
        {
            UpdateHandStatus(hand, entity, handData);
        }
    }

    private void OnItemRemoved(string name, EntityUid entity)
    {
        var hand = GetHand(name);
        if (hand == null)
            return;

        if (_playerHandsComponent != null &&
            _player.LocalSession?.AttachedEntity is { } playerEntity &&
            _handsSystem.TryGetHand((playerEntity, _playerHandsComponent), name, out var handData))
        {
            UpdateHandStatus(hand, null, handData);
            if (handData.Value.EmptyRepresentative is { } representative)
            {
                SetRepresentative(hand, representative);
                return;
            }
        }

        hand.SetEntity(null);
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

            return;
        }

        if (HandsGui?.HandContainer.TryGetButton(handName, out var handControl) != true || handControl == _activeHand)
            return;

        if (_activeHand != null)
            _activeHand.Highlight = false;

        handControl!.Highlight = true;
        _activeHand = handControl;

        if (_playerHandsComponent != null &&
            _player.LocalSession?.AttachedEntity is { } playerEntity &&
            _handsSystem.TryGetHand((playerEntity, _playerHandsComponent), handName, out var hand))
        {
            var heldEnt = _handsSystem.GetHeldItem((playerEntity, _playerHandsComponent), handName);

            var foldedLocation = hand.Value.Location;
            if (foldedLocation == HandLocation.Left)
            {
                _statusHandLeft = handControl;
                HandsGui.UpdatePanelEntityLeft(heldEnt, hand.Value);
            }
            else
            {
                // Middle or right
                _statusHandRight = handControl;
                HandsGui.UpdatePanelEntityRight(heldEnt, hand.Value);
            }

            HandsGui.SetHighlightHand(foldedLocation);
        }
    }

    private HandButton? GetHand(string handName)
    {
        return HandsGui?.HandContainer.GetButton(handName);
    }

    private HandButton AddHand(string handName, Hand hand)
    {
        var button = new HandButton(handName, hand.Location);
        button.StoragePressed += StorageActivate;
        button.Pressed += HandPressed;

        HandsGui?.HandContainer.TryAddButton(button);

        if (hand.EmptyRepresentative is { } representative)
        {
            SetRepresentative(button, representative);
        }
        UpdateHandStatus(button, null, hand);

        // If we don't have a status for this hand type yet, set it.
        // This means we have status filled by default in most scenarios,
        // otherwise the user'd need to switch hands to "activate" the hands the first time.
        if (hand.Location == HandLocation.Left)
            _statusHandLeft ??= button;
        else
            _statusHandRight ??= button;

        UpdateVisibleStatusPanels();

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

    private void RemoveHand(string handName)
    {
        if (HandsGui?.HandContainer.TryRemoveButton(handName, out var handButton) != true)
            return;

        if (_statusHandLeft == handButton)
            _statusHandLeft = null;
        if (_statusHandRight == handButton)
            _statusHandRight = null;

        UpdateVisibleStatusPanels();
    }

    private void UpdateVisibleStatusPanels()
    {
        var leftVisible = false;
        var rightVisible = false;

        if (HandsGui is null)
            return;

        foreach (var hand in HandsGui.HandContainer.GetButtons())
        {
            if (hand.HandLocation == HandLocation.Left)
            {
                leftVisible = true;
            }
            else
            {
                rightVisible = true;
            }
        }

        HandsGui.UpdateStatusVisibility(leftVisible, rightVisible);
    }

    public void OnStateEntered(GameplayState state)
    {
        HandsGui?.Visible = _playerHandsComponent != null;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (HandsGui is not { } handsGui)
            return;

        // TODO this should be event based but 2 systems modify the same component differently for some reason
        foreach (var hand in handsGui.HandContainer.GetButtons())
        {

            if (!_entities.TryGetComponent(hand.Entity, out UseDelayComponent? useDelay))
            {
                hand.CooldownDisplay.Visible = false;
                continue;
            }
            var delay = _useDelay.GetLastEndingDelay((hand.Entity.Value, useDelay));

            hand.CooldownDisplay.Visible = true;
            hand.CooldownDisplay.FromTime(delay.StartTime, delay.EndTime);
        }
    }

    private void UpdateHandStatus(HandButton hand, EntityUid? entity, Hand? handData)
    {
        if (hand == _statusHandLeft)
            HandsGui?.UpdatePanelEntityLeft(entity, handData);

        if (hand == _statusHandRight)
            HandsGui?.UpdatePanelEntityRight(entity, handData);
    }
}
