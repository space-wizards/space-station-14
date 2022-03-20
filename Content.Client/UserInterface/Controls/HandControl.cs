using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Shared.Hands.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls
{
    [Virtual]
    public class HandControl : ItemSlotButton
    {
        public TextureRect Blocked { get; }
        public Texture BlockedTexture => Theme.ResolveTexture(BlockedTextureFile);
        private const string BlockedTextureFile = "blocked.png";
        private const string StorageTextureFile = "back.png";
        private const string LeftHandTextureFile = "hand_l.png";
        private const string MidHandTextureFile = "hand_l.png";
        private const string RightHandTextureFile = "hand_l.png";
        private readonly IItemSlotManager _itemSlotManager;
        private readonly IEntityManager _entManager;
        private bool _activeHand;
        private bool _highlighted;
        private readonly HandsContainer _parent;
        private HandLocation _location;
        private EntityUid? _heldItem;

        public EntityUid? HeldItem
        {
            get => _heldItem;
            set
            {
                if (_heldItem == value) return;
                _heldItem = value;
                _itemSlotManager.SetItemSlot(this, _heldItem);
                UpdateBlockedState();
            }
        }
        public bool Active
        {
            get => _activeHand;
            set
            {
                _activeHand = value;
                if (value)
                {
                    if (_parent.ActiveHand != null) _parent.ActiveHand.Active = false;
                    _parent.ActiveHand = this;
                }
                UpdateHighlight();
            }
        }
        public HandLocation Location { get => _location;
            set
            {
                _location = value;
                UpdateHandIcon(value);
            } }
        public HandControl(HandsContainer parent,HandLocation location,IEntityManager entManager, IItemSlotManager slotManager)
        {
            _entManager = entManager;
            _itemSlotManager = slotManager;
            AddChild(Blocked = new TextureRect
            {
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });
            _parent = parent;
            Blocked.Texture = Theme.ResolveTexture(BlockedTextureFile);
            StorageButton.TextureNormal = Theme.ResolveTexture(StorageTextureFile);
            UpdateHandIcon(location);
            UpdateBlockedState();
        }

        public override void Highlight(bool highlight)
        {
            _highlighted = highlight;
            UpdateHighlight();
        }

        public void UpdateHandIcon(HandLocation location)
        {
            switch (location)
            {
                case HandLocation.Left:
                {
                    Button.Texture = Theme.ResolveTexture(LeftHandTextureFile);
                    break;
                }

                case HandLocation.Middle:
                {
                    Button.Texture = Theme.ResolveTexture(MidHandTextureFile);
                    break;
                }
                case HandLocation.Right:
                {
                    Button.Texture = Theme.ResolveTexture(RightHandTextureFile);
                    break;
                }
            }

            Location = location;
        }

        private void UpdateBlockedState()
        {
            Blocked.Visible = HeldItem != null &&  _entManager.HasComponent<HandVirtualItemComponent>(HeldItem.Value);
        }

        private void UpdateHighlight()
        {
            // always stay highlighted if active
            base.Highlight(_activeHand || _highlighted);
        }
        public override void UpdateTheme(HudTheme newTheme)
        {
            base.UpdateTheme(newTheme);
            Blocked.Texture = Theme.ResolveTexture(BlockedTextureFile);
            UpdateHandIcon(Location);
        }
    }
}
