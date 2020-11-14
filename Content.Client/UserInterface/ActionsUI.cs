using System;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The action hotbar on the left side of the screen.
    /// </summary>
    public sealed class ActionsUI : Control
    {
        private readonly VBoxContainer _vbox;
        private readonly ActionSlot[] _slots;
        private readonly EventHandler _onShowTooltip;
        private readonly EventHandler _onHideTooltip;
        private readonly Action<BaseButton.ButtonEventArgs> _onActionPressed;

        /// <param name="onShowTooltip">OnShowTooltip handler to assign to each action slot</param>
        /// <param name="onHideTooltip">OnHideTooltip handler to assign to each action slot</param>
        /// <param name="onActionPressed">OnPressed handler to assign to each action slot</param>
        /// <param name="resourceCache">resource cache to use to load action icon textures</param>
        public ActionsUI(EventHandler onShowTooltip, EventHandler onHideTooltip, Action<BaseButton.ButtonEventArgs> onActionPressed,
            IResourceCache resourceCache)
        {
            _onShowTooltip = onShowTooltip;
            _onHideTooltip = onHideTooltip;
            _onActionPressed = onActionPressed;

            var panelContainer = new PanelContainer
            {
                StyleClasses = {StyleNano.StyleClassBorderedWindowPanel},
                SizeFlagsVertical = SizeFlags.FillExpand,
            };
            AddChild(panelContainer);

            _vbox = new VBoxContainer() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.FillExpand};
            panelContainer.AddChild(_vbox);

            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopLeft, margin: 10);
            LayoutContainer.SetAnchorBottom(this, 1f);
            LayoutContainer.SetMarginTop(this, 250);
            LayoutContainer.SetMarginBottom(this, -250);

            _slots = new ActionSlot[ClientActionsComponent.Slots];

            for (byte i = 1; i <= ClientActionsComponent.Slots; i++)
            {
                var slot = new ActionSlot(i, resourceCache);
                slot.OnShowTooltip += onShowTooltip;
                slot.OnHideTooltip += onHideTooltip;
                slot.OnPressed += onActionPressed;
                _vbox.AddChild(slot);
                _slots[i - 1] = slot;
            }
        }

        /// <summary>
        /// Updates the action assigned to the indicated slot.
        /// </summary>
        /// <param name="slot">slot index to assign to (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        /// <param name="action">action to assign</param>
        public void Assign(byte slot, ActionPrototype action)
        {
            _slots[slot].Assign(action);
        }

        /// <summary>
        /// Displays the action on the indicated slot as revoked (makes the number red)
        /// </summary>
        /// <param name="slot">slot index containing the action to revoke (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void RevokeSlot(byte slot)
        {
            _slots[slot].Revoke();
        }


        /// <summary>
        /// Displays the action on the indicated slot as granted (makes the number white)
        /// </summary>
        /// <param name="slot">slot index containing the action to revoke (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        public void GrantSlot(byte slot)
        {
            _slots[slot].Grant();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var slot in _slots)
            {
                slot.OnShowTooltip -= _onShowTooltip;
                slot.OnHideTooltip -= _onHideTooltip;
                slot.OnPressed -= _onActionPressed;
            }
        }

        /// <summary>
        /// Update the cooldown shown on the indicated slot, clearing it if cooldown is null
        /// </summary>
        /// <param name="slot">slot index containing the action to update the cooldown of
        /// (0 corresponds to the one labeled 1, 9 corresponds to the one labeled 0)</param>
        /// <param name="cooldown">cooldown start and end time</param>
        /// <param name="curTime">current time</param>
        /// <returns>the action slot control that the grant was performed on</returns>
        public void UpdateCooldown(byte slot, (TimeSpan start, TimeSpan end)? cooldown, TimeSpan curTime)
        {
            _slots[slot].UpdateCooldown(cooldown, curTime);
        }
    }
}
