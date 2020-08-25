using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    /// <summary>
    ///     Component that represents an emergency light, it has an internal battery that charges when the power is on.
    /// </summary>
    [RegisterComponent]
    public class EmergencyLightComponent : Component, IExamine
    {
        public override string Name => "EmergencyLight";

        [ViewVariables]
        private EmergencyLightState _lightState = EmergencyLightState.Charging;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _wattage;
        [ViewVariables(VVAccess.ReadWrite)]
        private float _chargingWattage;
        [ViewVariables(VVAccess.ReadWrite)]
        private float _chargingEfficiency;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _wattage, "wattage", 5);
            serializer.DataField(ref _chargingWattage, "chargingWattage", 60);
            serializer.DataField(ref _chargingEfficiency, "chargingEfficiency", 0.85f);
        }

        /// <summary>
        ///     For attaching UpdateState() to events.
        /// </summary>
        public void UpdateState(object sender, EventArgs e)
        {
            UpdateState();
        }

        /// <summary>
        ///     Updates the light's power drain, battery drain, sprite and actual light state.
        /// </summary>
        public void UpdateState()
        {
            if (!Owner.TryGetComponent(out PowerReceiverComponent receiver))
            {
                return;
            }

            if (receiver.Powered)
            {
                receiver.Load = (int) Math.Abs(_wattage);
                TurnOff();
                _lightState = EmergencyLightState.Charging;
            }
            else
            {
                TurnOn();
                _lightState = EmergencyLightState.On;
            }
        }

        public void OnUpdate(float frameTime)
        {
            if (_lightState == EmergencyLightState.Empty
                || _lightState == EmergencyLightState.Full) return;

            if (!Owner.TryGetComponent(out BatteryComponent battery))
            {
                return;
            }

            if(_lightState == EmergencyLightState.On)
            {
                if (!battery.TryUseCharge(_wattage * frameTime))
                {
                    _lightState = EmergencyLightState.Empty;
                    TurnOff();
                }
            }
            else
            {
                battery.CurrentCharge += _chargingWattage * frameTime * _chargingEfficiency;
                if (battery.BatteryState == BatteryState.Full)
                {
                    if (Owner.TryGetComponent(out PowerReceiverComponent receiver))
                    {
                        receiver.Load = 1;
                    }

                    _lightState = EmergencyLightState.Full;
                }
            }
        }

        private void TurnOff()
        {
            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.LayerSetState(0, "emergency_light_off");
            }

            if (Owner.TryGetComponent(out PointLightComponent light))
            {
                light.Enabled = false;
            }
        }

        private void TurnOn()
        {
            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                sprite.LayerSetState(0, "emergency_light_on");
            }

            if (Owner.TryGetComponent(out PointLightComponent light))
            {
                light.Enabled = true;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.EnsureComponent(out PowerReceiverComponent receiver))
            {
                Logger.Warning($"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(PowerReceiverComponent)}");
            }

            receiver.OnPowerStateChanged += UpdateState;
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent receiver))
            {
                receiver.OnPowerStateChanged -= UpdateState;
            }

            base.OnRemove();
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString($"The battery indicator displays: {BatteryStateText[_lightState]}."));
        }

        public enum EmergencyLightState
        {
            Charging,
            Full,
            Empty,
            On
        }

        public Dictionary<EmergencyLightState, string> BatteryStateText = new Dictionary<EmergencyLightState, string>
        {
            { EmergencyLightState.Full, "[color=darkgreen]Full[/color]"},
            { EmergencyLightState.Empty, "[color=darkred]Empty[/color]"},
            { EmergencyLightState.Charging, "[color=darkorange]Charging[/color]"},
            { EmergencyLightState.On, "[color=darkorange]Discharging[/color]"}
        };
    }
}
