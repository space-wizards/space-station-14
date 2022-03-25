using Content.Client.Cooldown;
using Content.Client.HUD;
using Content.Client.Inventory;
using Content.Client.Items.Managers;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Containers;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls
{
    [Virtual]
    public class ItemSlotButton : Control, IEntityEventSubscriber, IHasHudTheme
    {
        private const string HighlightShader = "SelectionOutlineInrange";

        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        public EntityUid? Entity { get; set; }
        public TextureRect Button { get; }
        public TextureRect HighlightRect { get; }
        public SpriteView SpriteView { get; }
        public SpriteView HoverSpriteView { get; }
        public TextureButton StorageButton { get; }
        public CooldownGraphic CooldownDisplay { get; }
        public bool HighlightOverride { get; set; }

        public Texture ButtonTexture => Theme.ResolveTexture(ButtonTexturePath);
        private string _buttonTexturePath = "";
        public string ButtonTexturePath { get => _buttonTexturePath; set
        {
            _buttonTexturePath = value;
            Button.Texture = Theme.ResolveTexture(_buttonTexturePath);
        } }
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


        public Action<GUIBoundKeyEventArgs>? OnPressed { get; set; }
        public Action<GUIBoundKeyEventArgs>? OnStoragePressed { get; set; }
        public Action<GUIMouseHoverEventArgs>? OnHover { get; set; }

        public bool EntityHover => HoverSpriteView.Sprite != null;
        public bool MouseIsHovering;

        public ItemSlotButton()
        {
            IoCManager.InjectDependencies(this);
            Theme = HudThemes.DefaultTheme;
            MinSize = (ClientInventorySystem.ButtonSize, ClientInventorySystem.ButtonSize);
            HighlightOverride = false;
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
        }
        protected override void EnteredTree()
        {
            base.EnteredTree();

            _itemSlotManager.EntityHighlightedUpdated += HandleEntitySlotHighlighted;
            UpdateSlotHighlighted();
        }

        public void UpdateSprite(SpriteComponent? sprite)
        {
            SpriteView.Sprite = sprite;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _itemSlotManager.EntityHighlightedUpdated -= HandleEntitySlotHighlighted;
        }

        private void HandleEntitySlotHighlighted(EntitySlotHighlightedEventArgs entitySlotHighlightedEventArgs)
        {
            UpdateSlotHighlighted();
        }

        public void UpdateSlotHighlighted()
        {
            internal_highlight(_itemSlotManager.IsHighlighted(Entity));
        }

        public void ClearHover()
        {
            if (EntityHover)
            {
                ISpriteComponent? tempQualifier = HoverSpriteView.Sprite;
                if (tempQualifier != null)
                {
                    IoCManager.Resolve<IEntityManager>().DeleteEntity(tempQualifier.Owner);
                }

                HoverSpriteView.Sprite = null;
            }
        }

        private void internal_highlight(bool highLight)
        {
            if (HighlightOverride) return; //do not use internal highlighting if override is enabled
            HighlightRect.Visible = highLight;
        }

        public void Highlight(bool highlight)
        {
            HighlightRect.Visible = highlight;
        }

        private void OnButtonPressed(GUIBoundKeyEventArgs args)
        {
            OnPressed?.Invoke(args);
        }

        private void OnStorageButtonPressed(BaseButton.ButtonEventArgs args)
        {
            if (args.Event.Function == EngineKeyFunctions.UIClick)
            {
                OnStoragePressed?.Invoke(args.Event);
            }
            else
            {
                OnPressed?.Invoke(args.Event);
            }
        }

        private void OnButtonHover(GUIMouseHoverEventArgs args)
        {
            OnHover?.Invoke(args);
        }

        public HudTheme Theme { get; set; }
        public virtual void UpdateTheme(HudTheme newTheme)
        {
            StorageButton.TextureNormal = Theme.ResolveTexture(_storageTexturePath);
            Button.Texture = Theme.ResolveTexture(_buttonTexturePath);
        }
    }
}
