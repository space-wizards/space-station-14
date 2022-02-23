using System;
using Content.Client.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Utility;

namespace Content.Client.Actions.UI
{
    /// <summary>
    /// An individual action visible in the action menu.
    /// </summary>
    public sealed class ActionMenuItem : ContainerButton
    {
        // shorter than default tooltip delay so user can
        // quickly explore what each action is
        private const float CustomTooltipDelay = 0.2f;

        public ActionType Action;

        private Action<ActionMenuItem> _onControlFocusExited;

        public ActionMenuItem(ActionType action, Action<ActionMenuItem> onControlFocusExited)
        {
            Action = action;
            _onControlFocusExited = onControlFocusExited;

            MinSize = (64, 64);
            VerticalAlignment = VAlignment.Top;

            AddChild(new TextureRect
            {
                HorizontalExpand = true,
                VerticalExpand = true,
                Stretch = TextureRect.StretchMode.Scale,
                Texture = action.Icon?.Frame0()
            });

            TooltipDelay = CustomTooltipDelay;
            TooltipSupplier = SupplyTooltip;
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

        internal void SetActionState(object enabled)
        {
            throw new NotImplementedException();
        }
    }
}
