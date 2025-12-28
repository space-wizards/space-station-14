using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Mech.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Handles atmospheric systems for mechs including air circulation, fans, and life support.
/// </summary>
public sealed class MechAtmosphereSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private const float MinExternalPressure = 0.05f;
    private const float PressureTolerance = 0.1f;

    /// <inheritdoc/>
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
        while (query.MoveNext(out var uid, out var component))
        {
            var uiDirty = false;

            uiDirty |= UpdatePurgeCooldown(uid, frameTime);
            uiDirty |= UpdateFanModule((uid, component), frameTime);
            uiDirty |= UpdateCabinPressure((uid, component));

            if (uiDirty && _ui.IsUiOpen(uid, MechUiKey.Key))
                _mech.UpdateMechUi(uid);
        }
    }

    #region Cabin & Airtight

    public bool TryGetGasModuleAir(Entity<MechComponent> ent, out GasMixture? air)
    {
        air = null;
        foreach (var moduleEnt in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (!HasComp<Shared.Mech.Module.Components.MechAirTankModuleComponent>(moduleEnt))
                continue;

            if (!TryComp<GasTankComponent>(moduleEnt, out var tank))
                continue;

            air = tank.Air;
            return true;
        }

        return false;
    }

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

    private bool UpdateCabinPressure(Entity<MechComponent> ent)
    {
        if (!TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
            return false;

        var purgingActive = TryComp<MechCabinPurgeComponent>(ent.Owner, out var purgeComp) &&
                            purgeComp.CooldownRemaining > 0;
        if (purgingActive || !TryGetGasModuleAir(ent, out var tankAir) || tankAir == null)
            return false;

        return _atmosphere.PumpGasTo(tankAir, cabin.Air, cabin.TargetPressure);
    }

    private void OnAirtightMessage(Entity<MechComponent> ent, ref MechAirtightMessage args)
    {
        // Cannot be airtight if CanAirtight is false.
        ent.Comp.Airtight = ent.Comp.CanAirtight && args.IsAirtight;
        Dirty(ent);
        _mech.UpdateMechUi(ent.Owner);
    }

    private void OnInhale(Entity<MechPilotComponent> ent, ref InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        // Meter a single breath from the cabin using a tank-like regulator pressure.
        if (mechComp.Airtight && TryComp<MechCabinAirComponent>(ent.Comp.Mech, out var cabin))
        {
            var vol = args.Respirator.BreathVolume;
            var breath = new GasMixture(vol)
            {
                Temperature = cabin.Air.Temperature
            };
            var pressure = cabin.RegulatorPressure;
            _atmosphere.PumpGasTo(cabin.Air, breath, pressure);
            args.Gas = breath;
            return;
        }

        args.Gas = _atmosphere.GetContainingMixture(ent.Comp.Mech, excite: true);
    }

    private void OnExhale(Entity<MechPilotComponent> ent, ref ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        args.Gas = GetBreathMixture((ent.Comp.Mech, mechComp));
    }

    private void OnExpose(Entity<MechPilotComponent> ent, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        args.Gas = GetBreathMixture((ent.Comp.Mech, mechComp), args.Excite);
        args.Handled = true;
    }

    private GasMixture? GetBreathMixture(Entity<MechComponent> ent, bool excite = true)
    {
        if (ent.Comp.Airtight && TryComp<MechCabinAirComponent>(ent.Owner, out var cabin))
            return cabin.Air;

        return _atmosphere.GetContainingMixture(ent.Owner, excite: excite);
    }

    #endregion

    #region Fan

    private bool UpdateFanModule(Entity<MechComponent> ent, float frameTime)
    {
        var fanModule = GetFanModule(ent);
        if (fanModule == null || !fanModule.IsActive)
        {
            if (fanModule != null)
                SetFanState(ent, fanModule, MechFanState.Off);

            return false;
        }

        var (tankComp, tankAir) = GetGasTank(ent.Comp);
        if (tankAir == null || tankComp == null)
        {
            SetFanState(ent, fanModule, MechFanState.Off);
            return false;
        }

        return ProcessFanOperation(ent, fanModule, tankComp, tankAir, frameTime);
    }

    private (GasTankComponent? tank, GasMixture? air) GetGasTank(MechComponent mechComp)
    {
        foreach (var ent in mechComp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<Shared.Mech.Module.Components.MechAirTankModuleComponent>(ent, out _) && TryComp<GasTankComponent>(ent, out var tank))
                return (tank, tank.Air);
        }

        return (null, null);
    }

    private bool ProcessFanOperation(Entity<MechComponent> ent,
        MechFanModuleComponent fanModule,
        GasTankComponent tankComp,
        GasMixture tankAir,
        float frameTime)
    {
        var external = _atmosphere.GetContainingMixture(ent.Owner);
        if (external == null
            || external.Pressure <= MinExternalPressure
            || tankAir.Pressure >= tankComp.MaxOutputPressure - PressureTolerance)
        {
            SetFanState(ent, fanModule, MechFanState.Idle);
            return false;
        }

        if (!_mech.TryChangeEnergy(ent.AsNullable(), -fanModule.EnergyConsumption * frameTime))
        {
            SetFanState(ent, fanModule, MechFanState.Off);
            return false;
        }

        var success = ProcessFilteredTransfer(external, tankAir, fanModule, frameTime);

        SetFanState(ent, fanModule, success ? MechFanState.On : MechFanState.Idle);
        return success;
    }

    private bool ProcessFilteredTransfer(GasMixture external,
        GasMixture tankAir,
        MechFanModuleComponent fanModule,
        float frameTime)
    {
        // Calculate transfer volume based on processing rate.
        var transferVolume = fanModule.GasProcessingRate * frameTime;
        if (transferVolume <= 0)
            return false;

        // Remove gas from external environment.
        var removed = external.RemoveVolume(transferVolume);
        if (removed.TotalMoles <= 0)
            return false;

        if (fanModule is { FilterEnabled: true, FilterGases.Count: > 0 })
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

    private void SetFanState(Entity<MechComponent> ent, MechFanModuleComponent fanModule, MechFanState state)
    {
        if (fanModule.State == state)
            return;

        fanModule.State = state;
        Dirty(ent);
    }

    private void OnFanToggleMessage(Entity<MechComponent> ent, ref MechFanToggleMessage args)
    {
        var fanModule = GetFanModule(ent);
        if (fanModule == null)
            return;

        fanModule.IsActive = args.IsActive;

        // Set the correct state based on the toggle.
        var newState = args.IsActive ? MechFanState.On : MechFanState.Off;
        if (fanModule.State != newState)
        {
            fanModule.State = newState;
            Dirty(ent);
        }

        _mech.UpdateMechUi(ent.Owner);
    }

    private void OnFilterToggleMessage(Entity<MechComponent> ent, ref MechFilterToggleMessage args)
    {
        var fanModule = GetFanModule(ent);
        if (fanModule == null)
            return;

        fanModule.FilterEnabled = args.Enabled;
        Dirty(ent);
        _mech.UpdateMechUi(ent.Owner);
    }

    private MechFanModuleComponent? GetFanModule(Entity<MechComponent> ent)
    {
        foreach (var entModule in ent.Comp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<MechFanModuleComponent>(entModule, out var fanModule))
                return fanModule;
        }

        return null;
    }

    #endregion
}
