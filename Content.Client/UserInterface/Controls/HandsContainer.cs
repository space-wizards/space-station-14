using System.Linq;
using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

public sealed class HandsContainer : Control
{
    [Dependency] private IItemSlotManager _itemSlotManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IEntitySystemManager _systemManager = default!;
    public HandsSystem HandsSystem => _handsSystem;
    public HandsComponent? HandsComponent => _playerHands;
    private readonly HandsSystem _handsSystem;
    private HandsComponent? _playerHands = null;
    public List<HandControl> Hands => _hands.Values.ToList();
    private readonly Dictionary<string, HandControl> _hands = new();
    private readonly GridContainer _grid;
    public HandControl? ActiveHand { get; set; }

    public HandsContainer()
    {
        IoCManager.InjectDependencies(this);
        _handsSystem = _systemManager.GetEntitySystem<HandsSystem>();
        _handsSystem.TryGetPlayerHands(out _playerHands);
        AddChild(_grid = new GridContainer());
    }
    public bool TryGetHand(string name, out HandControl hand)
    {
        return _hands.TryGetValue(name, out hand!);
    }

    public bool TryGetActiveHand(out HandControl? activeHand)
    {
        activeHand = ActiveHand;
        return ActiveHand != null;
    }

    private void OnHandPressed(GUIBoundKeyEventArgs args, string handName)
    {
        if (_playerHands == null) return;//not sure how this would be possible lol
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            _handsSystem.UIHandClick(_playerHands, handName);
        }
        else if (TryGetHand(handName, out var hand))
        {
            _itemSlotManager.OnButtonPressed(args, hand.HeldItem);
        }
    }
    private void OnStoragePressed(string handName)
    {
        _handsSystem.UIHandActivate(handName);
    }
    private void UpdatePlayerHandsComponent()
    {
         _handsSystem.TryGetPlayerHands(out _playerHands);
    }

    private void RegisterHand(string name, HandLocation location, EntityUid? heldItem = null)
    {
        var newHand = new HandControl(this, location, _entityManager, _itemSlotManager);
        newHand.OnPressed += args => OnHandPressed(args, name);
        newHand.OnStoragePressed += args => OnStoragePressed(name);
        if (_hands.TryAdd(name, newHand)) return;
        throw new Exception("Duplicate handName detected!: "+ name);
    }

    public void SetActiveHand(Hand? handData)
    {
        if (handData == null)
        {

            return;
        }
        ActiveHand = null;
        _hands[handData.Name].Active = true;
    }

    public void RemoveHand(string name)
    {
        _hands[name].Dispose();
        _hands.Remove(name);
    }

    public void RemoveHand(Hand handData)
    {
        RemoveHand(handData.Name);
    }

    public void RegisterHand(Hand handData)
    {
        RegisterHand(handData.Name, handData.Location, handData.HeldEntity);
    }

    public void UpdateHand(Hand handData)
    {
        _hands[handData.Name].HeldItem = handData.HeldEntity;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var handData in _hands)
        {
            _itemSlotManager.UpdateCooldown(handData.Value, handData.Value.HeldItem);
        }
    }

    public void LoadHands(HandsComponent component)
    {
        _playerHands = component;
        foreach (var handData in _playerHands.Hands)
        {
            RegisterHand(handData.Value);
        }
    }

    public void UnloadHands()
    {
        foreach (var handData in _hands.Values)
        {
            handData.Dispose();
        }
        _hands.Clear();
        _playerHands = null;
    }

    // public struct HandData
    // {
    //     public string Name { get; }
    //     public HandLocation Location { get; }
    //
    //
    //     public HandData(string name, HandLocation location)
    //     {
    //         Name = name;
    //         Location = location;
    //     }
    // }
}
