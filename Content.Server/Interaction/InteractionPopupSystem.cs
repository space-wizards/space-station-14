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
    [Dependency] private readonly IEntityManager _entities = default!;

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

        EntityManager.TryGetComponent<MobStateComponent>(uid, out var MobStateComponent);

        string msg = "";

        if (MobStateComponent.IsAlive()) // Only if target is alive and well, not incapacitated or critical.
        {
            if (_random.Prob(component.SuccessChance))
            {
                if (component.InteractSuccessString != null)
                    msg = Loc.GetString(component.InteractSuccessString, ("target", uid)); // Success message (localized).

                if (component.InteractSuccessSound != null)
                    SoundSystem.Play(Filter.Pvs(args.Target), component.InteractSuccessSound.GetSound(), Transform(args.Target).Coordinates);
            }
            else
            {
                if (component.InteractFailureString != null)
                    msg = Loc.GetString(component.InteractFailureString, ("target", uid)); // Failure message (localized).

                if (component.InteractFailureSound != null)
                    SoundSystem.Play(Filter.Pvs(args.Target), component.InteractFailureSound.GetSound(), Transform(args.Target).Coordinates);
            }
        }

        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));

        component.LastInteractTime = curTime;
        args.Handled = true;
    }
}
