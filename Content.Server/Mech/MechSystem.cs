using Content.Server.Atmos.EntitySystems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Robust.Shared.Map;

namespace Content.Server.Mech;

public sealed class MechSystem : SharedMechSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IMapManager _map = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechPilotComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<MechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<MechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);
    }

    public override bool TryInsert(EntityUid uid, EntityUid? toInsert, SharedMechComponent? component = null)
    {
        if (!base.TryInsert(uid, toInsert, component))
            return false;

        if (!TryComp<MechComponent>(uid, out var mech))
            return false;

        if (mech.Airtight)
        {
            var coordinates = Transform(uid).MapPosition;
            if (_map.TryFindGridAt(coordinates, out var grid))
            {
                var tile = grid.GetTileRef(coordinates);

                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true) is {} environment)
                {
                    _atmosphere.Merge(mech.Air, environment.RemoveVolume(MechComponent.GasMixVolume));
                }
            }
        }

        return true;
    }

    public override bool TryEject(EntityUid uid, SharedMechComponent? component = null)
    {
        if (!base.TryEject(uid, component))
            return false;

        if (!TryComp<MechComponent>(uid, out var mech))
            return false;

        if (mech.Airtight)
        {
            var coordinates = Transform(uid).MapPosition;
            if (_map.TryFindGridAt(coordinates, out var grid))
            {
                var tile = grid.GetTileRef(coordinates);

                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices, true) is {} environment)
                {
                    _atmosphere.Merge(environment, mech.Air);
                    mech.Air.Clear();
                }
            }
        }

        return true;
    }

    #region Atmos Handling
    private void OnInhale(EntityUid uid, MechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        if (mech.Airtight)
            args.Gas = mech.Air;
    }

    private void OnExhale(EntityUid uid, MechPilotComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        if (mech.Airtight)
            args.Gas = mech.Air;
    }

    private void OnExpose(EntityUid uid, MechPilotComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        args.Gas = mech.Airtight ? mech.Air : _atmosphere.GetContainingMixture(component.Mech);

        args.Handled = true;
    }
    #endregion
}
