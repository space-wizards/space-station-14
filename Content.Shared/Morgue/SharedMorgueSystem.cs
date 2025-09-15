using Content.Shared.Mobs.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Morgue.Components;
using Robust.Shared.Player;

namespace Content.Shared.Morgue;

public abstract class SharedMorgueSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorgueComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MorgueComponent, StorageAfterCloseEvent>(OnClosed);
        SubscribeLocalEvent<MorgueComponent, StorageAfterOpenEvent>(OnOpened);
    }

    /// <summary>
    /// Handles the examination text for looking at a morgue.
    /// </summary>
    private void OnExamine(Entity<MorgueComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        _appearance.TryGetData<MorgueContents>(ent.Owner, MorgueVisuals.Contents, out var contents);

        var text = contents switch
        {
            MorgueContents.HasSoul => "morgue-entity-storage-component-on-examine-details-body-has-soul",
            MorgueContents.HasContents => "morgue-entity-storage-component-on-examine-details-has-contents",
            MorgueContents.HasMob => "morgue-entity-storage-component-on-examine-details-body-has-no-soul",
            _ => "morgue-entity-storage-component-on-examine-details-empty"
        };

        args.PushMarkup(Loc.GetString(text));
    }

    private void OnClosed(Entity<MorgueComponent> ent, ref StorageAfterCloseEvent args)
    {
        CheckContents(ent.Owner, ent.Comp);
    }

    private void OnOpened(Entity<MorgueComponent> ent, ref StorageAfterOpenEvent args)
    {
        CheckContents(ent.Owner, ent.Comp);
    }

    /// <summary>
    /// Updates data in case something died/got deleted in the morgue.
    /// </summary>
    public void CheckContents(EntityUid uid, MorgueComponent? morgue = null, EntityStorageComponent? storage = null, AppearanceComponent? app = null)
    {
        if (!Resolve(uid, ref morgue, ref storage, ref app))
            return;

        if (storage.Contents.ContainedEntities.Count == 0)
        {
            _appearance.SetData(uid, MorgueVisuals.Contents, MorgueContents.Empty, app);
            return;
        }

        var hasMob = false;

        foreach (var ent in storage.Contents.ContainedEntities)
        {
            if (!hasMob && HasComp<MobStateComponent>(ent))
                hasMob = true;

            if (HasComp<ActorComponent>(ent))
            {
                _appearance.SetData(uid, MorgueVisuals.Contents, MorgueContents.HasSoul, app);
                return;
            }
        }

        _appearance.SetData(uid, MorgueVisuals.Contents, hasMob ? MorgueContents.HasMob : MorgueContents.HasContents, app);
    }
}
