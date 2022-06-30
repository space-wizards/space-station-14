using Content.Server.Morgue.Components;
using Content.Shared.Morgue;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared.Standing;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Body.Components;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Shared.Interaction;
using System.Linq;
using Robust.Shared.Physics;
using Content.Shared.Verbs;
using Content.Shared.Database;
using System.Threading;
using Content.Shared.Interaction.Events;
using Content.Server.Players;

namespace Content.Server.Morgue;

public sealed class CrematoriumeEntityStorageSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly StandingStateSystem _stando = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly MorgueEntityStorageSystem _morgue = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrematoriumEntityStorageComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CrematoriumEntityStorageComponent, GetVerbsEvent<AlternativeVerb>>(AddCremateVerb);
        SubscribeLocalEvent<CrematoriumEntityStorageComponent, ActivateInWorldEvent>(OnActivate,
            before: new[] { typeof(MorgueEntityStorageSystem), typeof(EntityStorageSystem) });
        SubscribeLocalEvent<CrematoriumEntityStorageComponent, SuicideEvent>(OnSuicide);
    }

    private void OnActivate(EntityUid uid, CrematoriumEntityStorageComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (!TryComp<EntityStorageComponent>(uid, out var storage) || !TryComp<MorgueEntityStorageComponent>(uid, out var morgue))
            return;

        if (storage.Open)
        {
            _morgue.CloseMorgue(uid, morgue);
        }
        else if (!component.Cooking && _morgue.CanOpen(args.User, uid, morgue))
        {
            _morgue.OpenMorgue(uid, morgue);
        }
    }

    public void TryCremate(EntityUid uid, CrematoriumEntityStorageComponent component, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return;

        if (component.Cooking || storage.Open)
            return;

        SoundSystem.Play(component.CremateStartSound.GetSound(), Filter.Pvs(uid), uid);

        Cremate(uid, component, storage);
    }

    public void Cremate(EntityUid uid, CrematoriumEntityStorageComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return;

        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(CrematoriumVisuals.Burning, true);
        component.Cooking = true;

        SoundSystem.Play(component.CrematingSound.GetSound(), Filter.Pvs(uid), uid);

        component.CremateCancelToken?.Cancel();
        
        component.CremateCancelToken = new CancellationTokenSource();
        uid.SpawnTimer(component.BurnMilis, () =>
        {
            if (Deleted(uid))
                return;
            if (TryComp<AppearanceComponent>(uid, out var app))
                app.SetData(CrematoriumVisuals.Burning, false);
            component.Cooking = false;

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

            _morgue.OpenMorgue(uid, storage: storage);

            SoundSystem.Play(component.CremateFinishSound.GetSound(), Filter.Pvs(uid), uid);

        }, component.CremateCancelToken.Token);
    }

    private void AddCremateVerb(EntityUid uid, CrematoriumEntityStorageComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (!args.CanAccess || !args.CanInteract || args.Hands == null || component.Cooking || storage.Open)
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

    private void OnExamined(EntityUid uid, CrematoriumEntityStorageComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (app.TryGetData(CrematoriumVisuals.Burning, out bool isBurning) && isBurning)
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-is-burning", ("owner", uid)));

        if (app.TryGetData(MorgueVisuals.HasContents, out bool hasContents) && hasContents)
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-has-contents"));
        else
            args.PushMarkup(Loc.GetString("crematorium-entity-storage-component-on-examine-details-empty"));
    }

    private void OnSuicide(EntityUid uid, CrematoriumEntityStorageComponent component, SuicideEvent args)
    {
        if (args.Handled)
            return;
        args.SetHandled(SuicideKind.Heat);

        var victim = args.Victim;
        if (TryComp(victim, out ActorComponent? actor) && actor.PlayerSession.ContentData()?.Mind is { } mind)
        {
            _ticker.OnGhostAttempt(mind, false);

            if (mind.OwnedEntity is { Valid: true } entity)
            {
                _popup.PopupEntity(Loc.GetString("crematorium-entity-storage-component-suicide-message"), entity, Filter.Pvs(entity));
            }
        }

        _popup.PopupEntity(
            Loc.GetString("crematorium-entity-storage-component-suicide-message-others", ("victim", victim)),
            victim,
            Filter.Pvs(victim, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == victim));

        if (_entityStorage.CanInsert(victim, uid))
        {
            _morgue.CloseMorgue(uid);
            _entityStorage.Insert(victim, uid);
            _stando.Down(victim, false);
        }
        else
        {
            EntityManager.DeleteEntity(victim);
        }

        Cremate(uid, component);
    }
}
