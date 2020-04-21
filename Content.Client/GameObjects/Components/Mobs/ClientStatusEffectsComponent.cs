using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    public sealed class ClientStatusEffectsComponent : SharedStatusEffectsComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IResourceCache _resourceCache;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager;
#pragma warning restore 649

        private StatusEffectsUI _ui;
        private IDictionary<StatusEffect, string> _icons = new Dictionary<StatusEffect, string>();

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        private bool CurrentlyControlled => _playerManager.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity == Owner;

        protected override void Shutdown()
        {
            base.Shutdown();
            PlayerDetached();
        }

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PlayerAttachedMsg _:
                    PlayerAttached();
                    break;
                case PlayerDetachedMsg _:
                    PlayerDetached();
                    break;
            }
        }

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is StatusEffectComponentState state) || _icons == state.StatusEffects) return;
            _icons = state.StatusEffects;
            UpdateIcons();
        }

        private void PlayerAttached()
        {
            if (!CurrentlyControlled || _ui != null)
            {
                return;
            }
            _ui = new StatusEffectsUI();
            _userInterfaceManager.StateRoot.AddChild(_ui);
            UpdateIcons();
        }

        private void PlayerDetached()
        {
            _ui?.Dispose();
            _ui = null;
        }

        public void UpdateIcons()
        {
            if (!CurrentlyControlled || _ui == null)
            {
                return;
            }
            _ui.VBox.DisposeAllChildren();

            foreach (var effect in _icons.OrderBy(x => (int) x.Key))
            {
                TextureRect newIcon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Texture = _resourceCache.GetTexture(effect.Value)
                };

                newIcon.Texture = _resourceCache.GetTexture(effect.Value);
                _ui.VBox.AddChild(newIcon);
            }
        }

        public void RemoveIcon(StatusEffect name)
        {
            _icons.Remove(name);
            UpdateIcons();
            Logger.InfoS("statuseffects", $"Removed icon {name}");
        }
    }
}
