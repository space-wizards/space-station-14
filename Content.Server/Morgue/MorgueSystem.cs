using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Examine;
using Content.Shared.Morgue;
using Content.Shared.Morgue.Components;
using Robust.Shared.Player;

namespace Content.Server.Morgue;

public sealed class MorgueSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorgueComponent, ExaminedEvent>(OnExamine);
    }

    /// <summary>
    ///     Handles the examination text for looking at a morgue.
    /// </summary>
    private void OnExamine(EntityUid uid, MorgueComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        _appearance.TryGetData<MorgueContents>(uid, MorgueVisuals.Contents, out var contents);

        var text = contents switch
        {
            MorgueContents.HasSoul => "morgue-entity-storage-component-on-examine-details-body-has-soul",
            MorgueContents.HasContents => "morgue-entity-storage-component-on-examine-details-has-contents",
            MorgueContents.HasMob => "morgue-entity-storage-component-on-examine-details-body-has-no-soul",
            _ => "morgue-entity-storage-component-on-examine-details-empty"
        };

        args.PushMarkup(Loc.GetString(text));
    }

    /// <summary>
    ///     Updates data periodically in case something died/got deleted in the morgue.
    /// </summary>
    private void CheckContents(EntityUid uid, MorgueComponent? morgue = null, EntityStorageComponent? storage = null, AppearanceComponent? app = null)
    {
        if (!Resolve(uid, ref morgue, ref storage, ref app))
            return;

        if (storage.Contents.ContainedEntities.Count == 0)
        {
            _appearance.SetData(uid, MorgueVisuals.Contents, MorgueContents.Empty);
            return;
        }

        var hasMob = false;

        foreach (var ent in storage.Contents.ContainedEntities)
        {
            if (!hasMob && HasComp<BodyComponent>(ent))
                hasMob = true;

            if (HasComp<ActorComponent>(ent))
            {
                _appearance.SetData(uid, MorgueVisuals.Contents, MorgueContents.HasSoul, app);
                return;
            }
        }

        _appearance.SetData(uid, MorgueVisuals.Contents, hasMob ? MorgueContents.HasMob : MorgueContents.HasContents, app);
    }

    /// <summary>
    ///     Handles the periodic beeping that morgues do when a live body is inside.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MorgueComponent, EntityStorageComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var storage, out var appearance))
        {
            comp.AccumulatedFrameTime += frameTime;

            CheckContents(uid, comp, storage);

            if (comp.AccumulatedFrameTime < comp.BeepTime)
                continue;

            comp.AccumulatedFrameTime -= comp.BeepTime;

            if (comp.DoSoulBeep && _appearance.TryGetData<MorgueContents>(uid, MorgueVisuals.Contents, out var contents, appearance) && contents == MorgueContents.HasSoul)
            {
                _audio.PlayPvs(comp.OccupantHasSoulAlarmSound, uid);
            }
        }
    }
}
