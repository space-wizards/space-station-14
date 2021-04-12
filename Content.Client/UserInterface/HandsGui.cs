#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Utility;
using Content.Shared;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
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
        [Dependency] private readonly IGameHud _gameHud = default!;
        [Dependency] private readonly INetConfigurationManager _configManager = default!;

        private Texture StorageTexture => _gameHud.GetHudTexture("back.png");
        private Texture BlockedTexture => _resourceCache.GetTexture("/Textures/Interface/Inventory/blocked.png");

        private ItemStatusPanel StatusPanel { get; }

        private HBoxContainer HandsContainer { get; }

        [ViewVariables]
        public IReadOnlyList<GuiHand> Hands => _hands;
        private List<GuiHand> _hands = new();

        private string? ActiveHand { get; set; }

        public Action<HandClickEventArgs>? HandClick;

        public Action<HandActivateEventArgs>? HandActivate;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);
            _configManager.OnValueChanged(CCVars.HudTheme, UpdateHudTheme);

            AddChild(new HBoxContainer
            {
                SeparationOverride = 0,
                HorizontalAlignment = HAlignment.Center,
                Children =
                {
                    new VBoxContainer
                    {
                        Children =
                        {
                            (StatusPanel = ItemStatusPanel.FromSide(HandLocation.Middle)),
                            (HandsContainer = new HBoxContainer() { HorizontalAlignment = HAlignment.Center } ),
                        }
                    },
                }
            });
        }

        public void SetState(HandsGuiState state)
        {
            ActiveHand = state.ActiveHand;
            _hands = state.GuiHands;
            UpdateGui();
        }

        private void UpdateGui()
        {
            HandsContainer.DisposeAllChildren();

            foreach (var hand in _hands)
            {
                var newButton = MakeHandButton(hand.HandLocation);
                HandsContainer.AddChild(newButton);
                hand.HandButton = newButton;

                var handName = hand.Name;
                newButton.OnPressed += args => OnHandPressed(args, handName);
                newButton.OnStoragePressed += args => OnStoragePressed(handName);

                newButton.Blocked.Visible = !hand.Enabled;

                _itemSlotManager.SetItemSlot(newButton, hand.HeldItem);
            }
            if (TryGetActiveHand(out var activeHand))
            {
                activeHand.HandButton.SetActiveHand(true);
                StatusPanel.Update(activeHand.HeldItem);
            }
        }

        private void OnHandPressed(GUIBoundKeyEventArgs args, string handName)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                HandClick?.Invoke(new HandClickEventArgs(handName));
            }
            else if (TryGetHand(handName, out var hand))
            {
                _itemSlotManager.OnButtonPressed(args, hand.HeldItem);
            }
        }

        private void OnStoragePressed(string handName)
        {
            HandActivate?.Invoke(new HandActivateEventArgs(handName));
        }

        private bool TryGetActiveHand([NotNullWhen(true)] out GuiHand? activeHand)
        {
            TryGetHand(ActiveHand, out activeHand);
            return activeHand != null;
        }

        private bool TryGetHand(string? handName, [NotNullWhen(true)] out GuiHand? foundHand)
        {
            foundHand = null;

            if (handName == null)
                return false;

            foreach (var hand in _hands)
            {
                if (hand.Name == handName)
                    foundHand = hand;
            }
            return foundHand != null;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            foreach (var hand in _hands)
                _itemSlotManager.UpdateCooldown(hand.HandButton, hand.HeldItem);
        }

        private HandButton MakeHandButton(HandLocation buttonLocation)
        {
            var buttonTextureName = buttonLocation switch
            {
                HandLocation.Right => "hand_r.png",
                _ => "hand_l.png"
            };
            var buttonTexture = _gameHud.GetHudTexture(buttonTextureName);

            return new HandButton(buttonTexture, StorageTexture, buttonTextureName, BlockedTexture, buttonLocation);
        }

        private void UpdateHudTheme(int idx)
        {
            UpdateGui();
        }

        public class HandClickEventArgs
        {
            public string HandClicked { get; }

            public HandClickEventArgs(string handClicked)
            {
                HandClicked = handClicked;
            }
        }

        public class HandActivateEventArgs
        {
            public string HandUsed { get; }

            public HandActivateEventArgs(string handUsed)
            {
                HandUsed = handUsed;
            }
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
        ///     The name of the currently active hand.
        /// </summary>
        [ViewVariables]
        public string? ActiveHand { get; }

        public HandsGuiState(List<GuiHand> guiHands, string? activeHand = null)
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

        /// <summary>
        ///     The button in the gui associated with this hand. Assumed to be set by gui shortly after being received from the client HandsComponent.
        /// </summary>
        [ViewVariables]
        public HandButton HandButton { get; set; } = default!;

        /// <summary>
        ///     If this hand can be used by the player.
        /// </summary>
        [ViewVariables]
        public bool Enabled { get; }

        public GuiHand(string name, HandLocation handLocation, IEntity? heldItem, bool enabled)
        {
            Name = name;
            HandLocation = handLocation;
            HeldItem = heldItem;
            Enabled = enabled;
        }
    }
}
