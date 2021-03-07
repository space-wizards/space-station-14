#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.GameObjects.Components.Items;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        private readonly Texture _leftHandTexture;
        private readonly Texture _middleHandTexture;
        private readonly Texture _rightHandTexture;

        private readonly ItemStatusPanel _leftPanel;
        private readonly ItemStatusPanel _topPanel;
        private readonly ItemStatusPanel _rightPanel;

        private readonly HBoxContainer _guiContainer;
        private readonly VBoxContainer _handsColumn;
        private readonly HBoxContainer _handsContainer;

        private int _lastHands;

        /// <summary>
        ///     Last state sent by the client hands.
        ///     State has no hands if no state has been set yet.
        /// </summary>
        [ViewVariables]
        private HandsGuiState State { get; set; } = new();

        [ViewVariables]
        private Dictionary<GuiHand, HandButton> Hands { get; set; } = new();

        /// <summary>
        ///     The hands component that created this. Should only be used for sending network messages.
        /// </summary>
        private HandsComponent Creator { get; set; }

        public HandsGui(HandsComponent creator)
        {
            Creator = creator;

            IoCManager.InjectDependencies(this);

            AddChild(_guiContainer = new HBoxContainer
            {
                SeparationOverride = 0,
                Children =
                {
                    (_rightPanel = ItemStatusPanel.FromSide(HandLocation.Right)),
                    (_handsColumn = new VBoxContainer
                    {
                        Children =
                        {
                            (_topPanel = ItemStatusPanel.FromSide(HandLocation.Middle)),
                            (_handsContainer = new HBoxContainer())
                        }
                    }),
                    (_leftPanel = ItemStatusPanel.FromSide(HandLocation.Left))
                }
            });
            _leftHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            _middleHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            _rightHandTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_r.png");
        }

        public void SetState(HandsGuiState state)
        {
            State = state;
            Hands.Clear();
            foreach (var hand in state.GuiHands)
            {
                Hands.Add(hand, MakeHandbutton(hand.HandLocation));
            }

            //TODO: Update UI with new state
        }

        private Texture HandTexture(HandLocation location)
        {
            return location switch
            {
                HandLocation.Left => _leftHandTexture,
                HandLocation.Middle => _middleHandTexture,
                HandLocation.Right => _rightHandTexture,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private HandButton MakeHandbutton(HandLocation buttonLocation)
        {
            var buttonTexture = HandTexture(buttonLocation);
            var storageTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");
            var blockedTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");
            return new HandButton(buttonTexture, storageTexture, blockedTexture, buttonLocation);
        }

        private void RemoveHand(ClientHand hand)
        {
            var button = hand.Button;

            if (button != null)
            {
                _handsContainer.RemoveChild(button);
            }
        }

        private void UpdateHandIcons()
        {
            if (Parent == null)
            {
                return;
            }

            UpdateDraw();

            // TODO: Remove button on remove hand

            var hands = Creator.Hands.OrderByDescending(x => x.Location).ToArray();
            for (var i = 0; i < hands.Length; i++)
            {
                var hand = hands[i];

                if (hand.Button == null)
                {
                    AddHand(hand, hand.Location);
                }

                hand.Button!.Button.Texture = HandTexture(hand.Location);
                hand.Button!.SetPositionInParent(i);
                _itemSlotManager.SetItemSlot(hand.Button, hand.Entity);

                hand.Button!.SetActiveHand(State.ActiveHand == hand.Name);
            }

            _leftPanel.SetPositionFirst();
            _rightPanel.SetPositionLast();

            void AddHand(ClientHand hand, HandLocation buttonLocation)
            {
                var button = MakeHandbutton(buttonLocation);
                var slot = hand.Name;

                button.OnPressed += args => HandKeyBindDown(args, slot);
                button.OnStoragePressed += args => OnStoragePressed(args, slot);

                _handsContainer.AddChild(button);
                hand.Button = button;
            }
        }

        private void HandKeyBindDown(GUIBoundKeyEventArgs args, string slotName)
        {
            if (args.Function == ContentKeyFunctions.MouseMiddle)
            {
                Creator.SendChangeHand(slotName);
                args.Handle();
                return;
            }
            IEntity? entity = null; // Creator.GetEntity(slotName);
            if (entity == null)
            {
                if (args.Function == EngineKeyFunctions.UIClick && State.ActiveHand != slotName)
                {
                    Creator.SendChangeHand(slotName);
                    args.Handle();
                }
                return;
            }

            if (_itemSlotManager.OnButtonPressed(args, entity))
            {
                args.Handle();
                return;
            }

            if (args.Function == EngineKeyFunctions.UIClick)
            {
                if (State.ActiveHand == slotName)
                    Creator.UseActiveHand();
                else
                    Creator.AttackByInHand(slotName);
                args.Handle();
            }
        }

        private void OnStoragePressed(GUIBoundKeyEventArgs args, string handIndex)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            Creator.ActivateItemInHand(handIndex);
        }

        //per-frame update of hands
        private void UpdatePanels()
        {
            foreach (var hand in Creator.Hands)
            {
                //_itemSlotManager.UpdateCooldown(hand.Button, hand.Entity);
            }

            var hands = State.GuiHands;

            switch (hands.Count)
            {
                case var n when n == 0 && _lastHands != 0:
                    _guiContainer.Visible = false;

                    _topPanel.Update(null);
                    _leftPanel.Update(null);
                    _rightPanel.Update(null);

                    break;
                case 1:
                    if (_lastHands != 1)
                    {
                        _guiContainer.Visible = true;

                        _topPanel.Update(null);
                        _topPanel.Visible = false;

                        _leftPanel.Update(null);
                        _leftPanel.Visible = false;

                        _rightPanel.Visible = true;

                        if (!_guiContainer.Children.Contains(_rightPanel))
                        {
                            _rightPanel.AddChild(_rightPanel);
                            _rightPanel.SetPositionFirst();
                        }
                    }

                    _rightPanel.Update(hands[0].HeldItem);

                    break;
                case 2:
                    if (_lastHands != 2)
                    {
                        _guiContainer.Visible = true;
                        _topPanel.Update(null);
                        _topPanel.Visible = false;

                        _leftPanel.Visible = true;
                        _rightPanel.Visible = true;

                        if (_handsColumn.Children.Contains(_topPanel))
                        {
                            _handsColumn.RemoveChild(_topPanel);
                        }
                    }

                    _leftPanel.Update(hands[0].HeldItem);
                    _rightPanel.Update(hands[1].HeldItem);

                    // Order is left, right
                    foreach (var hand in State.GuiHands)
                    {
                        var tooltip = GetItemPanel(hand.HandLocation);
                        tooltip.Update(hand.HeldItem);
                    }

                    break;
                case var n when n > 2:
                    if (_lastHands <= 2)
                    {
                        _guiContainer.Visible = true;

                        _topPanel.Visible = true;
                        _leftPanel.Visible = false;
                        _rightPanel.Visible = false;

                        if (!_handsColumn.Children.Contains(_topPanel))
                        {
                            _handsColumn.AddChild(_topPanel);
                            _topPanel.SetPositionFirst();
                        }
                    }

                    //_topPanel.Update(Creator.ActiveHand);
                    _leftPanel.Update(null);
                    _rightPanel.Update(null);

                    break;
            }

            _lastHands = hands.Count;

            ItemStatusPanel GetItemPanel(HandLocation handLocation)
            {
                return handLocation switch
                {
                    HandLocation.Left => _rightPanel,
                    HandLocation.Middle => _topPanel,
                    HandLocation.Right => _leftPanel,
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
        }
    }

    /// <summary>
    ///     Info on a set of hands to be displayed.
    /// </summary>
    public class HandsGuiState
    {
        /// <summary>
        ///     The set of hands to be displayed.
        /// </summary>
        [ViewVariables]
        public List<GuiHand> GuiHands { get; } = new();

        /// <summary>
        ///     The name of the hand that is currently selected by this player.
        /// </summary>
        [ViewVariables]
        public string? ActiveHand { get; }

        public HandsGuiState() { }

        public HandsGuiState(List<GuiHand> guiHands, string? activeHand)
        {
            GuiHands = guiHands;
            ActiveHand = activeHand;
        }
    }

    /// <summary>
    ///     Info on an individual hand to be displayed.
    /// </summary>
    public class GuiHand
    {
        /// <summary>
        ///     The name of this hand.
        /// </summary>
        [ViewVariables]
        public string Name { get; }

        /// <summary>
        ///     Where this hand is located.
        /// </summary>
        [ViewVariables]
        public HandLocation HandLocation { get; }

        /// <summary>
        ///     The item being held in this hand.
        /// </summary>
        [ViewVariables]
        public IEntity? HeldItem { get; }

        public GuiHand(string name, HandLocation handLocation, IEntity? heldItem)
        {
            Name = name;
            HandLocation = handLocation;
            HeldItem = heldItem;
        }
    }
}
