using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using Serilog;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedStatusEffectsComponent))]
    public sealed class ClientStatusEffectsComponent : SharedStatusEffectsComponent
    {
        private static readonly float TooltipTextMaxWidth = 265;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private StatusEffectsUI _ui;
        private PanelContainer _tooltip;
        private RichTextLabel _stateName;
        private RichTextLabel _stateDescription;

        [ViewVariables]
        private Dictionary<StatusEffect, StatusEffectStatus> _status = new Dictionary<StatusEffect, StatusEffectStatus>();
        [ViewVariables]
        private Dictionary<StatusEffect, CooldownGraphic> _cooldown = new Dictionary<StatusEffect, CooldownGraphic>();

        public override IReadOnlyDictionary<StatusEffect, StatusEffectStatus> Statuses => _status;

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// </summary>
        [ViewVariables]
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
            _tooltip = new PanelContainer
            {
                Visible = false,
                StyleClasses = { StyleNano.StyleClassTooltipPanel }
            };
            var tooltipVBox = new VBoxContainer
            {
                RectClipContent = true
            };
            _tooltip.AddChild(tooltipVBox);
            _stateName = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipAlertTitle }
            };
            tooltipVBox.AddChild(_stateName);
            _stateDescription = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipAlertDescription }
            };
            tooltipVBox.AddChild(_stateDescription);

            _userInterfaceManager.PopupRoot.AddChild(_tooltip);

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
            // TODO: diff from our current statuses instead of the below, which
            // causes tooltips to disappear any time any status is changed
            _cooldown.Clear();
            _ui.VBox.DisposeAllChildren();

            foreach (var (key, effect) in _status.OrderBy(x => (int) x.Key))
            {
                if (!_statusEffectStateManager.TryDecode(effect.StatusEffectStateEncoded, out var statusEffectState))
                {
                    Logger.ErrorS("status", "Unable to decode status effect state {0}", effect.StatusEffectStateEncoded);
                    continue;
                }

                var texture = _resourceCache.GetTexture(statusEffectState.GetIconPath(effect.Severity));
                var status = new StatusControl(key, texture);
                // show custom tooltip for the status control
                status.OnShowTooltip += (sender, args) =>
                {
                    _tooltip.Visible = true;
                    _stateName.SetMessage(statusEffectState.Name);
                    _stateDescription.SetMessage(statusEffectState.Description);
                    // TODO: Text display of cooldown
                    Tooltips.PositionTooltip(_tooltip);
                };
                status.OnHideTooltip += StatusOnOnHideTooltip;

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

        private void StatusOnOnHideTooltip(object? sender, EventArgs e)
        {
            _tooltip.Visible = false;
        }

        private void StatusPressed(BaseButton.ButtonEventArgs args, StatusControl status)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            SendNetworkMessage(new ClickStatusMessage(status.Effect));
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

        /// <inheritdoc />
        public override void ChangeStatusEffectIcon(string statusEffectStateId, short? severity = null)
        {
            if (_statusEffectStateManager.TryGetWithEncoded(statusEffectStateId, out var statusEffectState,
                out var encoded))
            {
                if (_status.TryGetValue(statusEffectState.StatusEffect, out var value) &&
                    value.StatusEffectStateEncoded == encoded &&
                    value.Severity == severity)
                {
                    return;
                }
                _status[statusEffectState.StatusEffect] = new StatusEffectStatus
                {
                    Cooldown = value.Cooldown,
                    StatusEffectStateEncoded = encoded,
                    Severity = severity
                };

                Dirty();
            }
            else
            {
                Logger.ErrorS("status",
                    "Unable to set status effect state {0}, please ensure this is a valid statusEffectState",
                    statusEffectStateId);
            }

        }

        /// <inheritdoc />
        public override void ChangeStatusEffect(string statusEffectStateId, short? severity = null, (TimeSpan, TimeSpan)? cooldown = null)
        {
            if (_statusEffectStateManager.TryGetWithEncoded(statusEffectStateId, out var statusEffectState, out var encoded))
            {
                //TODO: All these duplicated modified checks should be refactored between this and ServerStatusEffectsComponent
                if (_status.TryGetValue(statusEffectState.StatusEffect, out var value) &&
                    value.StatusEffectStateEncoded == encoded &&
                    value.Severity == severity && value.Cooldown == cooldown)
                {
                    return;
                }
                _status[statusEffectState.StatusEffect] = new StatusEffectStatus()
                    {Cooldown = cooldown, StatusEffectStateEncoded = encoded, Severity = severity};
                Dirty();

            }
            else
            {
                Logger.ErrorS("status", "Unable to set status effect state {0}, please ensure this is a valid statusEffectState",
                    statusEffectStateId);
            }
        }

        public override void RemoveStatusEffect(StatusEffect effect)
        {
            if (!_status.Remove(effect))
            {
                return;
            }

            UpdateStatusEffects();
            Dirty();
        }
    }
}
