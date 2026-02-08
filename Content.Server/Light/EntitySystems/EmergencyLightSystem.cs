using Content.Server.Audio;
using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.AlertLevel;
using Content.Shared.Examine;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server.Light.EntitySystems;

public sealed class EmergencyLightSystem : SharedEmergencyLightSystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmergencyLightComponent, EmergencyLightEvent>(OnEmergencyLightEvent);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<EmergencyLightComponent, ExaminedEvent>(OnEmergencyExamine);
        SubscribeLocalEvent<EmergencyLightComponent, PowerChangedEvent>(OnEmergencyPower);
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
            if (!TryComp<AlertLevelComponent>(_station.GetOwningStation(uid), out var alertLevelComp))
                return;

            if (!_prototype.Resolve(alertLevelComp.CurrentAlertLevel, out var level))
                return;

            args.PushMarkup(
                Loc.GetString("emergency-light-component-on-examine-alert",
                    ("color", level.Color.ToHex()),
                    ("level", Loc.GetString($"alert-level-{alertLevelComp.CurrentAlertLevel}"))));
        }
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

    private void OnAlertLevelChanged(ref AlertLevelChangedEvent ev)
    {
        if (!_prototype.Resolve(ev.AlertLevel, out var level))
            return;

        var query = EntityQueryEnumerator<EmergencyLightComponent, PointLightComponent, AppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var light, out var pointLight, out var appearance, out var xform))
        {
            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != ev.Station)
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
        if (entity.Comp.State == EmergencyLightState.On)
        {
            if (!_battery.TryUseCharge((entity.Owner, battery), entity.Comp.Wattage * frameTime))
            {
                SetState(entity.Owner, entity.Comp, EmergencyLightState.Empty);
                TurnOff(entity);
            }
        }
        else
        {
            _battery.ChangeCharge((entity.Owner, battery), entity.Comp.ChargingWattage * frameTime * entity.Comp.ChargingEfficiency);
            if (_battery.IsFull((entity.Owner, battery)))
            {
                if (TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver))
                {
                    receiver.Load = 1;
                }

                SetState(entity.Owner, entity.Comp, EmergencyLightState.Full);
            }
        }
    }

    /// <summary>
    ///     Updates the light's power drain, battery drain, sprite and actual light state.
    /// </summary>
    public void UpdateState(Entity<EmergencyLightComponent> entity)
    {
        if (!TryComp<ApcPowerReceiverComponent>(entity.Owner, out var receiver))
            return;

        if (!TryComp<AlertLevelComponent>(_station.GetOwningStation(entity.Owner), out var alertLevelComp)
            || !_prototype.Resolve(alertLevelComp.CurrentAlertLevel, out var level))
        {
            TurnOff(entity, Color.Red); // if no alert, default to off red state
            return;
        }

        if (receiver.Powered && !entity.Comp.ForciblyEnabled) // Green alert
        {
            receiver.Load = (int)Math.Abs(entity.Comp.Wattage);
            TurnOff(entity, level.EmergencyLightColor);
            SetState(entity.Owner, entity.Comp, EmergencyLightState.Charging);
        }
        else if (!receiver.Powered) // If internal battery runs out it will end in off red state
        {
            TurnOn(entity, Color.Red);
            SetState(entity.Owner, entity.Comp, EmergencyLightState.On);
        }
        else // Powered and enabled
        {
            TurnOn(entity, level.EmergencyLightColor);
            SetState(entity.Owner, entity.Comp, EmergencyLightState.On);
        }
    }

    private void TurnOff(Entity<EmergencyLightComponent> entity)
    {
        _pointLight.SetEnabled(entity.Owner, false);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.On, false);
        _ambient.SetAmbience(entity.Owner, false);
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

    private void TurnOn(Entity<EmergencyLightComponent> entity)
    {
        _pointLight.SetEnabled(entity.Owner, true);
        _appearance.SetData(entity.Owner, EmergencyLightVisuals.On, true);
        _ambient.SetAmbience(entity.Owner, true);
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
}
