using Content.Server.Interaction.Components;
using Content.Server.Popups;
using Content.Shared.Bed.Sleep;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        if (args.Handled || args.User == args.Target)
            return;

        //Handling does nothing and this thing annoyingly plays way too often.
        if (HasComp<SleepingComponent>(uid))
            return;

        args.Handled = true;

        var curTime = _gameTiming.CurTime;

        if (curTime < component.LastInteractTime + component.InteractDelay)
            return;

        if (TryComp<MobStateComponent>(uid, out var state) // if it has a MobStateComponent,
            && !_mobStateSystem.IsAlive(uid, state))                           // AND if that state is not Alive (e.g. dead/incapacitated/critical)
            return;

        // TODO: Should be an attempt event
        // TODO: Need to handle pausing with an accumulator.

        string msg = ""; // Stores the text to be shown in the popup message
        string? sfx = null; // Stores the filepath of the sound to be played

        if (_random.Prob(component.SuccessChance))
        {
            if (component.InteractSuccessString != null)
                msg = Loc.GetString(component.InteractSuccessString, ("target", Identity.Entity(uid, EntityManager))); // Success message (localized).

            if (component.InteractSuccessSound != null)
                sfx = component.InteractSuccessSound.GetSound();

            if (component.InteractSuccessSpawn != null)
                Spawn(component.InteractSuccessSpawn, Transform(uid).MapPosition);
        }
        else
        {
            if (component.InteractFailureString != null)
                msg = Loc.GetString(component.InteractFailureString, ("target", Identity.Entity(uid, EntityManager))); // Failure message (localized).

            if (component.InteractFailureSound != null)
                sfx = component.InteractFailureSound.GetSound();

            if (component.InteractFailureSpawn != null)
                Spawn(component.InteractFailureSpawn, Transform(uid).MapPosition);
        }

        if (component.MessagePerceivedByOthers != null)
        {
            string msgOthers = Loc.GetString(component.MessagePerceivedByOthers,
                ("user", Identity.Entity(args.User, EntityManager)), ("target", Identity.Entity(uid, EntityManager)));
            _popupSystem.PopupEntity(msg, uid, args.User);
            _popupSystem.PopupEntity(msgOthers, uid, Filter.PvsExcept(args.User, entityManager: EntityManager), true);
        }
        else
            _popupSystem.PopupEntity(msg, uid, args.User); //play only for the initiating entity.

        if (sfx is not null) //not all cases will have sound.
        {
            if (component.SoundPerceivedByOthers)
                SoundSystem.Play(sfx, Filter.Pvs(args.Target), args.Target); //play for everyone in range
            else
                SoundSystem.Play(sfx, Filter.Entities(args.User, args.Target), args.Target); //play only for the initiating entity and its target.
        }

        component.LastInteractTime = curTime;
    }
}
