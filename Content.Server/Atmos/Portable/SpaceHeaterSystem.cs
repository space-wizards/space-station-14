using Content.Server.Atmos.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos.Portable;

namespace Content.Server.Atmos.Portable;

/// <summary>
/// Server side implementation of space heater.
/// When space heater is auto mode it uses atmos data to flip it between cooling and heating mode.
/// If you are searching for actual atmos logic it's handled by <seealso cref="GasThermoMachineSystem"/>
/// </summary>
public sealed partial class SpaceHeaterSystem : SharedSpaceHeaterSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private PowerReceiverSystem _receiver = default!;
    [Dependency] private EntityQuery<GasThermoMachineComponent> _thermoQuery = default!;

    [SubscribeLocalEvent]
    private void OnDeviceUpdateded(Entity<SpaceHeaterComponent> ent, ref AtmosDeviceUpdateEvent args)
    {
        if (!_thermoQuery.TryComp(ent, out var thermoMachine))
            return;

        UpdateAppearance(ent, thermoMachine);
        // If in automatic temperature mode, check if we need to adjust the heat exchange direction
        if (ent.Comp.Mode != SpaceHeaterMode.Auto || !_receiver.IsPowered(ent))
            return;

        var environment = _atmosphereSystem.GetContainingMixture(ent.Owner, args.Grid, args.Map);
        if (environment == null)
            return;

        float tolerance = thermoMachine.TemperatureTolerance + ent.Comp.AutoModeSwitchThreshold;

        if (environment.Temperature <= thermoMachine.TargetTemperature - tolerance)
        {
            thermoMachine.Cp = ent.Comp.HeatingCp;
            Dirty(ent.Owner, thermoMachine);
        }
        else if (environment.Temperature >= thermoMachine.TargetTemperature + tolerance)
        {
            thermoMachine.Cp = ent.Comp.CoolingCp;
            Dirty(ent.Owner, thermoMachine);
        }
    }
}
