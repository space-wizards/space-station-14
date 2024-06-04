using System.Numerics;
using Content.Client.Cooldown;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls
{
    [Virtual]
    public abstract class SlotControl : Control, IEntityControl
    {
        public static int DefaultButtonSize = 64;

        public TextureRect ButtonRect { get; }
        public TextureRect BlockedRect { get; }
        public TextureRect HighlightRect { get; }
        public SpriteView HoverSpriteView { get; }
        public TextureButton StorageButton { get; }
        public CooldownGraphic CooldownDisplay { get; }

        private SpriteView SpriteView { get; }

        public EntityUid? Entity => SpriteView.Entity;

        private bool _slotNameSet;

        private string _slotName = "";
        public string SlotName
        {
            get => _slotName;
            set
            {
                //this auto registers the button with it's parent container when it's set
                if (_slotNameSet)
                {
                    Logger.Warning("Tried to set slotName after init for:" + Name);
                    return;
                }
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

        private string? _blockedTexturePath;
        public string? BlockedTexturePath
        {
            get => _blockedTexturePath;
            set
            {
                _blockedTexturePath = value;
                BlockedRect.Texture = Theme.ResolveTextureOrNull(_blockedTexturePath)?.Texture;
            }
        }

        private string? _buttonTexturePath;
        public string? ButtonTexturePath
        {
            get => _buttonTexturePath;
            set
            {
                _buttonTexturePath = value;
                UpdateButtonTexture();
            }
        }

        private string? _fullButtonTexturePath;
        public string? FullButtonTexturePath
        {
            get => _fullButtonTexturePath;
            set
            {
                _fullButtonTexturePath = value;
                UpdateButtonTexture();
            }
        }


        private string? _storageTexturePath;
        public string? StorageTexturePath
        {
            get => _buttonTexturePath;
            set
            {
                _storageTexturePath = value;
                StorageButton.TextureNormal = Theme.ResolveTextureOrNull(_storageTexturePath)?.Texture;
            }
        }

        private string? _highlightTexturePath;
        public string? HighlightTexturePath
        {
            get => _highlightTexturePath;
            set
            {
                _highlightTexturePath = value;
                HighlightRect.Texture = Theme.ResolveTextureOrNull(_highlightTexturePath)?.Texture;
            }
        }

        public event Action<GUIBoundKeyEventArgs, SlotControl>? Pressed;
        public event Action<GUIBoundKeyEventArgs, SlotControl>? Unpressed;
        public event Action<GUIBoundKeyEventArgs, SlotControl>? StoragePressed;
        public event Action<GUIMouseHoverEventArgs, SlotControl>? Hover;

        public bool EntityHover => HoverSpriteView.Sprite != null;
        public bool MouseIsHovering;

        public SlotControl()
        {
            IoCManager.InjectDependencies(this);
            Name = "SlotButton_null";
            MinSize = new Vector2(DefaultButtonSize, DefaultButtonSize);
            AddChild(ButtonRect = new TextureRect
            {
                TextureScale = new Vector2(2, 2),
                MouseFilter = MouseFilterMode.Stop
            });
            AddChild(HighlightRect = new TextureRect
            {
                Visible = false,
                TextureScale = new Vector2(2, 2),
                MouseFilter = MouseFilterMode.Ignore
            });

            ButtonRect.OnKeyBindDown += OnButtonPressed;
            ButtonRect.OnKeyBindUp += OnButtonUnpressed;

            AddChild(SpriteView = new SpriteView
            {
                Scale = new Vector2(2, 2),
                SetSize = new Vector2(DefaultButtonSize, DefaultButtonSize),
                OverrideDirection = Direction.South
            });

            AddChild(HoverSpriteView = new SpriteView
            {
                Scale = new Vector2(2, 2),
                SetSize = new Vector2(DefaultButtonSize, DefaultButtonSize),
                OverrideDirection = Direction.South
            });

            AddChild(StorageButton = new TextureButton
            {
                Scale = new Vector2(0.75f, 0.75f),
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

            ButtonRect.OnMouseEntered += _ =>
            {
                MouseIsHovering = true;
            };
            ButtonRect.OnMouseEntered += OnButtonHover;

            ButtonRect.OnMouseExited += _ =>
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
                TextureScale = new Vector2(2, 2),
                MouseFilter = MouseFilterMode.Stop,
                Visible = false
            });

            HighlightTexturePath = "slot_highlight";
            BlockedTexturePath = "blocked";
        }

        public void ClearHover()
        {
            if (!EntityHover)
                return;

            var tempQualifier = HoverSpriteView.Entity;
            if (tempQualifier != null)
            {
                IoCManager.Resolve<IEntityManager>().QueueDeleteEntity(tempQualifier);
            }

            HoverSpriteView.SetEntity(null);
        }

        public void SetEntity(EntityUid? ent)
        {
            SpriteView.SetEntity(ent);
            UpdateButtonTexture();
        }

        private void UpdateButtonTexture()
        {
            var fullTexture = Theme.ResolveTextureOrNull(_fullButtonTexturePath);
            var texture = Entity.HasValue && fullTexture != null
                ? fullTexture.Texture
                : Theme.ResolveTextureOrNull(_buttonTexturePath)?.Texture;
            ButtonRect.Texture = texture;
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            Pressed?.Invoke(args, this);
        }

        private void OnButtonUnpressed(GUIBoundKeyEventArgs args)
        {
            Unpressed?.Invoke(args, this);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                StoragePressed?.Invoke(args.Event, this);
            }
            else
            {
                Pressed?.Invoke(args.Event, this);
            }
        }

        private void OnButtonHover(GUIMouseHoverEventArgs args)
        {
            Hover?.Invoke(args, this);
        }

        protected override void OnThemeUpdated()
        {
            base.OnThemeUpdated();

            StorageButton.TextureNormal = Theme.ResolveTextureOrNull(_storageTexturePath)?.Texture;
            HighlightRect.Texture = Theme.ResolveTextureOrNull(_highlightTexturePath)?.Texture;
            UpdateButtonTexture();
        }

        EntityUid? IEntityControl.UiEntity => Entity;
    }
}
