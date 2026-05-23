using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Examine;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server.Light.EntitySystems;

public sealed partial class EmergencyLightSystem : SharedEmergencyLightSystem
{
    [Dependency] private AmbientSoundSystem _ambient = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyLightComponent, EmergencyLightEvent>(OnEmergencyLightEvent);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<EmergencyLightComponent, ExaminedEvent>(OnEmergencyExamine);
        SubscribeLocalEvent<EmergencyLightComponent, PowerChangedEvent>(OnEmergencyPower);
        SubscribeLocalEvent<EmergencyLightComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, EmergencyLightComponent component, MapInitEvent args)
    {
        // This forces the light active during creation so that it will start charging
        EnsureComp<ActiveEmergencyLightComponent>(uid);

        // This is just so that the light has the right color and turns on when constucted on non green alert level
        Entity<EmergencyLightComponent> entity = (uid, component);
        EntityUid? station = _station.GetOwningStation(entity.Owner);
        if (station == null)
            return;
        if (!TryComp<AlertLevelComponent>(station, out var alerts))
            return;
        AlertLevelChangedEvent ev = new AlertLevelChangedEvent((EntityUid)station, alerts.CurrentLevel);
        OnAlertLevelChanged(ev);
    }

    private void OnEmergencyPower(Entity<EmergencyLightComponent> entity, ref PowerChangedEvent args)
    {
        var meta = MetaData(entity.Owner);

        // TODO: PowerChangedEvent shouldn't be issued for paused ents but this is the world we live in.
        if (meta.EntityLifeStage >= EntityLifeStage.Terminating ||
            meta.EntityPaused)
        {
            return;
        }

        UpdateState(entity);
    }

    private void OnEmergencyExamine(EntityUid uid, EmergencyLightComponent component, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(EmergencyLightComponent)))
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
                    ("level", Loc.GetString($"alert-level-{name.ToString().ToLower()}"))));
        }
    }

    private void OnEmergencyLightEvent(EntityUid uid, EmergencyLightComponent component, EmergencyLightEvent args)
    {
        if (component.IsEnabled) //We want the light to always update if it is trying to be on
        {
            EnsureComp<ActiveEmergencyLightComponent>(uid);
            return;
        }
        switch (args.State)
        {
            case EmergencyLightState.On:
            case EmergencyLightState.Charging:
            case EmergencyLightState.Empty:
                EnsureComp<ActiveEmergencyLightComponent>(uid);
                break;
            case EmergencyLightState.Full:
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

            if (details.ForceEnableEmergencyLights && !light.ForciblyEnabled)
            {
                light.ForciblyEnabled = true;
                TurnOn((uid, light));
                UpdateState((uid, light));
            }
            else if (!details.ForceEnableEmergencyLights && light.ForciblyEnabled)
            {
                // Previously forcibly enabled, and we went down an alert level.
                light.ForciblyEnabled = false;
                UpdateState((uid, light));
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
            Update((uid, emergencyLight), battery, frameTime);
        }
    }

    private void Update(Entity<EmergencyLightComponent> entity, BatteryComponent battery, float frameTime)
    {
        float batteryCharge = _battery.GetCharge((entity.Owner, battery));
        float maxCharge = _battery.GetMaxCharge((entity.Owner, battery));

        // If the light is running try and consume power
        bool lightOn = entity.Comp.IsEnabled;
        if (lightOn)
        {
            batteryCharge -= entity.Comp.Wattage * frameTime;
        }

        if (TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver))
        {
            if (receiver.Powered) // Only charge if powered
            {
                float oldCharge = batteryCharge;
                batteryCharge = Math.Clamp(oldCharge + entity.Comp.ChargingWattage * frameTime * entity.Comp.ChargingEfficiency, 0, maxCharge);
                float delta = batteryCharge - oldCharge;
                receiver.Load = (int)((delta / frameTime) / entity.Comp.ChargingEfficiency); // Reduce load if full wattage not needed
            }
            else
            {
                receiver.Load = 1;
            }
        }

        // Update state with new battery charge level
        if (batteryCharge <= 0)
        {
            TurnOff(entity);
            SetState(entity.Owner, entity.Comp, EmergencyLightState.Empty);
        }
        else if (batteryCharge == maxCharge)
        {
            if (lightOn)
                TurnOn(entity);
            else //If the light is off and the system fills set load to 1 because this is out last update call till the light turns back on
                if (TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver2))
                    receiver2.Load = 1;
            SetState(entity.Owner, entity.Comp, EmergencyLightState.Full);
        }
        else
        {
            if (TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver3))
            {
                if (receiver3.Powered)
                    SetState(entity.Owner, entity.Comp, EmergencyLightState.Charging);
                else
                    SetState(entity.Owner, entity.Comp, EmergencyLightState.On);
            }
            else
            {
                SetState(entity.Owner, entity.Comp, EmergencyLightState.On);
            }
        }
        if (batteryCharge < 0)
            batteryCharge = 0;
        _battery.SetCharge((entity.Owner, battery), batteryCharge);
    }

    /// <summary>
    ///     Updates the light's power drain, battery drain, sprite and actual light state.
    /// </summary>
    public void UpdateState(Entity<EmergencyLightComponent> entity)
    {
        if (!TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver))
            return;

        if (!TryComp<AlertLevelComponent>(_station.GetOwningStation(entity.Owner), out var alerts))
            return;

        if (alerts.AlertLevels == null || !alerts.AlertLevels.Levels.TryGetValue(alerts.CurrentLevel, out var details))
        {
            TurnOff(entity, Color.Red); // if no alert, default to off red state
            entity.Comp.IsEnabled = false;
            return;
        }

        if (receiver.Powered && !entity.Comp.ForciblyEnabled) // Green alert
        {
            TurnOff(entity, details.EmergencyLightColor);
            entity.Comp.IsEnabled = false;
        }
        else if (!receiver.Powered) // If internal battery runs out it will end in off red state
        {
            TurnOn(entity, Color.Red);
            entity.Comp.IsEnabled = true;
            RaiseLocalEvent(entity.Owner, new EmergencyLightEvent(EmergencyLightState.On));
        }
        else // Powered and enabled
        {
            TurnOn(entity, details.EmergencyLightColor);
            entity.Comp.IsEnabled = true;
            RaiseLocalEvent(entity.Owner, new EmergencyLightEvent(EmergencyLightState.On));
        }
    }

    /// <summary>
    ///     Turn off emergency light and set color.
    /// </summary>
    private void TurnOff(Entity<EmergencyLightComponent> entity, Color color)
    {
        _pointLight.SetEnabled(entity.Owner, false);
        _pointLight.SetColor(entity.Owner, color);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.Color, color);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.On, false);
        _ambient.SetAmbience(entity.Owner, false);
    }

    private void TurnOff(Entity<EmergencyLightComponent> entity)
    {
        _pointLight.SetEnabled(entity.Owner, false);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.On, false);
        _ambient.SetAmbience(entity.Owner, false);
    }

    /// <summary>
    ///     Turn on emergency light and set color.
    /// </summary>
    private void TurnOn(Entity<EmergencyLightComponent> entity, Color color)
    {
        _pointLight.SetEnabled(entity.Owner, true);
        _pointLight.SetColor(entity.Owner, color);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.Color, color);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.On, true);
        _ambient.SetAmbience(entity.Owner, true);
    }

    private void TurnOn(Entity<EmergencyLightComponent> entity)
    {
        _pointLight.SetEnabled(entity.Owner, true);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.On, true);
        _ambient.SetAmbience(entity.Owner, true);
    }
}
