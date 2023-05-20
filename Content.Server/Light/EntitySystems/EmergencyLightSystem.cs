using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Light;
using Content.Shared.Light.Component;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameStates;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class EmergencyLightSystem : SharedEmergencyLightSystem
    {
        [Dependency] private readonly AmbientSoundSystem _ambient = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<EmergencyLightComponent, EmergencyLightEvent>(OnEmergencyLightEvent);
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<EmergencyLightComponent, ComponentGetState>(GetCompState);
            SubscribeLocalEvent<EmergencyLightComponent, PointLightToggleEvent>(HandleLightToggle);
            SubscribeLocalEvent<EmergencyLightComponent, ExaminedEvent>(OnEmergencyExamine);
            SubscribeLocalEvent<EmergencyLightComponent, PowerChangedEvent>(OnEmergencyPower);
        }

        private void OnEmergencyPower(EntityUid uid, EmergencyLightComponent component, ref PowerChangedEvent args)
        {
            var meta = MetaData(uid);

            // TODO: PowerChangedEvent shouldn't be issued for paused ents but this is the world we live in.
            if (meta.EntityLifeStage >= EntityLifeStage.Terminating ||
                meta.EntityPaused)
            {
                return;
            }

            UpdateState(component);
        }

        private void OnEmergencyExamine(EntityUid uid, EmergencyLightComponent component, ExaminedEvent args)
        {
            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine",
                    ("batteryStateText",
                        Loc.GetString(component.BatteryStateText[component.State]))));

            // Show alert level on the light itself.
            if (!TryComp<AlertLevelComponent>(_station.GetOwningStation(uid), out var alerts))
                return;

            if (alerts.AlertLevels == null)
                return;

            var name = alerts.CurrentLevel;

            var color = Color.White;
            if (alerts.AlertLevels.Levels.TryGetValue(alerts.CurrentLevel, out var details))
                color = details.Color;

            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine-alert",
                    ("color", color.ToHex()),
                    ("level", name)));
        }

        private void HandleLightToggle(EntityUid uid, EmergencyLightComponent component, PointLightToggleEvent args)
        {
            if (component.Enabled == args.Enabled)
                return;

            component.Enabled = args.Enabled;
            Dirty(component);
        }

        private void GetCompState(EntityUid uid, EmergencyLightComponent component, ref ComponentGetState args)
        {
            args.State = new EmergencyLightComponentState(component.Enabled);
        }

        private void OnEmergencyLightEvent(EntityUid uid, EmergencyLightComponent component, EmergencyLightEvent args)
        {
            switch (args.State)
            {
                case EmergencyLightState.On:
                case EmergencyLightState.Charging:
                    EnsureComp<ActiveEmergencyLightComponent>(uid);
                    break;
                case EmergencyLightState.Full:
                case EmergencyLightState.Empty:
                    RemComp<ActiveEmergencyLightComponent>(uid);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
        {
            if (!TryComp<AlertLevelComponent>(ev.Station, out var alert))
                return;

            if (alert.AlertLevels == null || !alert.AlertLevels.Levels.TryGetValue(ev.AlertLevel, out var details))
                return;

            foreach (var (light, pointLight, appearance, xform) in EntityQuery<EmergencyLightComponent, PointLightComponent, AppearanceComponent, TransformComponent>())
            {
                if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != ev.Station)
                    continue;

                pointLight.Color = details.EmergencyLightColor;
                _appearance.SetData(appearance.Owner, EmergencyLightVisuals.Color, details.EmergencyLightColor, appearance);

                if (details.ForceEnableEmergencyLights && !light.ForciblyEnabled)
                {
                    light.ForciblyEnabled = true;
                    TurnOn(light);
                }
                else if (!details.ForceEnableEmergencyLights && light.ForciblyEnabled)
                {
                    // Previously forcibly enabled, and we went down an alert level.
                    light.ForciblyEnabled = false;
                    UpdateState(light);
                }
            }
        }

        public void SetState(EmergencyLightComponent component, EmergencyLightState state)
        {
            if (component.State == state) return;

            component.State = state;
            RaiseLocalEvent(component.Owner, new EmergencyLightEvent(component, state));
        }

        public override void Update(float frameTime)
        {
            foreach (var (_, activeLight, battery) in EntityQuery<ActiveEmergencyLightComponent, EmergencyLightComponent, BatteryComponent>())
            {
                Update(activeLight, battery, frameTime);
            }
        }

        private void Update(EmergencyLightComponent component, BatteryComponent battery, float frameTime)
        {
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

            if (receiver.Powered && !component.ForciblyEnabled)
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
                _appearance.SetData(appearance.Owner, EmergencyLightVisuals.On, false, appearance);

            _ambient.SetAmbience(component.Owner, false);
        }

        private void TurnOn(EmergencyLightComponent component)
        {
            if (TryComp(component.Owner, out PointLightComponent? light))
            {
                light.Enabled = true;
            }

            if (TryComp(component.Owner, out AppearanceComponent? appearance))
            {
                _appearance.SetData(appearance.Owner, EmergencyLightVisuals.On, true, appearance);
            }

            _ambient.SetAmbience(component.Owner, true);
        }
    }
}

