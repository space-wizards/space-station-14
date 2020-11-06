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
    [ComponentReference(typeof(SharedAlertsComponent))]
    public sealed class ClientAlertsComponent : SharedAlertsComponent
    {
        private static readonly float TooltipTextMaxWidth = 265;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private AlertsUI _ui;
        private PanelContainer _tooltip;
        private RichTextLabel _stateName;
        private RichTextLabel _stateDescription;


        [ViewVariables]
        private Dictionary<AlertSlot, CooldownGraphic> _cooldown = new Dictionary<AlertSlot, CooldownGraphic>();

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

            // TODO: Ensure this equals check actually works properly (value not ref)
            if (!(curState is AlertsComponentState state) || Alerts == state.Alerts)
            {
                return;
            }

            UpdateAlerts(state.Alerts);
        }

        private void PlayerAttached()
        {
            if (!CurrentlyControlled || _ui != null)
            {
                return;
            }
            _ui = new AlertsUI();
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

            UpdateAlerts(Alerts);
        }

        private void PlayerDetached()
        {
            _ui?.Dispose();
            _ui = null;
            _cooldown.Clear();
        }

        private void UpdateAlerts(IReadOnlyDictionary<AlertSlot, AlertState> newAlerts)
        {
            if (!CurrentlyControlled || _ui == null)
            {
                return;
            }
            // TODO: diff from our current alerts instead of the below, which
            // causes tooltips to disappear any time any alert is changed
            _cooldown.Clear();
            _ui.VBox.DisposeAllChildren();

            SetAlerts(newAlerts);

            foreach (var (key, effect) in Alerts.OrderBy(x => (int) x.Key))
            {
                if (!AlertManager.TryDecode(effect.AlertEncoded, out var alert))
                {
                    Logger.ErrorS("alert", "Unable to decode alert {0}", effect.AlertEncoded);
                    continue;
                }

                var texture = _resourceCache.GetTexture(alert.GetIconPath(effect.Severity));
                var alertControl = new AlertControl(key, texture);
                // show custom tooltip for the status control
                alertControl.OnShowTooltip += (sender, args) =>
                {
                    _tooltip.Visible = true;
                    _stateName.SetMessage(alert.Name);
                    _stateDescription.SetMessage(alert.Description);
                    // TODO: Text display of cooldown
                    Tooltips.PositionTooltip(_tooltip);
                };
                alertControl.OnHideTooltip += AlertOnOnHideTooltip;

                if (effect.Cooldown.HasValue)
                {
                    var cooldown = new CooldownGraphic();
                    alertControl.Children.Add(cooldown);
                    _cooldown[key] = cooldown;
                }

                alertControl.OnPressed += args => AlertPressed(args, alertControl);

                _ui.VBox.AddChild(alertControl);
            }
        }

        private void AlertOnOnHideTooltip(object? sender, EventArgs e)
        {
            _tooltip.Visible = false;
        }

        private void AlertPressed(BaseButton.ButtonEventArgs args, AlertControl alert)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            SendNetworkMessage(new ClickAlertMessage(alert.Effect));
        }

        public void FrameUpdate(float frameTime)
        {
            foreach (var (effect, cooldownGraphic) in _cooldown)
            {
                var alert = Alerts[effect];
                if (!alert.Cooldown.HasValue)
                {
                    cooldownGraphic.Progress = 0;
                    cooldownGraphic.Visible = false;
                    continue;
                }

                var start = alert.Cooldown.Value.Item1;
                var end = alert.Cooldown.Value.Item2;

                var length = (end - start).TotalSeconds;
                var progress = (_gameTiming.CurTime - start).TotalSeconds / length;
                var ratio = (progress <= 1 ? (1 - progress) : (_gameTiming.CurTime - end).TotalSeconds * -5);

                cooldownGraphic.Progress = MathHelper.Clamp((float)ratio, -1, 1);
                cooldownGraphic.Visible = ratio > -1f;
            }
        }

        protected override void AfterClearAlert(AlertSlot effect)
        {
            UpdateAlerts(Alerts);
        }
    }
}
