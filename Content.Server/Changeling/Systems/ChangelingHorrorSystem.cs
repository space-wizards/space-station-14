using Content.Server.Station.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingHorrorSystem : SharedChangelingHorrorSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private StationSystem _stationSystem = default!;

    protected override void OnAfterTransform(Entity<ChangelingHorrorComponent> ent, ref AfterChangelingTransformEvent ev)
    {
        // transformed into a changeling horror, spawn VFX station-wide, toggle actions, etc
        if (!HasComp<ChangelingHorrorComponent>(ev.StoredIdentity))
            return; // this shouldn't happen...

        // play an "oh shit" sound
        if (ent.Comp.SpawnAnnouncementSound != null)
        {
            var filter = _stationSystem.GetInOwningStation(ent);
            _audio.PlayGlobal(ent.Comp.SpawnAnnouncementSound, filter, true, ent.Comp.SpawnAnnouncementSound.Params);
        }

        base.OnAfterTransform(ent, ref ev);
    }
}
