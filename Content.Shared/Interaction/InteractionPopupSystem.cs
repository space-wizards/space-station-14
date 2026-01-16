using Content.Shared.Bed.Sleep;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<InteractionPopupComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<InteractionPopupComponent, GetVerbsEvent<ActivationVerb>>(AddVerb);
    }

    private void OnActivateInWorld(EntityUid uid, InteractionPopupComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!args.Complex)
            return;

        if (!component.OnActivate)
            return;

        if (SharedInteract(uid, component, args.Target, args.User))
            args.Handled = true;
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (SharedInteract(uid, component, args.Target, args.User))
            args.Handled = true;
    }

    private bool CanInteract(EntityUid uid, EntityUid target, EntityUid user)
    {
        if (user == target)
            return false;

        if (HasComp<SleepingComponent>(uid))
            return false;

        if (HasComp<SleepingComponent>(target))
            return false;

        if (TryComp<MobStateComponent>(uid, out var state)
            && !_mobStateSystem.IsAlive(uid, state))
        {
            return false;
        }

        return true;
    }

    private bool SharedInteract(
        EntityUid uid,
        InteractionPopupComponent component,
        EntityUid target,
        EntityUid user)
    {
        if (!CanInteract(uid, target, user))
            return false;
        
        var curTime = _gameTiming.CurTime;

        if (curTime < component.LastInteractTime + component.InteractDelay)
            return false;

        component.LastInteractTime = curTime;

        // TODO: Should be an attempt event
        // TODO: Need to handle pausing with an accumulator.

        var msg = ""; // Stores the text to be shown in the popup message
        SoundSpecifier? sfx = null; // Stores the filepath of the sound to be played

        var predict = component.SuccessChance is 0 or 1
                      && component.InteractSuccessSpawn == null
                      && component.InteractFailureSpawn == null;

        if (_netMan.IsClient && !predict)
            return false;

        if (_random.Prob(component.SuccessChance))
        {
            if (component.InteractSuccessString != null)
                msg = Loc.GetString(component.InteractSuccessString, ("target", Identity.Entity(uid, EntityManager))); // Success message (localized).

            if (component.InteractSuccessSound != null)
                sfx = component.InteractSuccessSound;

            if (component.InteractSuccessSpawn != null)
                Spawn(component.InteractSuccessSpawn, _transform.GetMapCoordinates(uid));

            var ev = new InteractionSuccessEvent(user);
            RaiseLocalEvent(target, ref ev);
        }
        else
        {
            if (component.InteractFailureString != null)
                msg = Loc.GetString(component.InteractFailureString, ("target", Identity.Entity(uid, EntityManager))); // Failure message (localized).

            if (component.InteractFailureSound != null)
                sfx = component.InteractFailureSound;

            if (component.InteractFailureSpawn != null)
                Spawn(component.InteractFailureSpawn, _transform.GetMapCoordinates(uid));

            var ev = new InteractionFailureEvent(user);
            RaiseLocalEvent(target, ref ev);
        }

        if (!string.IsNullOrEmpty(component.MessagePerceivedByOthers))
        {
            var msgOthers = Loc.GetString(component.MessagePerceivedByOthers,
                ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));
            _popupSystem.PopupEntity(msgOthers, uid, Filter.PvsExcept(user, entityManager: EntityManager), true);
        }

        if (!predict)
        {
            _popupSystem.PopupEntity(msg, uid, user);

            if (component.SoundPerceivedByOthers)
                _audio.PlayPvs(sfx, target);
            else
                _audio.PlayEntity(sfx, Filter.Entities(user, target), target, false);
            return false;
        }

        _popupSystem.PopupClient(msg, uid, user);

        if (sfx == null)
            return false;

        if (component.SoundPerceivedByOthers)
        {
            _audio.PlayPredicted(sfx, target, user);
            return false;
        }

        if (_netMan.IsClient)
        {
            if (_gameTiming.IsFirstTimePredicted)
                _audio.PlayEntity(sfx, Filter.Local(), target, true);
        }
        else
        {
            _audio.PlayEntity(sfx, Filter.Empty().FromEntities(target), target, false);
        }
        return true;
    }

    private void AddVerb(EntityUid uid, InteractionPopupComponent comp, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !CanInteract(uid, args.Target, args.User))
            return;

        var verb = new ActivationVerb
        {
            Text = Loc.GetString(comp.Verb),
            Act = () =>
            {
                SharedInteract(uid, comp, uid, args.User);
            }
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Sets <see cref="InteractionPopupComponent.InteractSuccessString"/>.
    /// </summary>
    /// <para>
    /// This field is not networked automatically, so this method must be called on both sides of the network.
    /// </para>
    public void SetInteractSuccessString(Entity<InteractionPopupComponent> ent, string str)
    {
        ent.Comp.InteractSuccessString = str;
    }

    /// <summary>
    /// Sets <see cref="InteractionPopupComponent.InteractFailureString"/>.
    /// </summary>
    /// <para>
    /// This field is not networked automatically, so this method must be called on both sides of the network.
    /// </para>
    public void SetInteractFailureString(Entity<InteractionPopupComponent> ent, string str)
    {
        ent.Comp.InteractFailureString = str;
    }
}
