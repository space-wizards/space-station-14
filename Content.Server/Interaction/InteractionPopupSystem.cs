using Content.Server.Popups;
using Content.Server.Interaction.Components;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Random;


namespace Content.Server.Interaction;

public sealed class InteractionPopupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InteractionPopupComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var curTime = _gameTiming.CurTime;

        if (curTime < component.LastInteractTime + component.InteractDelay)
            return;

        string msg = "";

        if (!TryComp<MobStateComponent>(uid, out var state) // if it doesn't have a MobStateComponent, e.g. for a window.
            || state.IsAlive())                             // OR if its state is Alive (not dead/incapacitated/critical).
        {
            if (_random.Prob(component.SuccessChance))
            {
                if (component.InteractSuccessString != null)
                    msg = Loc.GetString(component.InteractSuccessString, ("target", uid)); // Success message (localized).

                if (component.InteractSuccessSound != null)
                    SoundSystem.Play(Filter.Pvs(args.Target), component.InteractSuccessSound.GetSound(), args.Target);
            }
            else
            {
                if (component.InteractFailureString != null)
                    msg = Loc.GetString(component.InteractFailureString, ("target", uid)); // Failure message (localized).

                if (component.InteractFailureSound != null)
                    SoundSystem.Play(Filter.Pvs(args.Target), component.InteractFailureSound.GetSound(), args.Target);
            }
        }
        else return;

        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));

        component.LastInteractTime = curTime;
        args.Handled = true;
    }
}
