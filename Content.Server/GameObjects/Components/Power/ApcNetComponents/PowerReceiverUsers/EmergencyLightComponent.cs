using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
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
        private EmergencyLightState State
        {
            get => _state;
            set
            {
                if (_state == value)
                    return;

                _state = value;
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new EmergencyLightMessage(this, _state));
            }
        }

        private EmergencyLightState _state = EmergencyLightState.Empty;

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
        public void UpdateState(PowerChangedMessage e)
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
                State = EmergencyLightState.Charging;
            }
            else
            {
                TurnOn();
                State = EmergencyLightState.On;
            }
        }

        public void OnUpdate(float frameTime)
        {
            if (Owner.Deleted || !Owner.TryGetComponent(out BatteryComponent battery))
            {
                return;
            }

            if(State == EmergencyLightState.On)
            {
                if (!battery.TryUseCharge(_wattage * frameTime))
                {
                    State = EmergencyLightState.Empty;
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

                    State = EmergencyLightState.Full;
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

        public override void HandleMessage(ComponentMessage message, IComponent component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    UpdateState(powerChanged);
                    break;
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString($"The battery indicator displays: {BatteryStateText[State]}."));
        }

        public enum EmergencyLightState
        {
            Charging,
            Full,
            Empty,
            On
        }

        public Dictionary<EmergencyLightState, string> BatteryStateText = new()
        {
            { EmergencyLightState.Full, "[color=darkgreen]Full[/color]"},
            { EmergencyLightState.Empty, "[color=darkred]Empty[/color]"},
            { EmergencyLightState.Charging, "[color=darkorange]Charging[/color]"},
            { EmergencyLightState.On, "[color=darkorange]Discharging[/color]"}
        };
    }

    public sealed class EmergencyLightMessage : EntitySystemMessage
    {
        public EmergencyLightComponent Component { get; }

        public EmergencyLightComponent.EmergencyLightState State { get; }

        public EmergencyLightMessage(EmergencyLightComponent component, EmergencyLightComponent.EmergencyLightState state)
        {
            Component = component;
            State = state;
        }
    }
}
