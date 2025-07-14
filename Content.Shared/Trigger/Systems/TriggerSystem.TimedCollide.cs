using System.Linq;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeTimedCollide()
    {
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, StartCollideEvent>(OnTimerCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, EndCollideEvent>(OnTimerEndCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnTimerCollide(Entity<TriggerOnTimedCollideComponent> ent, ref StartCollideEvent args)
    {
        //Ensures the trigger entity will have an active component
        EnsureComp<ActiveTriggerOnTimedCollideComponent>(ent);
        var otherUID = args.OtherEntity;
        if (ent.Comp.Colliding.ContainsKey(otherUID))
            return;
        ent.Comp.Colliding.Add(otherUID, _timing.CurTime + ent.Comp.Threshold);
        Dirty(ent);
    }

    private void OnTimerEndCollide(Entity<TriggerOnTimedCollideComponent> ent, ref EndCollideEvent args)
    {
        var otherUID = args.OtherEntity;
        ent.Comp.Colliding.Remove(otherUID);
        //Dirty(ent); TODO: test if needed

        if (ent.Comp.Colliding.Count == 0)
            RemComp<ActiveTriggerOnTimedCollideComponent>(ent);
    }

    private void OnComponentRemove(Entity<TriggerOnTimedCollideComponent> ent, ref ComponentRemove args)
    {
        RemComp<ActiveTriggerOnTimedCollideComponent>(ent);
    }

    private void UpdateTimedCollide()
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveTriggerOnTimedCollideComponent, TriggerOnTimedCollideComponent>();
        while (query.MoveNext(out var uid, out _, out var triggerOnTimedCollide))
        {
            foreach (var (collidingEntity, collidingTime) in triggerOnTimedCollide.Colliding)
            {
                if (collidingTime > curTime)
                {
                    Trigger(uid, collidingEntity, triggerOnTimedCollide.TriggerKey);
                    triggerOnTimedCollide.Colliding[collidingEntity] += triggerOnTimedCollide.Threshold;
                    Dirty(uid, triggerOnTimedCollide);
                }
            }
        }
    }
}
