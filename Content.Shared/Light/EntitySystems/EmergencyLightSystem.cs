using Content.Shared.AlertLevel;
using Content.Shared.Audio;
using Content.Shared.Examine;
using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Color = Robust.Shared.Maths.Color;

namespace Content.Shared.Light.EntitySystems;

/// <summary>
/// System that handles the emergency light <see cref="EmergencyLightComponent"/>.
/// </summary>
public sealed partial class EmergencyLightSystem : EntitySystem
{
    [Dependency] private AlertLevelSystem _alert = default!;
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedStationSystem _station = default!;

    [Dependency] private EntityQuery<StationMemberComponent> _stationMemberQuery;

    [SubscribeLocalEvent]
    private void OnEmergencyPower(Entity<EmergencyLightComponent> ent, ref PowerChangedEvent args)
    {
        var meta = MetaData(ent.Owner);

        // TODO: PowerChangedEvent shouldn't be issued for paused ents but this is the world we live in.
        if (meta.EntityLifeStage >= EntityLifeStage.Terminating ||
            meta.EntityPaused)
        {
            return;
        }

        UpdateState(ent);
    }

    [SubscribeLocalEvent]
    private void OnEmergencyExamine(Entity<EmergencyLightComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(EmergencyLightComponent)))
        {
            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine",
                    ("batteryStateText",
                        Loc.GetString(ent.Comp.BatteryStateText[ent.Comp.State]))));

            // Show alert level on the light itself.
            if (_station.GetOwningStation(ent.Owner) is not { } station)
                return;

            if (!_alert.TryGetLevel(station, out var level)
                || !ProtoMan.Resolve(level, out var proto))
                return;

            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine-alert",
                    ("color", proto.Color.ToHex()),
                    ("level", proto.LocalizedName)));
        }
    }

    [SubscribeLocalEvent]
    private void OnEmergencyLightEvent(Entity<EmergencyLightComponent> ent, ref EmergencyLightEvent args)
    {
        switch (args.State)
        {
            case EmergencyLightState.On:
            case EmergencyLightState.Charging:
                EnsureComp<ActiveEmergencyLightComponent>(ent);
                break;
            case EmergencyLightState.Full:
            case EmergencyLightState.Empty:
                RemComp<ActiveEmergencyLightComponent>(ent);
                break;
        }
    }

    [SubscribeLocalEvent]
    private void OnAlertLevelChanged(ref AlertLevelChangedEvent ev)
    {
        if (!ProtoMan.Resolve(ev.AlertLevel, out var level))
            return;

        var query = EntityQueryEnumerator<EmergencyLightComponent, SharedPointLightComponent, AppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var light, out var pointLight, out var appearance, out var xform))
        {
            if (!_stationMemberQuery.TryComp(xform.GridUid, out var stationMember)
                || stationMember.Station != ev.Station)
                continue;

            _pointLight.SetColor(uid, level.EmergencyLightColor, pointLight);
            _appearance.SetData(uid, EmergencyLightVisuals.Color, level.EmergencyLightColor, appearance);

            if (level.ForceEnableEmergencyLights && !light.ForciblyEnabled)
            {
                light.ForciblyEnabled = true;
                TurnOn((uid, light));
            }
            else if (!level.ForceEnableEmergencyLights && light.ForciblyEnabled)
            {
                // Previously forcibly enabled, and we went down an alert level.
                light.ForciblyEnabled = false;
                UpdateState((uid, light));
            }

            Dirty(uid, light);
        }
    }

    /// <summary>
    /// Sets the state of the emergency light.
    /// </summary>
    public void SetState(Entity<EmergencyLightComponent> ent, EmergencyLightState state)
    {
        if (ent.Comp.State == state)
            return;

        ent.Comp.State = state;
        Dirty(ent);
        RaiseLocalEvent(ent, new EmergencyLightEvent(state));
    }

    /// <inheritdoc />
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveEmergencyLightComponent, EmergencyLightComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out _, out var emergencyLight, out var battery))
        {
            Update((uid, emergencyLight), battery, frameTime);
        }
    }

    private void Update(Entity<EmergencyLightComponent> ent, BatteryComponent battery, float frameTime)
    {
        if (ent.Comp.State == EmergencyLightState.On)
        {
            if (_battery.TryUseCharge((ent.Owner, battery), ent.Comp.Wattage * frameTime))
                return;

            SetState(ent, EmergencyLightState.Empty);
            TurnOff(ent);
        }
        else
        {
            _battery.ChangeCharge((ent.Owner, battery), ent.Comp.ChargingWattage * frameTime * ent.Comp.ChargingEfficiency);
            if (!_battery.IsFull((ent.Owner, battery)))
                return;

            if (TryComp<SharedApcPowerReceiverComponent>(ent.Owner, out var receiver))
                receiver.Load = 1;

            SetState(ent, EmergencyLightState.Full);
        }
    }

    /// <summary>
    /// Updates the light's power drain, battery drain, sprite and actual light state.
    /// </summary>
    public void UpdateState(Entity<EmergencyLightComponent> ent)
    {
        if (!TryComp<SharedApcPowerReceiverComponent>(ent.Owner, out var receiver))
            return;

        // Show alert level on the light itself.
        if (_station.GetOwningStation(ent.Owner) is not { } station
            || !_alert.TryGetLevel(station, out var level)
            || !ProtoMan.Resolve(level, out var proto))
        {
            TurnOff(ent, Color.Red); // if no alert, default to off red state.
            return;
        }

        switch (receiver.Powered)
        {
            // Green alert.
            case true when !ent.Comp.ForciblyEnabled:
                receiver.Load = (int)Math.Abs(ent.Comp.Wattage);
                TurnOff(ent, proto.EmergencyLightColor);
                SetState(ent, EmergencyLightState.Charging);
                break;
            // If internal battery runs out it will end in off red state.
            case false:
                TurnOn(ent, Color.Red);
                SetState(ent, EmergencyLightState.On);
                break;
            // Powered and enabled.
            default:
                TurnOn(ent, proto.EmergencyLightColor);
                SetState(ent, EmergencyLightState.On);
                break;
        }
    }

    private void TurnOff(Entity<EmergencyLightComponent> ent)
    {
        _pointLight.SetEnabled(ent.Owner, false);
        _appearance.SetData(ent.Owner, EmergencyLightVisuals.On, false);
        _ambient.SetAmbience(ent.Owner, false);
    }

    /// <summary>
    /// Turn off emergency light and set color.
    /// </summary>
    private void TurnOff(Entity<EmergencyLightComponent> ent, Color color)
    {
        _pointLight.SetEnabled(ent.Owner, false);
        _pointLight.SetColor(ent.Owner, color);
        _appearance.SetData(ent.Owner, EmergencyLightVisuals.Color, color);
        _appearance.SetData(ent.Owner, EmergencyLightVisuals.On, false);
        _ambient.SetAmbience(ent.Owner, false);
    }

    private void TurnOn(Entity<EmergencyLightComponent> ent)
    {
        _pointLight.SetEnabled(ent.Owner, true);
        _appearance.SetData(ent.Owner, EmergencyLightVisuals.On, true);
        _ambient.SetAmbience(ent.Owner, true);
    }

    /// <summary>
    /// Turn on emergency light and set color.
    /// </summary>
    private void TurnOn(Entity<EmergencyLightComponent> ent, Color color)
    {
        _pointLight.SetEnabled(ent.Owner, true);
        _pointLight.SetColor(ent.Owner, color);
        _appearance.SetData(ent.Owner, EmergencyLightVisuals.Color, color);
        _appearance.SetData(ent.Owner, EmergencyLightVisuals.On, true);
        _ambient.SetAmbience(ent.Owner, true);
    }
}
