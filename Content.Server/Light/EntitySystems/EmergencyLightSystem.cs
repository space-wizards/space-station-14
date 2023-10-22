using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server.Light.EntitySystems;

public sealed class EmergencyLightSystem : SharedEmergencyLightSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyLightComponent, EmergencyLightEvent>(OnEmergencyLightEvent);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
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

        UpdateState(uid, component);
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

        var query = EntityQueryEnumerator<EmergencyLightComponent, PointLightComponent, AppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var light, out var pointLight, out var appearance, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != ev.Station)
                continue;

            _pointLight.SetColor(uid, details.EmergencyLightColor, pointLight);
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
                UpdateState(uid, light);
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
        var query = EntityQueryEnumerator<ActiveEmergencyLightComponent, EmergencyLightComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out _, out var emergencyLight, out var battery))
        {
            Update(uid, emergencyLight, battery, frameTime);
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
        _pointLight.SetEnabled(uid, false);
        _appearance.SetData(uid, EmergencyLightVisuals.On, false);
        _ambient.SetAmbience(uid, false);
    }

    private void TurnOn(EntityUid uid, EmergencyLightComponent component)
    {
        _pointLight.SetEnabled(uid, true);
        _appearance.SetData(uid, EmergencyLightVisuals.On, true);
        _ambient.SetAmbience(uid, true);
    }
}
