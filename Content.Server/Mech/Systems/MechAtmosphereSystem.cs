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

    public override void Initialize()
    {
        base.Initialize();

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
            uiDirty |= UpdateCabinPressure(uid, mechComp);

            if (uiDirty && EntityManager.System<UserInterfaceSystem>().IsUiOpen(uid, MechUiKey.Key))
                UpdateMechUi(uid);
        }
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

    private bool UpdateFanModule(EntityUid uid, MechComponent mechComp, float frameTime)
    {
        var fanModule = GetFanModule(uid, mechComp);
        if (fanModule == null || !fanModule.IsActive)
        {
            if (fanModule != null)
            {
                fanModule.State = MechFanState.Off;
                Dirty(uid, mechComp);
            }
            return false;
        }

        var (tankComp, internalAir) = GetGasTank(mechComp);
        if (internalAir == null || tankComp == null)
        {
            fanModule.State = MechFanState.Off;
            Dirty(uid, mechComp);
            return false;
        }

        return ProcessFanOperation(uid, fanModule, tankComp, internalAir, mechComp, frameTime);
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

    private bool ProcessFanOperation(EntityUid uid, MechFanModuleComponent fanModule, GasTankComponent tankComp, GasMixture internalAir, MechComponent mechComp, float frameTime)
    {
        var external = _atmosphere.GetContainingMixture(uid);
        if (external == null || external.Pressure <= 0.05f)
        {
            // No suitable external environment.
            fanModule.State = MechFanState.Idle;
            Dirty(uid, mechComp);
            return false;
        }

        // Check if there are any gases available for sampling (if the filter is active).
        if (fanModule.FilterEnabled && fanModule.FilterGases.Count > 0)
        {
            var hasAllowedGas = false;
            foreach (var gas in Enum.GetValues<Gas>())
            {
                if (!fanModule.FilterGases.Contains(gas) && external.GetMoles(gas) > 0)
                {
                    hasAllowedGas = true;
                    break;
                }
            }

            if (!hasAllowedGas)
            {
                fanModule.State = MechFanState.Idle;
                Dirty(uid, mechComp);
                return false;
            }
        }

        var targetTankPressure = tankComp.MaxOutputPressure;
        var tankPressure = internalAir.Pressure;

        if (tankPressure >= targetTankPressure - 0.1f)
        {
            fanModule.State = MechFanState.Idle;
            Dirty(uid, mechComp);
            return false;
        }

        var energyConsumption = fanModule.EnergyConsumption * frameTime;
        if (!_mech.TryChangeEnergy(uid, -energyConsumption, mechComp))
        {
            fanModule.State = MechFanState.Off;
            Dirty(uid, mechComp);
            return false;
        }

        fanModule.State = MechFanState.On;
        Dirty(uid, mechComp);

        // Pump air from external to internal tank.
        var desiredDeltaP = MathF.Max(0f, targetTankPressure - tankPressure);
        if (desiredDeltaP <= 0)
            return false;

        var neededMoles = desiredDeltaP * internalAir.Volume / (internalAir.Temperature * Atmospherics.R);
        var externalPressure = MathF.Max(external.Pressure, 0.01f);
        var extMolesPerLiter = externalPressure / (Atmospherics.R * external.Temperature);
        var intakeVolume = MathF.Min(fanModule.GasProcessingRate * frameTime, external.Volume);
        var molesAvailableAtRate = extMolesPerLiter * intakeVolume;
        var takeMoles = MathF.Min(neededMoles, molesAvailableAtRate);

        if (takeMoles > 0)
        {
            var removed = external.Remove(takeMoles);

            if (fanModule.FilterEnabled && fanModule.FilterGases.Count > 0)
            {
                // We return the filtered gases back to the external environment.
                var filteredMoles = new GasMixture();
                foreach (var gas in fanModule.FilterGases)
                {
                    filteredMoles.AdjustMoles(gas, removed.GetMoles(gas));
                    removed.SetMoles(gas, 0f);
                }
                _atmosphere.Merge(external, filteredMoles);
            }

            _atmosphere.Merge(internalAir, removed);
            return true;
        }

        return false;
    }

    private bool UpdateCabinPressure(EntityUid uid, MechComponent mechComp)
    {
        if (!TryComp(uid, out MechCabinAirComponent? cabin))
            return false;

        var purgingActive = TryComp<MechCabinPurgeComponent>(uid, out var purgeComp) && purgeComp.CooldownRemaining > 0;
        if (purgingActive || !_mech.TryGetGasModuleAir(uid, out var tankAir) || tankAir == null)
            return false;

        var cabinVolume = cabin.Air.Volume > 0 ? cabin.Air.Volume : Atmospherics.CellVolume;
        var targetMoles = cabin.TargetPressure * cabinVolume / (Atmospherics.R * cabin.Air.Temperature);
        var deficit = targetMoles - cabin.Air.TotalMoles;

        if (deficit > 0f)
        {
            var removed = tankAir.Remove(deficit);
            _atmosphere.Merge(cabin.Air, removed);
            return true;
        }

        return false;
    }

    private void OnAirtightMessage(EntityUid uid, MechComponent component, MechAirtightMessage args)
    {
        // Сannot be airtight if CanAirtight is false.
        component.Airtight = component.CanAirtight && args.IsAirtight;
        Dirty(uid, component);
        UpdateMechUi(uid);
    }

    private void OnFanToggleMessage(EntityUid uid, MechComponent component, MechFanToggleMessage args)
    {
        var fanModule = GetFanModule(uid, component);
        if (fanModule == null)
            return;

        fanModule.IsActive = args.IsActive;
        Dirty(uid, component);
        UpdateMechUi(uid);
    }

    private void OnFilterToggleMessage(EntityUid uid, MechComponent component, MechFilterToggleMessage args)
    {
        var fanModule = GetFanModule(uid, component);
        if (fanModule == null)
            return;

        fanModule.FilterEnabled = args.Enabled;
        Dirty(uid, component);
        UpdateMechUi(uid);
    }

    private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Gas = GetInhaleMixture(component.Mech, mech, args.Respirator?.BreathVolume ?? 0f);
        UpdateMechUi(component.Mech);
    }

    private void OnExhale(EntityUid uid, MechPilotComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Gas = GetExhaleMixture(component.Mech, mech);
        UpdateMechUi(component.Mech);
    }

    private void OnExpose(EntityUid uid, MechPilotComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(component.Mech, out MechComponent? mech))
            return;

        args.Gas = GetExposureMixture(component.Mech, mech, args.Excite);

        args.Handled = true;
        UpdateMechUi(component.Mech);
    }

    private GasMixture? GetInhaleMixture(EntityUid mechUid, MechComponent mechComp, float breathVolume)
    {
        // Closed cabin: construct a breath-volume mixture from cabin air proportionally by pressure.
        if (mechComp.Airtight && TryComp<MechCabinAirComponent>(mechUid, out var cabin))
        {
            var vol = breathVolume > 0f ? breathVolume : Atmospherics.BreathVolume;
            var air = cabin.Air;
            var molesNeeded = air.Volume > 0f ? air.TotalMoles * (vol / air.Volume) : 0f;
            var removed = molesNeeded > 0f ? air.Remove(molesNeeded) : new GasMixture(vol) { Temperature = air.Temperature };
            removed.Volume = vol;
            return removed;
        }

        // Open cabin: use external atmosphere.
        return _atmosphere.GetContainingMixture(mechUid, excite: true);
    }

    private GasMixture? GetExhaleMixture(EntityUid mechUid, MechComponent mechComp)
    {
        // Exhale to cabin when airtight, otherwise to external.
        if (mechComp.Airtight && TryComp<MechCabinAirComponent>(mechUid, out var cabin))
            return cabin.Air;

        return _atmosphere.GetContainingMixture(mechUid, excite: true);
    }

    private GasMixture? GetExposureMixture(EntityUid mechUid, MechComponent mechComp, bool excite)
    {
        if (mechComp.Airtight && TryComp<MechCabinAirComponent>(mechUid, out var cabin))
            return cabin.Air;

        return _atmosphere.GetContainingMixture(mechUid, excite: excite);
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

    private void UpdateMechUi(EntityUid uid)
    {
        RaiseLocalEvent(uid, new UpdateMechUiEvent());
    }
}
