using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.Examine;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class EmergencyLightSystem : SharedEmergencyLightSystem
    {
        private readonly HashSet<EmergencyLightComponent> _activeLights = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmergencyLightEvent>(HandleEmergencyLightMessage);
            SubscribeLocalEvent<EmergencyLightComponent, ComponentGetState>(GetCompState);
            SubscribeLocalEvent<EmergencyLightComponent, PointLightToggleEvent>(HandleLightToggle);
            SubscribeLocalEvent<EmergencyLightComponent, ExaminedEvent>(OnEmergencyExamine);
            SubscribeLocalEvent<EmergencyLightComponent, PowerChangedEvent>(OnEmergencyPower);
        }

        private void OnEmergencyPower(EntityUid uid, EmergencyLightComponent component, PowerChangedEvent args)
        {
            UpdateState(component);
        }

        private void OnEmergencyExamine(EntityUid uid, EmergencyLightComponent component, ExaminedEvent args)
        {
            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine",
                    ("batteryStateText",
                        Loc.GetString(component.BatteryStateText[component.State]))));
        }

        private void HandleLightToggle(EntityUid uid, EmergencyLightComponent component, PointLightToggleEvent args)
        {
            if (component.Enabled == args.Enabled) return;
            component.Enabled = args.Enabled;
            Dirty(component);
        }

        private void GetCompState(EntityUid uid, EmergencyLightComponent component, ref ComponentGetState args)
        {
            args.State = new EmergencyLightComponentState(component.Enabled);
        }

        private void HandleEmergencyLightMessage(EmergencyLightEvent @event)
        {
            switch (@event.State)
            {
                case EmergencyLightState.On:
                case EmergencyLightState.Charging:
                    _activeLights.Add(@event.Component);
                    break;
                case EmergencyLightState.Full:
                case EmergencyLightState.Empty:
                    _activeLights.Remove(@event.Component);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetState(EmergencyLightComponent component, EmergencyLightState state)
        {
            if (component.State == state) return;

            component.State = state;
            RaiseLocalEvent(component.Owner, new EmergencyLightEvent(component, state), true);
        }

        public override void Update(float frameTime)
        {
            foreach (var activeLight in _activeLights)
            {
                Update(activeLight, frameTime);
            }
        }

        private void Update(EmergencyLightComponent component, float frameTime)
        {
            if (!EntityManager.EntityExists(component.Owner) || !TryComp(component.Owner, out BatteryComponent? battery) || MetaData(component.Owner).EntityPaused)
            {
                return;
            }

            if (component.State == EmergencyLightState.On)
            {
                if (!battery.TryUseCharge(component.Wattage * frameTime))
                {
                    SetState(component, EmergencyLightState.Empty);
                    TurnOff(component);
                }
            }
            else
            {
                battery.CurrentCharge += component.ChargingWattage * frameTime * component.ChargingEfficiency;
                if (battery.IsFullyCharged)
                {
                    if (TryComp(component.Owner, out ApcPowerReceiverComponent? receiver))
                    {
                        receiver.Load = 1;
                    }

                    SetState(component, EmergencyLightState.Full);
                }
            }
        }

        /// <summary>
        ///     Updates the light's power drain, battery drain, sprite and actual light state.
        /// </summary>
        public void UpdateState(EmergencyLightComponent component)
        {
            if (!TryComp(component.Owner, out ApcPowerReceiverComponent? receiver))
            {
                return;
            }

            if (receiver.Powered)
            {
                receiver.Load = (int) Math.Abs(component.Wattage);
                TurnOff(component);
                SetState(component, EmergencyLightState.Charging);
            }
            else
            {
                TurnOn(component);
                SetState(component, EmergencyLightState.On);
            }
        }

        private void TurnOff(EmergencyLightComponent component)
        {
            if (TryComp(component.Owner, out PointLightComponent? light))
            {
                light.Enabled = false;
            }

            if (TryComp(component.Owner, out AppearanceComponent? appearance))
                appearance.SetData(EmergencyLightVisuals.On, false);
        }

        private void TurnOn(EmergencyLightComponent component)
        {
            if (TryComp(component.Owner, out PointLightComponent? light))
            {
                light.Enabled = true;
            }

            if (TryComp(component.Owner, out AppearanceComponent? appearance))
                appearance.SetData(EmergencyLightVisuals.On, true);
        }
    }
}
