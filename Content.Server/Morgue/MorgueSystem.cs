using Content.Shared.Morgue;
using Content.Shared.Morgue.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Morgue;

public sealed class MorgueSystem : SharedMorgueSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorgueComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<MorgueComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextBeep = _timing.CurTime + ent.Comp.NextBeep;
    }

    /// <summary>
    /// Handles the periodic beeping that morgues do when a live body is inside.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<MorgueComponent, EntityStorageComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var storage, out var appearance))
        {
            if (curTime < comp.NextBeep)
                continue;

            comp.NextBeep += comp.BeepTime;

            CheckContents(uid, comp, storage);

            if (comp.DoSoulBeep && _appearance.TryGetData<MorgueContents>(uid, MorgueVisuals.Contents, out var contents, appearance) && contents == MorgueContents.HasSoul)
            {
                _audio.PlayPvs(comp.OccupantHasSoulAlarmSound, uid);
            }
        }
    }
}
