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

            UpdateState(ud, component);
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

            foreach (var (uid, light, pointLight, appearance, xform) in EntityQueryEnumerator<EmergencyLightComponent, PointLightComponent, AppearanceComponent, TransformComponent>())
            {
                if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != ev.Station)
                    continue;

                pointLight.Color = details.EmergencyLightColor;
                _appearance.SetData(uid, EmergencyLightVisuals.Color, details.EmergencyLightColor, appearance);

                if (details.ForceEnableEmergencyLights && !light.ForciblyEnabled)
                {
                    light.ForciblyEnabled = true;
                    TurnOn(uid, light);
                }
                else if (!details.ForceEnableEmergencyLights && light.ForciblyEnabled)
                {
                    // Previously forcibly enabled, and we went down an alert level.
                    light.ForciblyEnabled = false;
                    UpdateState(light);
                }
            }
        }

        public void SetState(EntityUid uid, EmergencyLightComponent component, EmergencyLightState state)
        {
            if (component.State == state) return;

            component.State = state;
            RaiseLocalEvent(uid, new EmergencyLightEvent(state));
        }

        public override void Update(float frameTime)
        {
            foreach (var (uid, _, activeLight, battery) in EntityQueryEnumerator<ActiveEmergencyLightComponent, EmergencyLightComponent, BatteryComponent>())
            {
                Update(uid, activeLight, battery, frameTime);
            }
        }

        private void Update(EntityUid uid, EmergencyLightComponent component, BatteryComponent battery, float frameTime)
        {
            if (component.State == EmergencyLightState.On)
            {
                if (!_battery.TryUseCharge(uid, component.Wattage * frameTime, battery))
                {
                    SetState(uid, component, EmergencyLightState.Empty);
                    TurnOff(uid, component);
                }
            }
            else
            {
                battery.CurrentCharge += component.ChargingWattage * frameTime * component.ChargingEfficiency;
                if (battery.IsFullyCharged)
                {
                    if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                    {
                        receiver.Load = 1;
                    }

                    SetState(uid, component, EmergencyLightState.Full);
                }
            }
        }

        /// <summary>
        ///     Updates the light's power drain, battery drain, sprite and actual light state.
        /// </summary>
        public void UpdateState(EntityUid uid, EmergencyLightComponent component)
        {
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                return;

            if (receiver.Powered && !component.ForciblyEnabled)
            {
                receiver.Load = (int) Math.Abs(component.Wattage);
                TurnOff(uid, component);
                SetState(uid, component, EmergencyLightState.Charging);
            }
            else
            {
                TurnOn(uid, component);
                SetState(uid, component, EmergencyLightState.On);
            }
        }

        private void TurnOff(EntityUid uid, EmergencyLightComponent component)
        {
            if (TryComp<PointLightComponent>(uid, out var light))
            {
                light.Enabled = false;
            }

            _appearance.SetData(uid, EmergencyLightVisuals.On, false, appearance);

            RemComp<RotatingLightComponent>(uid);

            _ambient.SetAmbience(uid, false);
        }

        private void TurnOn(EntityUid uid, EmergencyLightComponent component)
        {
            if (TryComp<PointLightComponent>(uid, out var light))
            {
                light.Enabled = true;
            }

            EnsureComp<RotatingLightComponent>(uid);

            _appearance.SetData(uid, EmergencyLightVisuals.On, true);
            _ambient.SetAmbience(uid, true);
        }
    }
}

