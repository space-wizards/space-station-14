using System;
using System.Collections.Generic;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Light.Component;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents an emergency light, it has an internal battery that charges when the power is on.
    /// </summary>
    [RegisterComponent]
#pragma warning disable 618
    public class EmergencyLightComponent : SharedEmergencyLightComponent, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables]
        private EmergencyLightState State
        {
            get => _state;
            set
            {
                if (_state == value)
                    return;

                _state = value;
                _entMan.EventBus.RaiseEvent(EventSource.Local, new EmergencyLightMessage(this, _state));
            }
        }

        private EmergencyLightState _state = EmergencyLightState.Empty;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("wattage")]
        private float _wattage = 5;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chargingWattage")]
        private float _chargingWattage = 60;
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chargingEfficiency")]
        private float _chargingEfficiency = 0.85f;

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
            if (!_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver))
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
            if ((!_entMan.EntityExists(Owner) || !_entMan.TryGetComponent(Owner, out BatteryComponent? battery) || _entMan.GetComponent<MetaDataComponent>(Owner).EntityPaused))
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
                if (battery.IsFullyCharged)
                {
                    if (_entMan.TryGetComponent(Owner, out ApcPowerReceiverComponent? receiver))
                    {
                        receiver.Load = 1;
                    }

                    State = EmergencyLightState.Full;
                }
            }
        }

        private void TurnOff()
        {
            if (_entMan.TryGetComponent(Owner, out PointLightComponent? light))
            {
                light.Enabled = false;
            }

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                appearance.SetData(EmergencyLightVisuals.On, false);
        }

        private void TurnOn()
        {
            if (_entMan.TryGetComponent(Owner, out PointLightComponent? light))
            {
                light.Enabled = true;
            }

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
                appearance.SetData(EmergencyLightVisuals.On, true);
        }

#pragma warning disable 618
        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleMessage(ComponentMessage message, IComponent? component)
#pragma warning restore 618
        {
#pragma warning disable 618
            base.HandleMessage(message, component);
#pragma warning restore 618
            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    UpdateState(powerChanged);
                    break;
            }
        }

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("emergency-light-component-on-examine",("batteryStateText", Loc.GetString(BatteryStateText[State]))));
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
            { EmergencyLightState.Full, "emergency-light-component-light-state-full" },
            { EmergencyLightState.Empty, "emergency-light-component-light-state-empty" },
            { EmergencyLightState.Charging, "emergency-light-component-light-state-charging" },
            { EmergencyLightState.On, "emergency-light-component-light-state-on" }
        };
    }

    public sealed class EmergencyLightMessage : EntityEventArgs
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
