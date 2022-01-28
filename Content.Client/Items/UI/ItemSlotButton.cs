using System;
using Content.Client.Cooldown;
using Content.Client.HUD;
using Content.Client.Items.Managers;
using Content.Client.Stylesheets;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Items.UI
{
    public class ItemSlotButton : Control, IEntityEventSubscriber
    {
        private const string HighlightShader = "SelectionOutlineInrange";

        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;

        public EntityUid? Entity { get; set; }
        public TextureRect Button { get; }
        public SpriteView SpriteView { get; }
        public SpriteView HoverSpriteView { get; }
        public TextureButton StorageButton { get; }
        public CooldownGraphic CooldownDisplay { get; }

        public Action<GUIBoundKeyEventArgs>? OnPressed { get; set; }
        public Action<GUIBoundKeyEventArgs>? OnStoragePressed { get; set; }
        public Action<GUIMouseHoverEventArgs>? OnHover { get; set; }

        public bool EntityHover => HoverSpriteView.Sprite != null;
        public bool MouseIsHovering;

        private readonly PanelContainer _highlightRect;

        private string _textureName;
        private string _storageTextureName;

        public ItemSlotButton(int size, string textureName, string storageTextureName, IGameHud gameHud)
        {
            _textureName = textureName;
            _storageTextureName = storageTextureName;
            IoCManager.InjectDependencies(this);

            MinSize = (size, size);

            AddChild(Button = new TextureRect
            {
                TextureScale = (2, 2),
                MouseFilter = MouseFilterMode.Stop
            });

            AddChild(_highlightRect = new PanelContainer
            {
                StyleClasses = { StyleNano.StyleClassHandSlotHighlight },
                MinSize = (32, 32),
                Visible = false
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

            RefreshTextures(gameHud);
        }

        public void RefreshTextures(IGameHud gameHud)
        {
            Button.Texture = gameHud.GetHudTexture(_textureName);
            StorageButton.TextureNormal = gameHud.GetHudTexture(_storageTextureName);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _itemSlotManager.EntityHighlightedUpdated += HandleEntitySlotHighlighted;
            UpdateSlotHighlighted();
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
            Highlight(_itemSlotManager.IsHighlighted(Entity));
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

        public virtual void Highlight(bool highlight)
        {
            if (highlight)
            {
                _highlightRect.Visible = true;
            }
            else
            {
                _highlightRect.Visible = false;
            }
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
    }
}
