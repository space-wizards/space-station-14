using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;

namespace Content.Server.PDA.Ringer;

public sealed partial class RingerSystem
{
    /// <summary>
    /// Returns if the map supplied allows open uplinks.
    /// </summary>
    /// <param name="map">Map to be checked.</param>
    /// <returns></returns>
    private bool CanUplinkBeOpenedOnMap(EntityUid? map)
    {
        // Uh... I'm just going to put this here as default behaviour since I think this is what used to happen...
        if (map is null)
            return true;

        return !HasComp<LockableUplinkBlockedMapComponent>(map);
    }

    /// <summary>
    /// Checks that a map allows for uplinks to be opened before opening an uplink.
    /// </summary>
    /// <param name="ent">PDA that holds the uplink</param>
    /// <param name="args"></param>
    private void CheckMapBeforeOpenUplink(Entity<RingerUplinkComponent> ent, ref BeforeUplinkOpenEvent args)
    {
        var map = _transform.GetMap((EntityUid)ent);
        if (CanUplinkBeOpenedOnMap(map))
            return;
        args.Canceled = true;
        _popupSystem.PopupEntity(Loc.GetString("uplink-no-connection"),
            ent,
            args.AttemptingClient,
            PopupType.LargeCaution);
    }

    /// <summary>
    /// Locks all open uplinks whenever they are on a map that blocks uplinks.
    /// </summary>
    private void LockUplinksBasedOnMap()
    {
        // Surely there has to be a better way to check if the map has changed...
        // Eh, surely someone will catch it in reviews and tell me how to do it right?
        var uplinkPdaQuery = EntityQueryEnumerator<RingerUplinkComponent>();
        while (uplinkPdaQuery.MoveNext(out var uid, out var ringer))
        {
            if (!ringer.Unlocked)
                continue;

            var map = _transform.GetMap(uid);
            if (CanUplinkBeOpenedOnMap(map))
                continue;

            LockUplink(uid, ringer);
            if (TryComp(uid, out PdaComponent? pda))
                _pda.UpdatePdaUi(uid, pda);

            // "Find" the person holding onto this PDA
            // Again, I feel there must be a better way of doing this, hopefully review will let me know!
            var owner = _transform.GetParentUid(uid);
            _popupSystem.PopupEntity(
                Loc.GetString("uplink-lose-connection"),
                uid,
                owner,
                PopupType.LargeCaution);
        }
    }
}
