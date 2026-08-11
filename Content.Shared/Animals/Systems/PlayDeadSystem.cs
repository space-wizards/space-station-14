using Content.Shared.Actions;
using Content.Shared.Animals.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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

        if (_action.GetActions<RegenerativeStasisActionComponent>(ent).FirstOrNull() is not { } action)
            return;

        if (!action.Comp2.IsInStasis)
        {
            PlayDead(ent, ent.Comp.PlayDeadDuration);
            return;
        }

        //Make sure morty doesn't wake up if they're getting the shit beat out of them
        if (action.Comp2.IsInStasis && ent.Comp.AutoWake)
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
            if (!comp.AutoWake)
                continue;

            if (curTime < comp.StopPlayingDeadTime)
                continue;

            if (_action.GetActions<RegenerativeStasisActionComponent>(uid).FirstOrNull() is not { } action)
                return;

            if (action.Comp2.IsInStasis)
                StopPlayingDead((uid, comp));
        }
    }

    private void PlayDead(Entity<PlayDeadComponent> ent, TimeSpan duration)
    {
        if (_action.GetActions<RegenerativeStasisActionComponent>(ent).FirstOrNull() is not { } action)
            return;

        if (action.Comp2.IsInStasis)
            return;

        _stasis.EnterStasis((action, action.Comp2), ent);

        ent.Comp.StopPlayingDeadTime = _timing.CurTime + duration;
        ent.Comp.AutoWake = true;
        Dirty(ent);
    }

    private void StopPlayingDead(Entity<PlayDeadComponent> ent)
    {
        if (_action.GetActions<RegenerativeStasisActionComponent>(ent).FirstOrNull() is not { } action)
            return;

        if (!ent.Comp.AutoWake)
            return;

        ent.Comp.AutoWake = false;

        if (action.Comp2.IsInStasis)
            _stasis.ExitStasis((action, action.Comp2), ent);
        Dirty(ent);
    }
}
