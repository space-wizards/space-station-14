using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Morgue.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Morgue;

public abstract class SharedCrematoriumSystem : EntitySystem
{
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly StandingStateSystem Standing = default!;
    [Dependency] protected readonly SharedMindSystem Mind = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrematoriumComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CrematoriumComponent, GetVerbsEvent<AlternativeVerb>>(AddCremateVerb);
        SubscribeLocalEvent<ActiveCrematoriumComponent, StorageOpenAttemptEvent>(OnAttemptOpen);
    }

    private void OnExamine(Entity<CrematoriumComponent> ent, ref ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        using (args.PushGroup(nameof(CrematoriumComponent)))
        {
            if (_appearance.TryGetData<bool>(ent.Owner, CrematoriumVisuals.Burning, out var isBurning, appearance) &&
                isBurning)
            {
                args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning",
                    ("owner", ent.Owner)));
            }

            if (_appearance.TryGetData<bool>(ent.Owner, StorageVisuals.HasContents, out var hasContents, appearance) &&
                hasContents)
            {
                args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
            }
        }
    }

    private void OnAttemptOpen(Entity<ActiveCrematoriumComponent> ent, ref StorageOpenAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void AddCremateVerb(EntityUid uid, CrematoriumComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || storage.Open)
            return;

        if (HasComp<ActiveCrematoriumComponent>(uid))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("cremate-verb-get-data-text"),
            // TODO VERB ICON add flame/burn symbol?
            Act = () => TryCremate((uid, component, storage), args.User),
            Impact = LogImpact.High // could be a body? or evidence? I dunno.
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Start the cremation.
    /// </summary>
    public bool Cremate(Entity<CrematoriumComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (HasComp<ActiveCrematoriumComponent>(ent))
            return false;

        _audio.PlayPredicted(ent.Comp.CremateStartSound, ent.Owner, user);
        _audio.PlayPredicted(ent.Comp.CrematingSound, ent.Owner, user);
        _appearance.SetData(ent.Owner, CrematoriumVisuals.Burning, true);

        AddComp<ActiveCrematoriumComponent>(ent);
        ent.Comp.ActiveUntil = _timing.CurTime + ent.Comp.CookTime;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Try to start to start the cremation.
    /// Only works when the crematorium is closed and there are entities inside.
    /// </summary>
    public bool TryCremate(Entity<CrematoriumComponent?, EntityStorageComponent?> ent, EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        if (ent.Comp2.Open || ent.Comp2.Contents.ContainedEntities.Count < 1)
            return false;

        return Cremate((ent.Owner, ent.Comp1), user);
    }

    /// <summary>
    /// Finish the cremation process.
    /// This will delete the entities inside and spawn ash.
    /// </summary>
    private void FinishCooking(Entity<CrematoriumComponent?, EntityStorageComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        _appearance.SetData(ent.Owner, CrematoriumVisuals.Burning, false);
        RemComp<ActiveCrematoriumComponent>(ent);

        if (ent.Comp2.Contents.ContainedEntities.Count > 0)
        {
            for (var i = ent.Comp2.Contents.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var item = ent.Comp2.Contents.ContainedEntities[i];
                _container.Remove(item, ent.Comp2.Contents);
                PredictedDel(item);
            }
            PredictedTrySpawnInContainer(ent.Comp1.LeftOverProtoId, ent.Owner, ent.Comp2.Contents.ID, out _);
        }

        EntityStorage.OpenStorage(ent.Owner, ent.Comp2);

        if (_net.IsServer) // can't predict without the user
            _audio.PlayPvs(ent.Comp1.CremateFinishSound, ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveCrematoriumComponent, CrematoriumComponent>();
        while (query.MoveNext(out var uid, out _, out var crematorium))
        {
            if (curTime < crematorium.ActiveUntil)
                continue;

            FinishCooking((uid, crematorium, null));
        }
    }
}
