using System;
using Content.Client.GameObjects.Components.Mobs;
using Content.Client.UserInterface.Controls;
using Content.Client.Utility;
using Content.Shared.Actions;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The action hotbar on the left side of the screen.
    /// </summary>
    public sealed class ActionsUI : PanelContainer
    {
        private readonly EventHandler _onShowTooltip;
        private readonly EventHandler _onHideTooltip;
        private readonly Action<BaseButton.ButtonToggledEventArgs> _onSlotToggled;
        private readonly ActionSlot[] _slots;

        private VBoxContainer _hotbarContainer;
        private VBoxContainer _slotContainer;

        private TextureButton _lockButton;
        private TextureButton _settingsButton;
        private TextureButton _previousHotbarButton;
        private Label _loadoutNumber;
        private TextureButton _nextHotbarButton;

        /// <param name="onShowTooltip">OnShowTooltip handler to assign to each action slot</param>
        /// <param name="onHideTooltip">OnHideTooltip handler to assign to each action slot</param>
        /// <param name="onSlotToggled">OnToggled handler to assign to each action slot button</param>
        public ActionsUI(EventHandler onShowTooltip, EventHandler onHideTooltip, Action<BaseButton.ButtonToggledEventArgs> onSlotToggled)
        {
            _onShowTooltip = onShowTooltip;
            _onHideTooltip = onHideTooltip;
            _onSlotToggled = onSlotToggled;

            SizeFlagsHorizontal = SizeFlags.FillExpand;
            SizeFlagsVertical = SizeFlags.FillExpand;

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            _hotbarContainer = new VBoxContainer
            {
                SeparationOverride = 3
            };
            AddChild(_hotbarContainer);

            var settingsContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(settingsContainer);

            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            _lockButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/lock.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            settingsContainer.AddChild(_lockButton);
            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            _settingsButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/gear.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            settingsContainer.AddChild(_settingsButton);
            settingsContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });

            _slotContainer = new VBoxContainer();
            _hotbarContainer.AddChild(_slotContainer);

            var loadoutContainer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.FillExpand
            };
            _hotbarContainer.AddChild(loadoutContainer);

            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });
            _previousHotbarButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/left_arrow.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(_previousHotbarButton);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            _loadoutNumber = new Label
            {
                Text = "1",
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(_loadoutNumber);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 2 });
            _nextHotbarButton = new TextureButton
            {
                TextureNormal = resourceCache.GetTexture("/Textures/Interface/Nano/right_arrow.svg.png"),
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                SizeFlagsStretchRatio = 1
            };
            loadoutContainer.AddChild(_nextHotbarButton);
            loadoutContainer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsStretchRatio = 1 });

            _slots = new ActionSlot[ClientActionsComponent.Slots];

            for (byte i = 1; i <= ClientActionsComponent.Slots; i++)
            {
                var slot = new ActionSlot(i);
                slot.OnShowTooltip += onShowTooltip;
                slot.OnHideTooltip += onHideTooltip;
                slot.OnToggled += onSlotToggled;
                _slotContainer.AddChild(slot);
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
                slot.OnToggled -= _onSlotToggled;
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
