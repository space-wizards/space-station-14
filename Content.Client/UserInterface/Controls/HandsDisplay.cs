using System.Linq;
using Content.Client.Hands;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Controls;

//
// public sealed class HandsContainer : Control
// {
//     private readonly GridContainer _grid;
//
//     private readonly List<HandControl> _handControls = new();
//     public int ColumnLimit { get => _grid.Columns; set => _grid.Columns = value; }
//     public HandsContainer()
//     {
//         AddChild(_grid = new GridContainer());
//     }
//     public void AddHandControl(HandControl hand)
//     {
//         _handControls.Add(hand);
//         _grid.AddChild(hand);
//     }
//     public void RemoveHandControl(HandControl hand)
//     {
//         _handControls.Remove(hand);
//         _grid.RemoveChild(hand);
//     }
//
//     public bool TryGetLastHand(out HandControl? control)
//     {
//         if (_handControls.Count == 0)
//         {
//             control = null;
//             return false;
//         }
//         control = _handControls.Last();
//         return true;
//     }
//
//     public bool TryRemoveLastHand(out HandControl? control)
//     {
//         var success = TryGetLastHand(out control);
//         if (control != null) RemoveHandControl(control);
//         return success;
//     }
//
//     public int HandCount => _grid.ChildCount;
//
// }
//
//
//
//
// public sealed class HandsDisplay : Control
// {
//     [Dependency] private readonly IEntityManager _entityManager = default!;
//     [Dependency] private readonly IEntitySystemManager _systemManager = default!;
//     private HandsComponent? HandsComponent => _playerHands;
//     private readonly HandsSystem _handsSystem;
//     private HandsComponent? _playerHands = null;
//     public List<HandControl> Hands => _hands.Values.ToList();
//     private readonly Dictionary<string, HandControl> _hands = new();
//     private readonly GridContainer _grid;
//     private readonly List<HandDataPanel> _handDataPanels = new();
//     private HandControl? _activeHand = null;
//     public HandControl? ActiveHand => _activeHand;
//     public HandsContainer? ContainerExtension { get; set; }
//     public int MaxHandCount { get; set; } = -1;
//     public int Columns { get => _grid.Columns; set => _grid.Columns = value; }
//     public int? ColumnSeparation { get => _grid.VSeparationOverride; set => _grid.VSeparationOverride = value;}
//     public HandsDisplay()
//     {
//         IoCManager.InjectDependencies(this);
//         _handsSystem = _systemManager.GetEntitySystem<HandsSystem>();
//         _handsSystem.TryGetPlayerHands(out _playerHands);
//         AddChild(_grid = new GridContainer());
//         _grid.Columns = 4;
//         _grid.HorizontalExpand = false;
//         _grid.HorizontalAlignment = HAlignment.Center;
//     }
//     public bool TryGetHand(string name, out HandControl hand)
//     {
//         return _hands.TryGetValue(name, out hand!);
//     }
//
//     public bool TryGetActiveHand(out HandControl? activeHand)
//     {
//         activeHand = _activeHand;
//         return _activeHand != null;
//     }
//
//     private void OnHandPressed(GUIBoundKeyEventArgs args, string handName)
//     {
//         if (_playerHands == null) return;//not sure how this would be possible lol
//         if (args.Function == EngineKeyFunctions.UIClick)
//         {
//             _handsSystem.UIHandClick(_playerHands, handName);
//         }
//         else if (TryGetHand(handName, out var hand))
//         {
//             _itemSlotManager.OnButtonPressed(args, hand.HeldItem);
//         }
//     }
//     private void OnStoragePressed(string handName)
//     {
//         _handsSystem.UIHandActivate(handName);
//     }
//
//     private void RegisterHand(string name, HandLocation location, EntityUid? heldItem = null)
//     {
//         var newHand = new HandControl(this, location, _entityManager, _itemSlotManager, heldItem, name);
//         newHand.OnPressed += args => OnHandPressed(args, name);
//         newHand.OnStoragePressed += args => OnStoragePressed(name);
//         if (!_hands.TryAdd(name, newHand)) throw new Exception("Duplicate handName detected!: " + name);
//         AddHandToGui(newHand);
//     }
//
//     private void UpdateHandStatusPanels()
//     {
//         foreach (var dataPanel in _handDataPanels)
//         {
//             dataPanel.UpdateData(_activeHand);
//         }
//     }
//
//     public void RegisterHandDataPanel(HandDataPanel dataPanel)
//     {
//         _handDataPanels.Add(dataPanel);
//     }
//
//     public void SetActiveHand(SharedHandsComponent? handComp)
//     {
//         if (handComp == null) //if the hand component is null, disable active hand
//         {
//             if (_activeHand != null)
//             {
//                 _activeHand.Active = false;
//             }
//
//             UpdateHandStatusPanels();
//             return;
//         }
//
//         if (_activeHand != null) //clear the current active hand state
//         {
//             _activeHand.Active = false;
//         }
//
//         if (handComp.ActiveHand == null) {
//             _activeHand = null;
//         }
//         else
//         {
//             if (!_hands.TryGetValue(handComp.ActiveHand.Name, out var hand)) return;
//             hand.Active = true;
//             _activeHand = hand;
//         }
//
//         UpdateHandStatusPanels();
//     }
//
//     public void RemoveHand(string name)
//     {
//         RemoveHandFromGui(_hands[name]);
//         _hands[name].Dispose();
//         _hands.Remove(name);
//     }
//
//     public void RemoveHand(Hand handData)
//     {
//         RemoveHand(handData.Name);
//         SetActiveHand(HandsComponent);
//     }
//
//     public void RegisterHand(Hand handData)
//     {
//         RegisterHand(handData.Name, handData.Location, handData.HeldEntity);
//         SetActiveHand(HandsComponent);
//     }
//
//     public void UpdateHandGui(Hand handData)
//     {
//         _hands[handData.Name].HeldItem = handData.HeldEntity;
//         UpdateHandStatusPanels();
//     }
//
//     protected override void FrameUpdate(FrameEventArgs args)
//     {
//         base.FrameUpdate(args);
//
//         foreach (var handData in _hands)
//         {
//             _itemSlotManager.UpdateCooldown(handData.Value, handData.Value.HeldItem);
//         }
//     }
//
//     public void LoadHands(HandsComponent component)
//     {
//         _playerHands = component;
//         foreach (var handData in _playerHands.Hands)
//         {
//             Logger.Debug(handData.Key);
//             RegisterHand(handData.Value);
//         }
//     }
//
//     public void UnloadHands()
//     {
//         foreach (var handData in _hands.Values)
//         {
//             handData.Dispose();
//         }
//         _hands.Clear();
//         _playerHands = null;
//     }
//
//     private void AddHandToGui(HandControl control)
//     {
//         if (MaxHandCount == -1 || _grid.ChildCount < MaxHandCount)
//         {
//             _grid.AddChild(control);
//             return;
//         }
//         if (ContainerExtension == null) throw new Exception("Overflow container not found for hand");
//         ContainerExtension.AddHandControl(control);
//     }
//
//     private void RemoveHandFromGui(HandControl control)
//     {
//         if (_grid.Children.Contains(control))
//         {
//             _grid.RemoveChild(control);
//             if (ContainerExtension != null && ContainerExtension.TryRemoveLastHand(out var exHand))
//             {
//                 _grid.AddChild(exHand!);
//             }
//         }
//         else if (MaxHandCount != -1)
//         {
//             if (ContainerExtension == null) throw new Exception("Overflow container not found for hand");
//             ContainerExtension.RemoveHandControl(control);
//         }
//
//     }
//
// }
