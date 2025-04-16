using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Shared.Audio.Systems;
using Content.Shared.IgnitionSource.Components;
using Robust.Shared.Timing;

namespace Content.Shared.IgnitionSource.EntitySystems;

public sealed partial class MatchstickSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedIgnitionSourceSystem _ignition = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MatchstickComponent>();

        while (query.MoveNext(out var uid, out var match))
        {
            if (match.State != SmokableState.Lit)
                continue;

            // Check if the match has expired.
            if (_timing.CurTime > match.TimeMatchWillBurnOut)
                SetState((uid, match), SmokableState.Burnt);
        }
    }
}
