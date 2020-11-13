using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.Graphics;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Mobs
{
    /// <inheritdoc/>
    [RegisterComponent]
    [ComponentReference(typeof(SharedActionsComponent))]
    public sealed class ClientActionsComponent : SharedAlertsComponent
    {
        private static readonly float TooltipTextMaxWidth = 265;

        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private ActionsUI _ui;
        private PanelContainer _tooltip;
        private RichTextLabel _actionName;
        private RichTextLabel _actionDescription;
        private RichTextLabel _actionCooldown;
        private RichTextLabel _actionRequirements;
        private bool _tooltipReady;

        [ViewVariables]
        private Dictionary<AlertKey, AlertControl> _alertControls
            = new Dictionary<AlertKey, AlertControl>();

        /// <summary>
        /// Allows calculating if we need to act due to this component being controlled by the current mob
        /// TODO: should be revisited after space-wizards/RobustToolbox#1255
        /// </summary>
        [ViewVariables]
        private bool CurrentlyControlled => _playerManager.LocalPlayer != null && _playerManager.LocalPlayer.ControlledEntity == Owner;


        /// <summary>
        /// x = hotbar number, y = slot of that hotbar (index 0 corresponds to the one labeled "1",
        /// index 9 corresponds to the one labeled "0"). Essentially the inverse of _assignments.
        /// </summary>
        private ActionSlot[,] _slots = new ActionSlot[10, 10];

        /// <summary>
        /// Hotbar and slot assignment for each action type (slot index 0 corresponds to the one labeled "1",
        /// slot index 9 corresponds to the one labeled "0"). The key corresponds to an index in the _slots array.
        /// The value is a list because actions can be assigned to multiple slots. Even if an action type has not been granted,
        /// it can still be assigned to a slot. Essentially the inverse of _slots.
        /// </summary>
        private Dictionary<ActionType, List<(byte Hotbar, byte Slot)>> _assignments = new Dictionary<ActionType, List<(byte Hotbar, byte Slot)>>();

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

            UpdateAlertsControls();
        }

        private void PlayerAttached()
        {
            if (!CurrentlyControlled || _ui != null)
            {
                return;
            }

            _alertOrder = IoCManager.Resolve<IPrototypeManager>().EnumeratePrototypes<AlertOrderPrototype>().FirstOrDefault();
            if (_alertOrder == null)
            {
                Logger.ErrorS("alert", "no alertOrder prototype found, alerts will be in random order");
            }

            _ui = new AlertsUI(IoCManager.Resolve<IClyde>());
            var uiManager = IoCManager.Resolve<IUserInterfaceManager>();
            uiManager.StateRoot.AddChild(_ui);

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
            _actionName = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipAlertTitle }
            };
            tooltipVBox.AddChild(_actionName);
            _actionDescription = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipAlertDescription }
            };
            tooltipVBox.AddChild(_actionDescription);
            _actionCooldown = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = { StyleNano.StyleClassTooltipAlertCooldown }
            };
            tooltipVBox.AddChild(_actionCooldown);

            uiManager.PopupRoot.AddChild(_tooltip);

            UpdateAlertsControls();
        }

        private void PlayerDetached()
        {
            _ui?.Dispose();
            _ui = null;
            _alertControls.Clear();
        }

        /// <summary>
        /// Updates the displayed alerts based on current state of Alerts, performing
        /// a diff to ensure we only change what's changed (this avoids active tooltips disappearing any
        /// time state changes)
        /// </summary>
        private void UpdateAlertsControls()
        {
            if (!CurrentlyControlled || _ui == null)
            {
                return;
            }

            // remove any controls with keys no longer present
            var toRemove = new List<AlertKey>();
            foreach (var existingKey in _alertControls.Keys)
            {
                if (!IsShowingAlert(existingKey))
                {
                    toRemove.Add(existingKey);
                }
            }

            foreach (var alertKeyToRemove in toRemove)
            {
                // remove and dispose the control
                _alertControls.Remove(alertKeyToRemove, out var control);
                control?.Dispose();
            }

            // now we know that alertControls contains alerts that should still exist but
            // may need to updated,
            // also there may be some new alerts we need to show.
            // further, we need to ensure they are ordered w.r.t their configured order
            foreach (var alertStatus in EnumerateAlertStates())
            {
                if (!AlertManager.TryGet(alertStatus.AlertType, out var newAlert))
                {
                    Logger.ErrorS("alert", "Unrecognized alertType {0}", alertStatus.AlertType);
                    continue;
                }

                if (_alertControls.TryGetValue(newAlert.AlertKey, out var existingAlertControl) &&
                    existingAlertControl.Alert.AlertType == newAlert.AlertType)
                {
                    // id is the same, simply update the existing control severity
                    existingAlertControl.SetSeverity(alertStatus.Severity);
                }
                else
                {
                    existingAlertControl?.Dispose();

                    // this is a new alert + alert key or just a different alert with the same
                    // key, create the control and add it in the appropriate order
                    var newAlertControl = CreateAlertControl(newAlert, alertStatus);
                    if (_alertOrder != null)
                    {
                        var added = false;
                        foreach (var alertControl in _ui.Grid.Children)
                        {
                            if (_alertOrder.Compare(newAlert, ((AlertControl) alertControl).Alert) < 0)
                            {
                                var idx = alertControl.GetPositionInParent();
                                _ui.Grid.Children.Add(newAlertControl);
                                newAlertControl.SetPositionInParent(idx);
                                added = true;
                                break;
                            }
                        }

                        if (!added)
                        {
                            _ui.Grid.Children.Add(newAlertControl);
                        }
                    }
                    else
                    {
                        _ui.Grid.Children.Add(newAlertControl);
                    }

                    _alertControls[newAlert.AlertKey] = newAlertControl;
                }
            }
        }

        private AlertControl CreateAlertControl(AlertPrototype alert, AlertState alertState)
        {

            var alertControl = new AlertControl(alert, alertState.Severity, _resourceCache);
            // show custom tooltip for the status control
            alertControl.OnShowTooltip += AlertOnOnShowTooltip;
            alertControl.OnHideTooltip += AlertOnOnHideTooltip;

            alertControl.OnPressed += AlertControlOnPressed;

            return alertControl;
        }

        private void AlertControlOnPressed(BaseButton.ButtonEventArgs args)
        {
            AlertPressed(args, args.Button as AlertControl);
        }

        private void AlertOnOnHideTooltip(object sender, EventArgs e)
        {
            _tooltipReady = false;
            _tooltip.Visible = false;
        }

        private void AlertOnOnShowTooltip(object sender, EventArgs e)
        {
            var alertControl = (AlertControl) sender;
            _actionName.SetMessage(alertControl.Alert.Name);
            _actionDescription.SetMessage(alertControl.Alert.Description);
            // check for a cooldown
            if (alertControl.TotalDuration != null && alertControl.TotalDuration > 0)
            {
                _actionCooldown.SetMessage(FormattedMessage.FromMarkup("[color=#776a6a]" +
                                                                      alertControl.TotalDuration +
                                                                      " sec cooldown[/color]"));
                _actionCooldown.Visible = true;
            }
            else
            {
                _actionCooldown.Visible = false;
            }
            // TODO: Text display of cooldown
            Tooltips.PositionTooltip(_tooltip);
            // if we set it visible here the size of the previous tooltip will flicker for a frame,
            // so instead we wait until FrameUpdate to make it visible
            _tooltipReady = true;
        }

        private void AlertPressed(BaseButton.ButtonEventArgs args, AlertControl alert)
        {
            if (args.Event.Function != EngineKeyFunctions.UIClick)
            {
                return;
            }

            SendNetworkMessage(new ClickAlertMessage(alert.Alert.AlertType));
        }

        public void FrameUpdate(float frameTime)
        {
            if (_tooltipReady)
            {
                _tooltipReady = false;
                _tooltip.Visible = true;
            }
            foreach (var (alertKey, alertControl) in _alertControls)
            {
                // reconcile all alert controls with their current cooldowns
                if (TryGetAlertState(alertKey, out var alertState))
                {
                    alertControl.UpdateCooldown(alertState.Cooldown, _gameTiming.CurTime);
                }
                else
                {
                    Logger.WarningS("alert", "coding error - no alert state for alert {0} " +
                                             "even though we had an AlertControl for it, this" +
                                             " should never happen", alertControl.Alert.AlertType);
                }

            }
        }

        protected override void AfterShowAlert()
        {
            UpdateAlertsControls();
        }

        protected override void AfterClearAlert()
        {
            UpdateAlertsControls();
        }

        public override void OnRemove()
        {
            base.OnRemove();

            foreach (var alertControl in _alertControls.Values)
            {
                alertControl.OnShowTooltip -= AlertOnOnShowTooltip;
                alertControl.OnHideTooltip -= AlertOnOnHideTooltip;
                alertControl.OnPressed -= AlertControlOnPressed;
            }

        }
    }
}
