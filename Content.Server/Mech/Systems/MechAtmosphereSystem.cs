using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Mech.Components;
using Content.Server.Mech.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles atmospheric systems for mechs including air circulation, fans, and life support.
/// </summary>
public sealed class MechAtmosphereSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MechSystem _mech = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const float MinExternalPressure = 0.05f;
    private const float PressureTolerance = 0.1f;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechComponent, MechAirtightMessage>(OnAirtightMessage);
        SubscribeLocalEvent<MechComponent, MechFanToggleMessage>(OnFanToggleMessage);
        SubscribeLocalEvent<MechComponent, MechFilterToggleMessage>(OnFilterToggleMessage);

        SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MechComponent>();
        while (query.MoveNext(out var uid, out var mechComp))
        {
            var uiDirty = false;

            uiDirty |= UpdatePurgeCooldown(uid, frameTime);
            uiDirty |= UpdateFanModule(uid, mechComp, frameTime);
            uiDirty |= UpdateCabinPressure(uid, mechComp, frameTime);

            if (uiDirty && _ui.IsUiOpen(uid, MechUiKey.Key))
                UpdateMechUI(uid);
        }
    }

    #region Cabin & Airtight
    private bool UpdatePurgeCooldown(EntityUid uid, float frameTime)
    {
        if (!TryComp<MechCabinPurgeComponent>(uid, out var purge))
            return false;

        if (purge.CooldownRemaining <= 0)
            return false;

        purge.CooldownRemaining -= frameTime;
        if (purge.CooldownRemaining <= 0)
        {
            RemCompDeferred<MechCabinPurgeComponent>(uid);
            return true;
        }

        return false;
    }

    private bool UpdateCabinPressure(EntityUid uid, MechComponent mechComp, float frameTime)
    {
        if (!TryComp(uid, out MechCabinAirComponent? cabin))
            return false;

        var purgingActive = TryComp<MechCabinPurgeComponent>(uid, out var purgeComp) && purgeComp.CooldownRemaining > 0;
        if (purgingActive || !_mech.TryGetGasModuleAir(uid, out var tankAir) || tankAir == null)
            return false;

        return _atmosphere.PumpGasTo(tankAir, cabin.Air, cabin.TargetPressure);
    }

    private void OnAirtightMessage(EntityUid uid, MechComponent component, MechAirtightMessage args)
    {
        // Cannot be airtight if CanAirtight is false.
        component.Airtight = component.CanAirtight && args.IsAirtight;
        Dirty(uid, component);
        UpdateMechUI(uid);
    }

    private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        // Meter a single breath from the cabin using a tank-like regulator pressure.
        if (mech.Airtight && TryComp<MechCabinAirComponent>(component.Mech, out var cabin))
        {
            var vol = args.Respirator?.BreathVolume ?? Atmospherics.BreathVolume;
            var breath = new GasMixture(vol)
            {
                Temperature = cabin.Air.Temperature
            };
            var pressure = cabin.RegulatorPressure;
            _atmosphere.PumpGasTo(cabin.Air, breath, pressure);
            args.Gas = breath;
            return;
        }

        args.Gas = _atmosphere.GetContainingMixture(component.Mech, excite: true);
    }

    private void OnExhale(EntityUid uid, MechPilotComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Gas = GetBreathMixture(component.Mech, mech);
    }

    private void OnExpose(EntityUid uid, MechPilotComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(component.Mech, out MechComponent? mech))
            return;

        args.Gas = GetBreathMixture(component.Mech, mech, args.Excite);
        args.Handled = true;
    }

    private GasMixture? GetBreathMixture(EntityUid mechUid, MechComponent mechComp, bool excite = true)
    {
        if (mechComp.Airtight && TryComp<MechCabinAirComponent>(mechUid, out var cabin))
            return cabin.Air;

        return _atmosphere.GetContainingMixture(mechUid, excite: excite);
    }
    #endregion

    #region Fan
    private bool UpdateFanModule(EntityUid uid, MechComponent mechComp, float frameTime)
    {
        var fanModule = GetFanModule(uid, mechComp);
        if (fanModule == null || !fanModule.IsActive)
        {
            if (fanModule != null)
                SetFanState(uid, fanModule, MechFanState.Off, mechComp);

            return false;
        }

        var (tankComp, tankAir) = GetGasTank(mechComp);
        if (tankAir == null || tankComp == null)
        {
            SetFanState(uid, fanModule, MechFanState.Off, mechComp);
            return false;
        }

        return ProcessFanOperation(uid, fanModule, tankComp, tankAir, mechComp, frameTime);
    }

    private (GasTankComponent? tank, GasMixture? air) GetGasTank(MechComponent mechComp)
    {
        foreach (var ent in mechComp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechAirTankModuleComponent>(ent, out _) && TryComp<GasTankComponent>(ent, out var tank))
                return (tank, tank.Air);
        }
        return (null, null);
    }

    private bool ProcessFanOperation(EntityUid uid, MechFanModuleComponent fanModule, GasTankComponent tankComp, GasMixture tankAir, MechComponent mechComp, float frameTime)
    {
        var external = _atmosphere.GetContainingMixture(uid);
        if (external == null || external.Pressure <= MinExternalPressure)
        {
            SetFanState(uid, fanModule, MechFanState.Idle, mechComp);
            return false;
        }

        if (tankAir.Pressure >= tankComp.MaxOutputPressure - PressureTolerance)
        {
            SetFanState(uid, fanModule, MechFanState.Idle, mechComp);
            return false;
        }

        if (!_mech.TryChangeEnergy(uid, -fanModule.EnergyConsumption * frameTime, mechComp))
        {
            SetFanState(uid, fanModule, MechFanState.Off, mechComp);
            return false;
        }

        var success = ProcessFilteredTransfer(external, tankAir, fanModule, frameTime);

        SetFanState(uid, fanModule, success ? MechFanState.On : MechFanState.Idle, mechComp);
        return success;
    }

    private bool ProcessFilteredTransfer(GasMixture external, GasMixture tankAir, MechFanModuleComponent fanModule, float frameTime)
    {
        // Calculate transfer volume based on processing rate.
        var transferVolume = fanModule.GasProcessingRate * frameTime;
        if (transferVolume <= 0) return false;

        // Remove gas from external environment.
        var removed = external.RemoveVolume(transferVolume);
        if (removed.TotalMoles <= 0) return false;

        if (fanModule.FilterEnabled && fanModule.FilterGases.Count > 0)
        {
            var filtered = new GasMixture { Temperature = removed.Temperature };
            _atmosphere.ScrubInto(removed, filtered, fanModule.FilterGases);

            // Return filtered gases to external environment.
            _atmosphere.Merge(external, filtered);
        }

        // Add remaining gas to internal tank (either unfiltered, or post-scrub remainder).
        _atmosphere.Merge(tankAir, removed);
        return true;
    }

    private void SetFanState(EntityUid uid, MechFanModuleComponent fanModule, MechFanState state, MechComponent mechComp)
    {
        if (fanModule.State != state)
        {
            fanModule.State = state;
            Dirty(uid, mechComp);
        }
    }

    private void OnFanToggleMessage(EntityUid uid, MechComponent component, MechFanToggleMessage args)
    {
        var fanModule = GetFanModule(uid, component);
        if (fanModule == null)
            return;

        fanModule.IsActive = args.IsActive;

        // Set the correct state based on the toggle.
        var newState = args.IsActive ? MechFanState.On : MechFanState.Off;
        if (fanModule.State != newState)
        {
            fanModule.State = newState;
            Dirty(uid, component);
        }

        UpdateMechUI(uid);
    }

    private void OnFilterToggleMessage(EntityUid uid, MechComponent component, MechFilterToggleMessage args)
    {
        var fanModule = GetFanModule(uid, component);
        if (fanModule == null)
            return;

        fanModule.FilterEnabled = args.Enabled;
        Dirty(uid, component);
        UpdateMechUI(uid);
    }

    private MechFanModuleComponent? GetFanModule(EntityUid mech, MechComponent mechComp)
    {
        foreach (var ent in mechComp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechFanModuleComponent>(ent, out var fanModule))
                return fanModule;
        }
        return null;
    }
    #endregion

    private void UpdateMechUI(EntityUid uid)
    {
        RaiseLocalEvent(uid, new UpdateMechUiEvent());
    }
}
