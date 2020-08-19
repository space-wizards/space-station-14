using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Content.Shared.GameObjects.Components.Power;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
{
    /// <summary>
    ///     Component that represents a wall light. It has a light bulb that can be replaced when broken.
    /// </summary>
    [RegisterComponent]
    public class EmergencyLightComponent : SharedEmergencyLightComponent
    {
        private EmergencyLightState _lightState = EmergencyLightState.Charging;
        [ViewVariables]
        private EmergencyLightState LightState {
            get => _lightState;
            set {
                _lightState = value;
                Dirty();
            }
        }

        [ViewVariables]
        private BatteryComponent Battery { get => Owner.GetComponent<BatteryComponent>(); }
        [ViewVariables]
        private PointLightComponent Light { get => Owner.GetComponent<PointLightComponent>(); }
        [ViewVariables]
        private PowerReceiverComponent PowerReceiver { get => Owner.GetComponent<PowerReceiverComponent>(); }
        private SpriteComponent Sprite { get => Owner.GetComponent<SpriteComponent>(); }

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
                LightState = EmergencyLightState.Charging;
            }
            else
            {
                TurnOn();
                LightState = EmergencyLightState.On;
            }
        }

        public void OnUpdate(float frameTime)
        {
            if (LightState == EmergencyLightState.Empty
                || LightState == EmergencyLightState.Full) return;

            if(LightState == EmergencyLightState.On)
            {
                if (!Battery.TryUseCharge(Wattage * frameTime))
                {
                    LightState = EmergencyLightState.Empty;
                    TurnOff();
                }
            }
            else
            {
                Battery.CurrentCharge += ChargingWattage * frameTime * ChargingEfficiency;
                if (Battery.BatteryState == BatteryState.Full)
                {
                    PowerReceiver.Load = 1;
                    LightState = EmergencyLightState.Full;
                }
            }
        }

        private void TurnOff()
        {
            Sprite.LayerSetState(0, "off");
            Light.Enabled = false;
        }

        private void TurnOn()
        {
            Sprite.LayerSetState(0, "on");
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

        public override ComponentState GetComponentState()
        {
            return new EmergencyLightComponentState(LightState);
        }
    }
}
