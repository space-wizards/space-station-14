using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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

        [ViewVariables]
        private BatteryComponent Battery => Owner.GetComponent<BatteryComponent>();
        [ViewVariables]
        private PointLightComponent Light => Owner.GetComponent<PointLightComponent>();
        [ViewVariables]
        private PowerReceiverComponent PowerReceiver => Owner.GetComponent<PowerReceiverComponent>();
        private SpriteComponent Sprite => Owner.GetComponent<SpriteComponent>();

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
            if (PowerReceiver.Powered)
            {
                PowerReceiver.Load = (int) Math.Abs(_wattage);
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

            if(_lightState == EmergencyLightState.On)
            {
                if (!Battery.TryUseCharge(_wattage * frameTime))
                {
                    _lightState = EmergencyLightState.Empty;
                    TurnOff();
                }
            }
            else
            {
                Battery.CurrentCharge += _chargingWattage * frameTime * _chargingEfficiency;
                if (Battery.BatteryState == BatteryState.Full)
                {
                    PowerReceiver.Load = 1;
                    _lightState = EmergencyLightState.Full;
                }
            }
        }

        private void TurnOff()
        {
            Sprite.LayerSetState(0, "emergency_light_off");
            Light.Enabled = false;
        }

        private void TurnOn()
        {
            Sprite.LayerSetState(0, "emergency_light_on");
            Light.Enabled = true;
        }

        public override void Initialize()
        {
            base.Initialize();
            Owner.GetComponent<PowerReceiverComponent>().OnPowerStateChanged += UpdateState;
        }

        public override void OnRemove()
        {
            Owner.GetComponent<PowerReceiverComponent>().OnPowerStateChanged -= UpdateState;
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

        public Dictionary<EmergencyLightState, string> BatteryStateText = new Dictionary<EmergencyLightState, String>
        {
            { EmergencyLightState.Full, "[color=darkgreen]Full[/color]"},
            { EmergencyLightState.Empty, "[color=darkred]Empty[/color]"},
            { EmergencyLightState.Charging, "[color=darkorange]Charging[/color]"},
            { EmergencyLightState.On, "[color=darkorange]Discharging[/color]"}
        };
    }
}
