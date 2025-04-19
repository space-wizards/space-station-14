using Content.Shared.CCVar;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;

namespace Content.Server.PDA.Ringer;

public sealed partial class RingerSystem
{
    /// <summary>
    /// Returns if the uplink is allowed to be opened.
    /// </summary>
    /// <param name="uplink">Entity containing the uplink.</param>
    /// <returns></returns>
    private bool CanUplinkBeOpened(EntityUid uplink)
    {
        if (!_cfg.GetCVar(CCVars.GameLockUplinks))
            return true;

        var uplinkMapId = _transform.GetMapId(uplink);

        var blockEnumerator = EntityQueryEnumerator<LockableUplinkBlockedMapComponent>();
        while (blockEnumerator.MoveNext(out var uid, out _))
        {
            if (_transform.GetMapId(uid) == uplinkMapId)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks that a map allows for uplinks to be opened before opening an uplink.
    /// </summary>
    /// <param name="ent">PDA that holds the uplink</param>
    /// <param name="args"></param>
    private void CheckMapBeforeOpenUplink(Entity<RingerUplinkComponent> ent, ref BeforeUplinkOpenEvent args)
    {
        var enumerator = EntityQueryEnumerator<LockableUplinkBlockedMapComponent>();
        while (enumerator.MoveNext(out var uid, out _))
        {
            Log.Debug($"{ToPrettyString(uid)}");
        }

        if (CanUplinkBeOpened(ent))
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
        while (uplinkPdaQuery.MoveNext(out var pdaUid, out var ringer))
        {
            if (!ringer.Unlocked)
                continue;

            if (CanUplinkBeOpened(pdaUid))
                continue;

            LockUplink(pdaUid, ringer);
            if (TryComp(pdaUid, out PdaComponent? pda))
                _pda.UpdatePdaUi(pdaUid, pda);

            // "Find" the person holding onto this PDA
            // Again, I feel there must be a better way of doing this, hopefully review will let me know!
            var owner = _transform.GetParentUid(pdaUid);
            _popupSystem.PopupEntity(
                Loc.GetString("uplink-lose-connection"),
                pdaUid,
                owner,
                PopupType.LargeCaution);
        }
    }
}
