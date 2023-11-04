using Content.Server.Atmos;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Mech.Components;
using Content.Shared.Atmos;
using Content.Shared.Mech.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.Mech.Systems;

// TODO: this could be reused for gasmask or something if MechAir wasnt a thing
public sealed partial class MechSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private void InitializeFiltering()
    {
        SubscribeLocalEvent<MechAirIntakeComponent, AtmosDeviceUpdateEvent>(OnIntakeUpdate);
        SubscribeLocalEvent<MechAirFilterComponent, AtmosDeviceUpdateEvent>(OnFilterUpdate);
    }

    private void OnIntakeUpdate(EntityUid uid, MechAirIntakeComponent intake, AtmosDeviceUpdateEvent args)
    {
        if (!TryComp<MechComponent>(uid, out var mech) || !mech.Airtight || !TryComp<MechAirComponent>(uid, out var mechAir))
            return;

        // if the mech is filled there is nothing to do
        if (mechAir.Air.Pressure >= intake.Pressure)
            return;

        var environment = _atmosphere.GetContainingMixture(uid, true, true);
        // nothing to intake from
        if (environment == null)
            return;

        // absolute maximum pressure change
        var pressureDelta = args.dt * intake.TargetPressureChange;
        pressureDelta = MathF.Min(pressureDelta, intake.Pressure - mechAir.Air.Pressure);
        if (pressureDelta <= 0)
            return;

        // how many moles to transfer to change internal pressure by pressureDelta
        // ignores temperature difference because lazy
        var transferMoles = pressureDelta * mechAir.Air.Volume / (environment.Temperature * Atmospherics.R);
        _atmosphere.Merge(mechAir.Air, environment.Remove(transferMoles));
    }

    private void OnFilterUpdate(EntityUid uid, MechAirFilterComponent filter, AtmosDeviceUpdateEvent args)
    {
        if (!TryComp<MechComponent>(uid, out var mech) || !mech.Airtight || !TryComp<MechAirComponent>(uid, out var mechAir))
            return;

        var ratio = MathF.Min(1f, args.dt * filter.TransferRate / mechAir.Air.Volume);
        var removed = mechAir.Air.RemoveRatio(ratio);
        // nothing left to remove from the mech
        if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
            return;


        var coordinates = Transform(uid).MapPosition;
        GasMixture? destination = null;
        if (_map.TryFindGridAt(coordinates, out _, out var grid))
        {
            var tile = grid.GetTileRef(coordinates);
            destination = _atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true);
        }

        if (destination != null)
        {
            _atmosphere.ScrubInto(removed, destination, filter.Gases);
        }
        else
        {
            // filtering into space/planet so just discard them
            foreach (var gas in filter.Gases)
            {
                removed.SetMoles(gas, 0f);
            }
        }
        _atmosphere.Merge(mechAir.Air, removed);
    }
}
