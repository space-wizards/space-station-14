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
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedStatusEffectsComponent))]
    public sealed class ClientStatusEffectsComponent : SharedStatusEffectsComponent
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private StatusEffectsUI _ui;
        [ViewVariables]
        private Dictionary<StatusEffect, StatusEffectStatus> _status = new Dictionary<StatusEffect, StatusEffectStatus>();
        [ViewVariables]
        private Dictionary<StatusEffect, CooldownGraphic> _cooldown = new Dictionary<StatusEffect, CooldownGraphic>();

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

            if (!(curState is StatusEffectComponentState state) || _status == state.StatusEffects)
            {
                return;
            }

            _status = state.StatusEffects;
            UpdateStatusEffects();
        }

        private void PlayerAttached()
        {
            if (!CurrentlyControlled || _ui != null)
            {
                return;
            }
            _ui = new StatusEffectsUI();
            _userInterfaceManager.StateRoot.AddChild(_ui);
            UpdateStatusEffects();
        }

        private void PlayerDetached()
        {
            _ui?.Dispose();
            _ui = null;
            _cooldown.Clear();
        }

        public void UpdateStatusEffects()
        {
            if (!CurrentlyControlled || _ui == null)
            {
                return;
            }
            _cooldown.Clear();
            _ui.VBox.DisposeAllChildren();

            foreach (var (key, effect) in _status.OrderBy(x => (int) x.Key))
            {
                var texture = _resourceCache.GetTexture(effect.Icon);
                var status = new StatusControl(key, texture)
                {
                    ToolTip = key.ToString()
                };

                if (effect.Cooldown.HasValue)
                {
                    var cooldown = new CooldownGraphic();
                    status.Children.Add(cooldown);
                    _cooldown[key] = cooldown;
                }

                status.OnPressed += args => StatusPressed(args, status);

                _ui.VBox.AddChild(status);
            }
        }

        private void StatusPressed(BaseButton.ButtonEventArgs args, StatusControl status)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            SendNetworkMessage(new ClickStatusMessage(status.Effect));
        }

        public void RemoveStatusEffect(StatusEffect name)
        {
            _status.Remove(name);
            UpdateStatusEffects();
        }

        public void FrameUpdate(float frameTime)
        {
            foreach (var (effect, cooldownGraphic) in _cooldown)
            {
                var status = _status[effect];
                if (!status.Cooldown.HasValue)
                {
                    cooldownGraphic.Progress = 0;
                    cooldownGraphic.Visible = false;
                    continue;
                }

                var start = status.Cooldown.Value.Item1;
                var end = status.Cooldown.Value.Item2;

                var length = (end - start).TotalSeconds;
                var progress = (_gameTiming.CurTime - start).TotalSeconds / length;
                var ratio = (progress <= 1 ? (1 - progress) : (_gameTiming.CurTime - end).TotalSeconds * -5);

                cooldownGraphic.Progress = MathHelper.Clamp((float)ratio, -1, 1);
                cooldownGraphic.Visible = ratio > -1f;
            }
        }

        public override void ChangeStatusEffect(StatusEffect effect, string icon, (TimeSpan, TimeSpan)? cooldown)
        {
            _status[effect] = new StatusEffectStatus()
            {
                Icon = icon,
                Cooldown = cooldown
            };

            Dirty();
        }
    }
}
