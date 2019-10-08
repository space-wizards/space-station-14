using System;
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
using Robust.Shared.ViewVariables;

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
        // TODO: Combine with StatusEffectsUI?
        private StatusEffectsUI _ui;
        private readonly IDictionary<StatusEffect, TextureRect> _icons = new Dictionary<StatusEffect, TextureRect>();

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        private bool CurrentlyControlled => _playerManager.LocalPlayer.ControlledEntity == Owner;

        public override void OnAdd()
        {
            base.OnAdd();
            PlayerAttached();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            PlayerDetached();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            switch (message)
            {
                case StatusEffectsMessage msg:
                    if (!CurrentlyControlled)
                    {
                        break;
                    }
                    switch (msg.Mode)
                    {
                        case StatusEffectsMode.Change:
                            ChangeIcon(msg.Name, msg.Filepath);
                            break;
                        case StatusEffectsMode.Remove:
                            RemoveIcon(msg.Name);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case PlayerAttachedMsg _:
                    if (!CurrentlyControlled)
                    {
                        break;
                    }
                    PlayerAttached();
                    break;
                case PlayerDetachedMsg _:
                    if (!CurrentlyControlled)
                    {
                        break;
                    }
                    PlayerDetached();
                    break;
            }
        }

        private void PlayerAttached()
        {
            if (_ui != null)
            {
                return;
            }
            _ui = new StatusEffectsUI();
            _userInterfaceManager.StateRoot.AddChild(_ui);
        }

        private void PlayerDetached()
        {
            _ui?.Dispose();
            _icons.Clear();
        }

        public void ChangeIcon(StatusEffect name, string filepath)
        {
            // Create
            if (!_icons.ContainsKey(name))
            {
                TextureRect newIcon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Texture = _resourceCache.GetTexture(filepath)
                };
                _icons.Add(name, newIcon);
                // TODO: May be a better way to do this but I don't imagine they would update that frequently
                // Priorities
                _ui.VBox.DisposeAllChildren();
                foreach (var effect in _icons.OrderBy(x => (int) x.Key))
                {
                    _ui.VBox.AddChild(effect.Value);
                }
                return;
            }
            // Update
            _icons[name].Texture = _resourceCache.GetTexture(filepath);
            Logger.InfoS("statuseffects", $"Changed icon {name} to {filepath}");
        }

        public void RemoveIcon(StatusEffect name)
        {
            _icons[name].Dispose();
            Logger.InfoS("statuseffects", $"Removed icon {name}");
        }
    }
}
