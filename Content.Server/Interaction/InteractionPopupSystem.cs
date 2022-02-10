using Content.Server.Popups;
using Content.Server.Interaction.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.MobState;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
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
        SubscribeLocalEvent<InteractionPopupComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(EntityUid uid, InteractionPopupComponent component, MobStateChangedEvent args)
    {
        var originalSuccessChance = component.SuccessChance; // Stores this for later in case mob state changes back and forth.

        if (!args.Component.IsAlive()) // if the mob being interacted with is not alive (dead, incapacitated, etc.)
        {
            component.SuccessChance = -1.0f; // set to the "invalid" value, which suppresses all popups and sound effects.
            return;
        }
        else // if the mob being interacted with is alive and well
        {
            component.SuccessChance = originalSuccessChance;
            return;
        }
    }

    private void OnInteractHand(EntityUid uid, InteractionPopupComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.InteractDelay.TotalSeconds <= 0)
            return;

        if (_gameTiming.CurTime < component.LastInteractTime + component.InteractDelay)
            return;

        if (component.SuccessChance == -1.0f) // for the special "invalid" case where SuccessChance is -1 (e.g. target is deceased), suppress both popup and sound effect.
            return;


        string msg = "";

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

        _popupSystem.PopupEntity(msg, uid, Filter.Pvs(uid));

        component.LastInteractTime = _gameTiming.CurTime;
        args.Handled = true;
    }
}
