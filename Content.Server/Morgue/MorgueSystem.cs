using Content.Server.Morgue.Components;
using Content.Shared.Morgue;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Server.Storage.Components;
using Content.Shared.Body.Components;
using Content.Shared.Storage;

namespace Content.Server.Morgue;

public sealed partial class MorgueSystem : EntitySystem
{
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
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (!args.IsInDetailsRange)
            return;

        appearance.TryGetData(MorgueVisuals.Contents, out MorgueContents contents);
        
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
            app.SetData(MorgueVisuals.Contents, MorgueContents.Empty);
            return;
        }

        var hasMob = false;
        
        foreach (var ent in storage.Contents.ContainedEntities)
        {
            if (!hasMob && HasComp<SharedBodyComponent>(ent))
                hasMob = true;

            if (TryComp<ActorComponent?>(ent, out var actor) && actor.PlayerSession != null)
            {
                app.SetData(MorgueVisuals.Contents, MorgueContents.HasSoul);
                return;
            }
        }

        app.SetData(MorgueVisuals.Contents, hasMob ? MorgueContents.HasMob : MorgueContents.HasContents);
    }

    /// <summary>
    ///     Handles the periodic beeping that morgues do when a live body is inside.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (comp, storage, appearance) in EntityQuery<MorgueComponent, EntityStorageComponent, AppearanceComponent>())
        {
            comp.AccumulatedFrameTime += frameTime;

            CheckContents(comp.Owner, comp, storage, appearance);

            if (comp.AccumulatedFrameTime < comp.BeepTime)
                continue;

            comp.AccumulatedFrameTime -= comp.BeepTime;

            if (comp.DoSoulBeep && appearance.TryGetData(MorgueVisuals.Contents, out MorgueContents contents) && contents == MorgueContents.HasSoul)
            {
                SoundSystem.Play(comp.OccupantHasSoulAlarmSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner);
            }
        }
    }
}
