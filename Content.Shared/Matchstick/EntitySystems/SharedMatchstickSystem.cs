using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Shared.Audio.Systems;
using Content.Shared.Matchstick.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Matchstick.EntitySystems;

public abstract class SharedMatchstickSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MatchstickComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<MatchstickComponent, IsHotEvent>(OnIsHot);
    }

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

    private void OnIsHot(Entity<MatchstickComponent> ent, ref IsHotEvent args)
    {
        args.IsHot |= ent.Comp.State == SmokableState.Lit;
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
        var query = EntityQueryEnumerator<MatchstickComponent>();

        while (query.MoveNext(out var uid, out var match))
        {
            if (match.State != SmokableState.Lit)
                continue;

            CreateMatchstickHotspot((uid, match));

            // Check if the match has expired.
            if (_timing.CurTime > match.TimeMatchWillBurnOut)
                SetState((uid, match), SmokableState.Burnt);
        }
    }

    // Atmos isn't predicted on client so client will do nothing, server will do the actual event.
    protected abstract void CreateMatchstickHotspot(Entity<MatchstickComponent> ent);
}
