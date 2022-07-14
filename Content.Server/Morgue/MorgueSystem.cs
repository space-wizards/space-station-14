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

        if (appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-soul"));
        else if (appearance.TryGetData(MorgueVisuals.HasMob, out bool hasMob) && hasMob)
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-body-has-no-soul"));
        else if (appearance.TryGetData(StorageVisuals.HasContents, out bool hasContents) && hasContents)
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-has-contents"));
        else
            args.PushMarkup(Loc.GetString("morgue-entity-storage-component-on-examine-details-empty"));
    }

    /// <summary>
    ///     Updates data periodically in case something died/got deleted in the morgue.
    /// </summary>
    private void CheckContents(EntityUid uid, MorgueComponent? morgue = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref morgue, ref storage))
            return;

        var hasMob = false;
        var hasSoul = false;

        foreach (var ent in storage.Contents.ContainedEntities)
        {
            if (!hasMob && HasComp<SharedBodyComponent>(ent))
                hasMob = true;
            if (!hasSoul && TryComp<ActorComponent?>(ent, out var actor) && actor.PlayerSession != null)
                hasSoul = true;
        }

        if (TryComp<AppearanceComponent>(uid, out var app))
        {
            app.SetData(MorgueVisuals.HasMob, hasMob);
            app.SetData(MorgueVisuals.HasSoul, hasSoul);
        }
    }

    /// <summary>
    ///     Handles the periodic beeping that morgues do when a live body is inside.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<MorgueComponent>())
        {
            comp.AccumulatedFrameTime += frameTime;

            CheckContents(comp.Owner, comp);

            if (comp.AccumulatedFrameTime < comp.BeepTime)
                continue;
            comp.AccumulatedFrameTime -= comp.BeepTime;

            if (comp.DoSoulBeep && TryComp<AppearanceComponent>(comp.Owner, out var appearance) &&
                appearance.TryGetData(MorgueVisuals.HasSoul, out bool hasSoul) && hasSoul)
            {
                SoundSystem.Play(comp.OccupantHasSoulAlarmSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner);
            }
        }
    }
}
