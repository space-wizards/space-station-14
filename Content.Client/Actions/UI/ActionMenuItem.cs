using System;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Actions.UI
{
    // TODO merge this with action-slot when it gets XAMLd
    // this has way too much overlap, especially now that they both have the item-sprite icons.

    /// <summary>
    /// An individual action visible in the action menu.
    /// </summary>
    public sealed class ActionMenuItem : ContainerButton
    {
        // shorter than default tooltip delay so user can
        // quickly explore what each action is
        private const float CustomTooltipDelay = 0.2f;

        private readonly TextureRect _bigActionIcon;
        private readonly TextureRect _smallActionIcon;
        private readonly SpriteView _smallItemSpriteView;
        private readonly SpriteView _bigItemSpriteView;

        public ActionType Action;

        private Action<ActionMenuItem> _onControlFocusExited;

        private readonly ActionsUI _actionsUI;

        public ActionMenuItem(ActionsUI actionsUI, ActionType action, Action<ActionMenuItem> onControlFocusExited)
        {
            _actionsUI = actionsUI;
            Action = action;
            _onControlFocusExited = onControlFocusExited;

            MinSize = (64, 64);
            VerticalAlignment = VAlignment.Top;

            _bigActionIcon = new TextureRect
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            _bigItemSpriteView = new SpriteView
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                Scale = (2, 2),
                Visible = false,
                OverrideDirection = Direction.South,
            };
            _smallActionIcon = new TextureRect
            {
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom,
                Stretch = TextureRect.StretchMode.Scale,
                Visible = false
            };
            _smallItemSpriteView = new SpriteView
            {
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom,
                Visible = false,
                OverrideDirection = Direction.South,
            };

            // padding to the left of the small icon
            var paddingBoxItemIcon = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
                MinSize = (64, 64)
            };
            paddingBoxItemIcon.AddChild(new Control()
            {
                MinSize = (32, 32),
            });
            paddingBoxItemIcon.AddChild(new Control
            {
                Children =
                {
                    _smallActionIcon,
                    _smallItemSpriteView
                }
            });
            AddChild(_bigActionIcon);
            AddChild(_bigItemSpriteView);
            AddChild(paddingBoxItemIcon);

            TooltipDelay = CustomTooltipDelay;
            TooltipSupplier = SupplyTooltip;
            UpdateIcons();
        }


        public void UpdateIcons()
        {
            UpdateItemIcon();

            if (Action == null)
            {
                SetActionIcon(null);
                return;
            }

            if ((_actionsUI.SelectingTargetFor?.Action == Action || Action.Toggled) && Action.IconOn != null)
                SetActionIcon(Action.IconOn.Frame0());
            else
                SetActionIcon(Action.Icon?.Frame0());
        }

        private void SetActionIcon(Texture? texture)
        {
            if (texture == null || Action == null)
            {
                _bigActionIcon.Texture = null;
                _bigActionIcon.Visible = false;
                _smallActionIcon.Texture = null;
                _smallActionIcon.Visible = false;
            }
            else if (Action.Provider != null && Action.ItemIconStyle == ItemActionIconStyle.BigItem)
            {
                _smallActionIcon.Texture = texture;
                _smallActionIcon.Modulate = Action.IconColor;
                _smallActionIcon.Visible = true;
                _bigActionIcon.Texture = null;
                _bigActionIcon.Visible = false;
            }
            else
            {
                _bigActionIcon.Texture = texture;
                _bigActionIcon.Modulate = Action.IconColor;
                _bigActionIcon.Visible = true;
                _smallActionIcon.Texture = null;
                _smallActionIcon.Visible = false;
            }
        }

        private void UpdateItemIcon()
        {
            if (Action?.Provider == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent(Action.Provider.Value, out SpriteComponent sprite))
            {
                _bigItemSpriteView.Visible = false;
                _bigItemSpriteView.Sprite = null;
                _smallItemSpriteView.Visible = false;
                _smallItemSpriteView.Sprite = null;
            }
            else
            {
                switch (Action.ItemIconStyle)
                {
                    case ItemActionIconStyle.BigItem:
                        _bigItemSpriteView.Visible = true;
                        _bigItemSpriteView.Sprite = sprite;
                        _smallItemSpriteView.Visible = false;
                        _smallItemSpriteView.Sprite = null;
                        break;
                    case ItemActionIconStyle.BigAction:

                        _bigItemSpriteView.Visible = false;
                        _bigItemSpriteView.Sprite = null;
                        _smallItemSpriteView.Visible = true;
                        _smallItemSpriteView.Sprite = sprite;
                        break;

                    case ItemActionIconStyle.NoItem:

                        _bigItemSpriteView.Visible = false;
                        _bigItemSpriteView.Sprite = null;
                        _smallItemSpriteView.Visible = false;
                        _smallItemSpriteView.Sprite = null;
                        break;
                }
            }
        }

        protected override void ControlFocusExited()
        {
            base.ControlFocusExited();
            _onControlFocusExited.Invoke(this);
        }

        private Control SupplyTooltip(Control? sender)
        {
            var name = FormattedMessage.FromMarkupPermissive(Loc.GetString(Action.Name));
            var decr = FormattedMessage.FromMarkupPermissive(Loc.GetString(Action.Description));

            var tooltip = new ActionAlertTooltip(name, decr);

            if (Action.Enabled && (Action.Charges == null || Action.Charges != 0))
                tooltip.Cooldown = Action.Cooldown;

            return tooltip;
        }

        /// <summary>
        /// Change how this is displayed depending on if it is granted or revoked
        /// </summary>
        public void SetActionState(bool granted)
        {
            if (granted)
            {
                if (HasStyleClass(StyleNano.StyleClassActionMenuItemRevoked))
                {
                    RemoveStyleClass(StyleNano.StyleClassActionMenuItemRevoked);
                }
            }
            else
            {
                if (!HasStyleClass(StyleNano.StyleClassActionMenuItemRevoked))
                {
                    AddStyleClass(StyleNano.StyleClassActionMenuItemRevoked);
                }
            }
        }
    }
}
