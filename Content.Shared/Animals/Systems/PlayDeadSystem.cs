using Content.Shared.Actions;
using Content.Shared.Animals.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Animals.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class PlayDeadSystem : EntitySystem
{
    [Dependency] private RegenerativeStasisSystem _stasis = default!;
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private IGameTiming _timing = default!;

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<PlayDeadComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!ent.Comp.IsPlayingDead)
        {
            PlayDead(ent, ent.Comp.PlayDeadDuration);
            return;
        }

        //Make sure morty doesn't wake up if they're getting the shit beat out of them
        if (ent.Comp.IsPlayingDead)
        {
            ent.Comp.StopPlayingDeadTime = _timing.CurTime + ent.Comp.PlayDeadDuration;
            Dirty(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlayDeadComponent>();

        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.IsPlayingDead && curTime > comp.StopPlayingDeadTime)
                StopPlayingDead((uid, comp));
        }
    }

    private void PlayDead(Entity<PlayDeadComponent> ent, TimeSpan duration)
    {
        if (ent.Comp.IsPlayingDead)
            return;

        var actions = _action.GetActions(ent);

        //Have to get the action entity to enter stasis
        foreach (var action in actions)
        {
            if (!TryComp<RegenerativeStasisActionComponent>(action, out var regenStasisComp))
                continue;

            if (regenStasisComp.IsInStasis)
                return;

            _stasis.EnterStasis((action, regenStasisComp), ent);
            ent.Comp.IsPlayingDead = true;
            ent.Comp.StopPlayingDeadTime = _timing.CurTime + duration;
            Dirty(ent);
            return;
        }
    }

    private void StopPlayingDead(Entity<PlayDeadComponent> ent)
    {
        if (!ent.Comp.IsPlayingDead)
            return;

        var actions = _action.GetActions(ent);

        foreach (var action in actions)
        {
            if (!TryComp<RegenerativeStasisActionComponent>(action, out var regenStasisComp))
                continue;

            _stasis.ExitStasis((action, regenStasisComp), ent);
            ent.Comp.IsPlayingDead = false;
            Dirty(ent);
            return;
        }
    }
}
