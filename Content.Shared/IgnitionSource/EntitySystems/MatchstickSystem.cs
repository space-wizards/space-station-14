using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Shared.Audio.Systems;
using Content.Shared.IgnitionSource.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.IgnitionSource.EntitySystems;

public sealed partial class MatchstickSystem : EntitySystem
{
    private static readonly EntityTimerId BurnoutTimer = new("burnout");

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedIgnitionSourceSystem _ignition = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MatchstickComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<MatchstickComponent, EntityTimerEvent>(OnBurnout);
    }

    // This is for something *else* lighting the matchstick, not the matchstick lighting something else.
    private void OnInteractUsing(Entity<MatchstickComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(args.Used, isHotEvent);

        if (!isHotEvent.IsHot)
            return;

        args.Handled = TryIgnite(ent, args.User);
    }

    /// <summary>
    ///     Try to light a matchstick!
    /// </summary>
    /// <param name="matchstick">The matchstick to light.</param>
    /// <param name="user">The user lighting the matchstick can be null if there isn't any user.</param>
    /// <returns>True if the matchstick was lit, false otherwise.</returns>
    public bool TryIgnite(Entity<MatchstickComponent> matchstick, EntityUid? user)
    {
        if (matchstick.Comp.State != SmokableState.Unlit)
            return false;

        // Play Sound
        _audio.PlayPredicted(matchstick.Comp.IgniteSound, matchstick, user);

        // Change state
        SetState(matchstick, SmokableState.Lit);
        matchstick.Comp.TimeMatchWillBurnOut = _timing.CurTime + matchstick.Comp.Duration;
        _timers.SetTimerAt(matchstick, BurnoutTimer, matchstick.Comp.TimeMatchWillBurnOut.Value);

        Dirty(matchstick);

        return true;
    }

    private void SetState(Entity<MatchstickComponent> ent, SmokableState newState)
    {
        _lights.SetEnabled(ent, newState == SmokableState.Lit);

        _appearance.SetData(ent, SmokingVisuals.Smoking, newState);

        _ignition.SetIgnited(ent.Owner, newState == SmokableState.Lit);

        switch (newState)
        {
            case SmokableState.Lit:
                _item.SetHeldPrefix(ent, "lit");
                break;
            default:
                _item.SetHeldPrefix(ent, "unlit");
                break;
        }

        ent.Comp.State = newState;
        Dirty(ent);
    }

    private void OnHandleState(Entity<MatchstickComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnBurnout(Entity<MatchstickComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id == BurnoutTimer && ent.Comp.State == SmokableState.Lit)
            SetState(ent, SmokableState.Burnt);
    }

    private void Schedule(Entity<MatchstickComponent> ent)
    {
        if (ent.Comp.State == SmokableState.Lit && ent.Comp.TimeMatchWillBurnOut is {} deadline)
            _timers.SetTimerAt(ent, BurnoutTimer, deadline);
        else
            _timers.CancelTimer<MatchstickComponent>(ent, BurnoutTimer);
    }
}
