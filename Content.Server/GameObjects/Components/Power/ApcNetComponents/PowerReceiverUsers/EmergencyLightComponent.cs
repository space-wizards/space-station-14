using System;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class EmergencyLightComponent : Component
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
        public float Wattage { get; set; } = 5;
        [ViewVariables(VVAccess.ReadWrite)]
        public float ChargingWattage { get; set; } = 60;
        [ViewVariables(VVAccess.ReadWrite)]
        public float ChargingEfficiency { get; set; } = 85;

        /// <summary>
        ///     For attaching UpdateLight() to events.
        /// </summary>
        public void UpdateState(object sender, EventArgs e)
        {
            UpdateState();
        }

        /// <summary>
        ///     Updates the light's power drain, sprite and actual light state.
        /// </summary>
        public void UpdateState()
        {
            if (PowerReceiver.Powered)
            {
                PowerReceiver.Load = (int) Math.Abs(Wattage);
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
                if (!Battery.TryUseCharge(Wattage * frameTime))
                {
                    _lightState = EmergencyLightState.Empty;
                    TurnOff();
                }
            }
            else
            {
                Battery.CurrentCharge += ChargingWattage * frameTime * ChargingEfficiency;
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

        public enum EmergencyLightState
        {
            Charging,
            Full,
            Empty,
            On
        }
    }
}
