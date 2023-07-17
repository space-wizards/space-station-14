using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Robust.Shared.Timing;

namespace Content.Server.ThermoelectricGenerator;

public sealed class ThermoelectricGeneratorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ThermoelectricGeneratorComponent, AtmosDeviceUpdateEvent>(OnGeneratorUpdated);
    }

    private void OnGeneratorUpdated(EntityUid uid, ThermoelectricGeneratorComponent component, AtmosDeviceUpdateEvent args)
    {
        if (!TryComp(uid, out NodeContainerComponent? nodeContainer)
            || !TryComp(uid, out AtmosDeviceComponent? device)
            || !nodeContainer.TryGetNode(component.ColdLoopName, out PipeNode? cold)
            || !nodeContainer.TryGetNode(component.HotLoopName, out PipeNode? hot))
        {
            return;
        }

        // thank you ilya very cool
        var hotTransferred = hot.Air.RemoveVolume((float)(component.TransferRate * (_gameTiming.CurTime - device.LastProcess).TotalSeconds));
        var coldTransferred = cold.Air.RemoveVolume((float)(component.TransferRate * (_gameTiming.CurTime - device.LastProcess).TotalSeconds));
        // code copied from volumetric pump, assuming it'd be pretty easy to make it work
        // also yeah add TransferRate to the component, maybe set it to like 1000 by default
        var hotTemp = hotTransferred.Temperature;
        var coldTemp = coldTransferred.Temperature;
        var hotCapacity = _atmosphereSystem.GetHeatCapacity(hotTransferred);
        var coldCapacity = _atmosphereSystem.GetHeatCapacity(coldTransferred);
        var finalTemp = (hotTemp * hotCapacity + coldTemp * coldCapacity) / (hotCapacity + coldCapacity);
        var heatTransferred = (finalTemp - coldTemp) * coldCapacity;
        hotTransferred.Temperature = finalTemp;
        coldTransferred.Temperature = finalTemp;
        _atmosphereSystem.Merge(hot.Air, hotTransferred);
        _atmosphereSystem.Merge(cold.Air, coldTransferred);

        if (TryComp<PowerSupplierComponent>(uid, out var powerSupplierComponent))
            powerSupplierComponent.MaxSupply = heatTransferred * component.PowerOutput; // also add PowerOutput to the component
    }
}
