using Content.Client.Cooldown;
using Content.Client.HUD;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls
{
    [Virtual]
    public abstract class ItemSlotControl : Control, IEntityEventSubscriber, IHasHudTheme
    {
        private const string HighlightShader = "SelectionOutlineInrange";

        public TextureRect Button { get; }
        public TextureRect BlockedRect { get; }
        public TextureRect HighlightRect { get; }
        public SpriteView SpriteView { get; }
        public SpriteView HoverSpriteView { get; }
        public TextureButton StorageButton { get; }
        public CooldownGraphic CooldownDisplay { get; }
        private string _slotName = "";
        private bool _slotNameSet;
        public string SlotName
        {
            get => _slotName;
            set
            {//this auto registers the button with it's parent container when it's set
                if (_slotNameSet)
                {
                    Logger.Warning("Tried to set slotName after init for:" + Name);
                    return;
                };
                _slotNameSet = true;
                if (Parent is IItemslotUIContainer container)
                {
                    container.TryRegisterButton(this, value);
                }
                Name = "SlotButton_" + value;
                _slotName = value;
            }
        }

        public bool Highlight { get => HighlightRect.Visible; set => HighlightRect.Visible = value;}
        public bool Blocked { get => BlockedRect.Visible; set => BlockedRect.Visible = value;}
        public Texture BlockedTexture => Theme.ResolveTexture(BlockedTexturePath);
        private string _blockedTexturePath = "";
        public string BlockedTexturePath
        {
            get => _blockedTexturePath;
            set
            {
                _blockedTexturePath = value;
                BlockedRect.Texture = Theme.ResolveTexture(_blockedTexturePath);
            }
        }

        public Texture ButtonTexture => Theme.ResolveTexture(ButtonTexturePath);
        private string _buttonTexturePath = "";
        public string ButtonTexturePath {
            get => _buttonTexturePath;
            set
            {
                _buttonTexturePath = value;
                Button.Texture = Theme.ResolveTexture(_buttonTexturePath);
            }
        }
        public Texture StorageTexture => Theme.ResolveTexture(StorageTexturePath);
        private string _storageTexturePath = "";
        public string StorageTexturePath
        {
            get => _buttonTexturePath;
            set
            {
                _storageTexturePath = value;
                StorageButton.TextureNormal = Theme.ResolveTexture(_storageTexturePath);
            }
        }

        private string _highlightTexturePath = "";
        public string HighlightTexturePath
        {
            get => _highlightTexturePath;
            set
            {
                _highlightTexturePath = value;
                HighlightRect.Texture = Theme.ResolveTexture(_highlightTexturePath);
            }
        }
        public Action<GUIBoundKeyEventArgs, ItemSlotControl>? OnPressed { get; set; }
        public Action<GUIBoundKeyEventArgs, ItemSlotControl>? OnStoragePressed { get; set; }
        public Action<GUIMouseHoverEventArgs, ItemSlotControl>? OnHover { get; set; }

        public bool EntityHover => HoverSpriteView.Sprite != null;
        public bool MouseIsHovering;

        public ItemSlotControl()
        {
            IoCManager.InjectDependencies(this);
            Name = "SlotButton_null";
            Theme = HudThemes.DefaultTheme;
            MinSize = (64, 64);
            AddChild(Button = new TextureRect
            {
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop
            });
            AddChild(HighlightRect = new TextureRect()
            {
                Visible = false,
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Ignore
            });

            Button.OnKeyBindDown += OnButtonPressed;

            AddChild(SpriteView = new SpriteView
            {
                Scale = (2, 2),
                OverrideDirection = Direction.South
            });

            AddChild(HoverSpriteView = new SpriteView
            {
                Scale = (2, 2),
                OverrideDirection = Direction.South
            });

            AddChild(StorageButton = new TextureButton
            {
                Scale = (0.75f, 0.75f),
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom,
                Visible = false,
            });

            StorageButton.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIClick)
                {
                    OnButtonPressed(args);
                }
            };

            StorageButton.OnPressed += OnStorageButtonPressed;

            Button.OnMouseEntered += _ =>
            {
                MouseIsHovering = true;
            };
            Button.OnMouseEntered += OnButtonHover;

            Button.OnMouseExited += _ =>
            {
                MouseIsHovering = false;
                ClearHover();
            };

            AddChild(CooldownDisplay = new CooldownGraphic
            {
                Visible = false,
            });

            AddChild(BlockedRect = new TextureRect
            {
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });

            HighlightTexturePath = "slot_highlight.png";
            BlockedTexturePath = "blocked.png";
        }

        public void ClearHover()
        {
            if (!EntityHover) return;
            ISpriteComponent? tempQualifier = HoverSpriteView.Sprite;
            if (tempQualifier != null)
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(tempQualifier.Owner);
            }
            HoverSpriteView.Sprite = null;
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            OnPressed?.Invoke(args, this);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                OnStoragePressed?.Invoke(args.Event, this);
            }
            else
            {
                OnPressed?.Invoke(args.Event, this);
            }
        }

        private void OnButtonHover(GUIMouseHoverEventArgs args)
        {
            OnHover?.Invoke(args, this);
        }

        public HudTheme Theme { get; set; }
        public virtual void UpdateTheme(HudTheme newTheme)
        {
            StorageButton.TextureNormal = Theme.ResolveTexture(_storageTexturePath);
            Button.Texture = Theme.ResolveTexture(_buttonTexturePath);
            HighlightRect.Texture = Theme.ResolveTexture(_highlightTexturePath);
        }
    }
}
