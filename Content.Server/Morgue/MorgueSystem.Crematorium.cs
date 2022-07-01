using Content.Server.Morgue.Components;
using Content.Shared.Morgue;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Content.Server.Storage.Components;
using System.Threading;
using Content.Shared.Verbs;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Server.Players;

namespace Content.Server.Morgue;

/// <summary>
///     This is the system for morgues but is also used for 
///     crematoriums. Anything with a slab that you stick
///     bodies into would work as well.
/// </summary>
public sealed partial class MorgueSystem : EntitySystem
{
    private void AddCremateVerb(EntityUid uid, MorgueComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!component.IsCrematorium)
            return;

        if (!TryComp<EntityStorageComponent>(component.Tray, out var storage))
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

    public void Cremate(EntityUid uid, MorgueComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.IsCrematorium)
            return;

        if (!Resolve(component.Tray, ref storage))
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

            OpenMorgue(uid, component);

            SoundSystem.Play(component.CremateFinishSound.GetSound(), Filter.Pvs(uid), uid);

        }, component.CremateCancelToken.Token);
    }

    public void TryCremate(EntityUid uid, MorgueComponent component, EntityStorageComponent? storage = null)
    {
        if (!component.IsCrematorium || component.Cooking || component.Open)
            return;

        SoundSystem.Play(component.CremateStartSound.GetSound(), Filter.Pvs(uid), uid);

        Cremate(uid, component, storage);
    }

    private void OnSuicide(EntityUid uid, MorgueComponent component, SuicideEvent args)
    {
        if (args.Handled || !component.IsCrematorium)
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
            Filter.PvsExcept(victim));

        if (_entityStorage.CanInsert(victim, component.Tray))
        {
            CloseMorgue(uid, component);
            _entityStorage.Insert(victim, component.Tray);
            _standing.Down(victim, false);
        }
        else
        {
            EntityManager.DeleteEntity(victim);
        }

        Cremate(uid, component);
    }
}
