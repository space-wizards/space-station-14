using Content.Client.GameObjects;
using Content.Client.Interfaces.GameObjects;
using Content.Client.Utility;
using Content.Shared.Input;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface
{
    public class HandsGui : Control
    {
        private const string HandNameLeft = "left";
        private const string HandNameRight = "right";

#pragma warning disable 0649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IItemSlotManager _itemSlotManager;
#pragma warning restore 0649

        private IEntity _leftHand;
        private IEntity _rightHand;

        private readonly TextureRect ActiveHandRect;

        private readonly ItemSlotButton _leftButton;
        private readonly ItemSlotButton _rightButton;

        private readonly ItemStatusPanel _rightStatusPanel;
        private readonly ItemStatusPanel _leftStatusPanel;

        public HandsGui()
        {
            IoCManager.InjectDependencies(this);

            var textureHandLeft = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_l.png");
            var textureHandRight = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_r.png");
            var textureHandActive = _resourceCache.GetTexture("/Textures/Interface/Inventory/hand_active.png");
            var storageTexture = _resourceCache.GetTexture("/Textures/Interface/Inventory/back.png");

            _rightStatusPanel = new ItemStatusPanel(true);
            _leftStatusPanel = new ItemStatusPanel(false);

            _leftButton = new ItemSlotButton(textureHandLeft, storageTexture);
            _rightButton = new ItemSlotButton(textureHandRight, storageTexture);
            var hBox = new HBoxContainer
            {
                SeparationOverride = 0,
                Children = {_rightStatusPanel, _rightButton, _leftButton, _leftStatusPanel}
            };

            AddChild(hBox);

            _leftButton.OnPressed += args => HandKeyBindDown(args, HandNameLeft);
            _leftButton.OnStoragePressed += args => _OnStoragePressed(args, HandNameLeft);
            _rightButton.OnPressed += args => HandKeyBindDown(args, HandNameRight);
            _rightButton.OnStoragePressed += args => _OnStoragePressed(args, HandNameRight);

            // Active hand
            _leftButton.AddChild(ActiveHandRect = new TextureRect
            {
                Texture = textureHandActive,
                TextureScale = (2, 2)
            });
        }

        /// <summary>
        /// Gets the hands component controling this gui, returns true if successful and false if failure
        /// </summary>
        /// <param name="hands"></param>
        /// <returns></returns>
        private bool TryGetHands(out IHandsComponent hands)
        {
            hands = default;

            var entity = _playerManager?.LocalPlayer?.ControlledEntity;
            return entity != null && entity.TryGetComponent(out hands);
        }

        public void UpdateHandIcons()
        {
            if (Parent == null)
            {
                return;
            }

            UpdateDraw();

            if (!TryGetHands(out var hands))
                return;

            var left = hands.GetEntity(HandNameLeft);
            var right = hands.GetEntity(HandNameRight);

            ActiveHandRect.Parent.RemoveChild(ActiveHandRect);
            var parent = hands.ActiveIndex == HandNameLeft ? _leftButton : _rightButton;
            parent.AddChild(ActiveHandRect);
            ActiveHandRect.SetPositionInParent(1);

            if (left != _leftHand)
            {
                _leftHand = left;
                _itemSlotManager.SetItemSlot(_leftButton, _leftHand);
            }

            if (right != _rightHand)
            {
                _rightHand = right;
                _itemSlotManager.SetItemSlot(_rightButton, _rightHand);
            }
        }

        private void HandKeyBindDown(GUIBoundKeyEventArgs args, string handIndex)
        {
            if (!TryGetHands(out var hands))
                return;

            if (args.Function == ContentKeyFunctions.MouseMiddle)
            {
                hands.SendChangeHand(handIndex);
                args.Handle();
                return;
            }

            var entity = hands.GetEntity(handIndex);
            if (entity == null)
            {
                if (args.Function == EngineKeyFunctions.UIClick && hands.ActiveIndex != handIndex)
                {
                    hands.SendChangeHand(handIndex);
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
                if (hands.ActiveIndex == handIndex)
                {
                    hands.UseActiveHand();
                }
                else
                {
                    hands.AttackByInHand(handIndex);
                }
                args.Handle();
                return;
            }
        }

        private void _OnStoragePressed(GUIBoundKeyEventArgs args, string handIndex)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;
            if (!TryGetHands(out var hands))
                return;
            hands.ActivateItemInHand(handIndex);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _itemSlotManager.UpdateCooldown(_leftButton, _leftHand);
            _itemSlotManager.UpdateCooldown(_rightButton, _rightHand);

            _rightStatusPanel.Update(_rightHand);
            _leftStatusPanel.Update(_leftHand);
        }
    }
}
