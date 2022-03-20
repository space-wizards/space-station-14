using Content.Client.Hands;
using Content.Client.Items.Managers;
using Content.Shared.Hands.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls
{
    public sealed class HandControl : ItemSlotButton
    {
        public TextureRect Blocked { get; }
        public Texture BlockedTexture => Theme.ResolveTexture(BlockedTextureFile);
        private const string BlockedTextureFile = "blocked.png";
        private const string StorageTextureFile = "back.png";
        private const string LeftHandTextureFile = "hand_l.png";
        private const string MidHandTextureFile = "hand.png";
        private const string RightHandTextureFile = "hand_R.png";
        private const string HighlightTextureFile = "slot_highlight.png";
        private readonly IItemSlotManager _itemSlotManager;
        private readonly IEntityManager _entManager;
        private readonly HandsContainer _parent;
        private HandLocation _location;
        private EntityUid? _heldItem;
        public bool Active
        {
            get => HighlightRect.Visible;
            set => Highlight(value);
        }
        public EntityUid? HeldItem
        {
            get => _heldItem;
            set
            {
                if (_heldItem == value) return;
                _heldItem = value;
                _itemSlotManager.SetItemSlot(this, _heldItem);
                //UpdateSlotHighlighted();
                UpdateBlockedState();
            }
        }
        public HandLocation Location { get => _location;
            set
            {
                _location = value;
                UpdateHandIcon(value);
            } }
        public HandControl(HandsContainer parent,HandLocation location,IEntityManager entManager, IItemSlotManager slotManager, EntityUid? heldItem,string handName)
        {
            _entManager = entManager;
            _itemSlotManager = slotManager;
            HighlightOverride = true;
            AddChild(Blocked = new TextureRect
            {
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });
            _parent = parent;
            HighlightRect.Texture = Theme.ResolveTexture(HighlightTextureFile);
            Blocked.Texture = Theme.ResolveTexture(BlockedTextureFile);
            StorageButton.TextureNormal = Theme.ResolveTexture(StorageTextureFile);
            UpdateHandIcon(location);
            Name = handName + "_Hand";
            HeldItem = heldItem;
            if (HeldItem != null)_itemSlotManager.SetItemSlot(this, _heldItem);
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

            _location = location;
        }

        private void UpdateBlockedState()
        {
            Blocked.Visible = HeldItem != null &&  _entManager.HasComponent<HandVirtualItemComponent>(HeldItem.Value);
        }

        public override void UpdateTheme(HudTheme newTheme)
        {
            base.UpdateTheme(newTheme);
            Blocked.Texture = Theme.ResolveTexture(BlockedTextureFile);
            HighlightRect.Texture = Theme.ResolveTexture(HighlightTextureFile);
            UpdateHandIcon(Location);
        }
    }
}
