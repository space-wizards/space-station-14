using Content.Server.GameTicking;
using Content.Server.Morgue.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Morgue;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Morgue;

public sealed class CrematoriumSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrematoriumComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CrematoriumComponent, GetVerbsEvent<AlternativeVerb>>(AddCremateVerb);
        SubscribeLocalEvent<CrematoriumComponent, SuicideEvent>(OnSuicide);
        SubscribeLocalEvent<ActiveCrematoriumComponent, StorageOpenAttemptEvent>(OnAttemptOpen);
    }

    private void OnExamine(EntityUid uid, CrematoriumComponent component, ExaminedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (_appearance.TryGetData<bool>(uid, CrematoriumVisuals.Burning, out var isBurning, appearance) && isBurning)
        {
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning", ("owner", uid)));
        }
        if (_appearance.TryGetData<bool>(uid, StorageVisuals.HasContents, out var hasContents, appearance) && hasContents)
        {
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
        }
        else
        {
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
        }
    }

    private void OnAttemptOpen(EntityUid uid, ActiveCrematoriumComponent component, ref StorageOpenAttemptEvent args)
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
            Act = () => TryCremate(uid, component, storage),
            Impact = LogImpact.Medium // could be a body? or evidence? I dunno.
        };
        args.Verbs.Add(verb);
    }

    public bool Cremate(EntityUid uid, CrematoriumComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return false;

        if (HasComp<ActiveCrematoriumComponent>(uid))
            return false;

        _audio.PlayPvs(component.CremateStartSound, uid);
        _appearance.SetData(uid, CrematoriumVisuals.Burning, true);

        _audio.PlayPvs(component.CrematingSound, uid);

        AddComp<ActiveCrematoriumComponent>(uid);
        return true;
    }

    public bool TryCremate(EntityUid uid, CrematoriumComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return false;

        if (storage.Open || storage.Contents.ContainedEntities.Count < 1)
            return false;

        return Cremate(uid, component, storage);
    }

    private void FinishCooking(EntityUid uid, CrematoriumComponent component, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return;

        _appearance.SetData(uid, CrematoriumVisuals.Burning, false);
        RemComp<ActiveCrematoriumComponent>(uid);

        if (storage.Contents.ContainedEntities.Count > 0)
        {
            for (var i = storage.Contents.ContainedEntities.Count - 1; i >= 0; i--)
            {
                var item = storage.Contents.ContainedEntities[i];
                storage.Contents.Remove(item);
                EntityManager.DeleteEntity(item);
            }
            var ash = Spawn("Ash", Transform(uid).Coordinates);
            storage.Contents.Insert(ash);
        }

        _entityStorage.OpenStorage(uid, storage);
        _audio.PlayPvs(component.CremateFinishSound, uid);
    }

    private void OnSuicide(EntityUid uid, CrematoriumComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;
        args.SetHandled(SuicideKind.Heat);

        var victim = args.Victim;
        if (TryComp(victim, out ActorComponent? actor) && _minds.TryGetMind(victim, out var mindId, out var mind))
        {
            _ticker.OnGhostAttempt(mindId, false, mind: mind);

            if (mind.OwnedEntity is { Valid: true } entity)
            {
                _popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message"), entity);
            }
        }

        _popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message-others",
            ("victim", Identity.Entity(victim, EntityManager))),
            victim, Filter.PvsExcept(victim), true, PopupType.LargeCaution);

        if (_entityStorage.CanInsert(victim, uid))
        {
            _entityStorage.CloseStorage(uid);
            _standing.Down(victim, false);
            _entityStorage.Insert(victim, uid);
        }
        else
        {
            EntityManager.DeleteEntity(victim);
        }
        _entityStorage.CloseStorage(uid);
        Cremate(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (act, crem) in EntityQuery<ActiveCrematoriumComponent, CrematoriumComponent>())
        {
            act.Accumulator += frameTime;

            if (act.Accumulator >= crem.CookTime)
                FinishCooking(act.Owner, crem);
        }
    }
}
