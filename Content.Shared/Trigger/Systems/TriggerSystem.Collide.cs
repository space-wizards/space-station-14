using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeCollide()
    {
        SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<TriggerOnStepTriggerComponent, StepTriggeredOffEvent>(OnStepTriggered);

        SubscribeLocalEvent<TriggerOnTimedCollideComponent, StartCollideEvent>(OnTimedCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, EndCollideEvent>(OnTimedEndCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, ComponentShutdown>(OnTimedShutdown);
    }

    private void OnCollide(Entity<TriggerOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId == ent.Comp.FixtureID && (!ent.Comp.IgnoreOtherNonHard || args.OtherFixture.Hard))
            Trigger(ent.Owner, args.OtherEntity, ent.Comp.KeyOut);
    }

    private void OnStepTriggered(Entity<TriggerOnStepTriggerComponent> ent, ref StepTriggeredOffEvent args)
    {
        Trigger(ent, args.Tripper, ent.Comp.KeyOut);
    }

    private void OnTimedCollide(Entity<TriggerOnTimedCollideComponent> ent, ref StartCollideEvent args)
    {
        //Ensures the trigger entity will have an active component
        EnsureComp<ActiveTriggerOnTimedCollideComponent>(ent);
        var otherUID = args.OtherEntity;
        if (ent.Comp.Colliding.ContainsKey(otherUID))
            return;
        ent.Comp.Colliding.Add(otherUID, _timing.CurTime + ent.Comp.Threshold);
        Dirty(ent);
    }

    private void OnTimedEndCollide(Entity<TriggerOnTimedCollideComponent> ent, ref EndCollideEvent args)
    {
        var otherUID = args.OtherEntity;
        ent.Comp.Colliding.Remove(otherUID);
        Dirty(ent);

        if (ent.Comp.Colliding.Count == 0)
            RemComp<ActiveTriggerOnTimedCollideComponent>(ent);
    }

    private void OnTimedShutdown(Entity<TriggerOnTimedCollideComponent> ent, ref ComponentShutdown args)
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
                if (curTime > collidingTime)
                {
                    triggerOnTimedCollide.Colliding[collidingEntity] += triggerOnTimedCollide.Threshold;
                    Dirty(uid, triggerOnTimedCollide);
                    Trigger(uid, collidingEntity, triggerOnTimedCollide.KeyOut);
                }
            }
        }
    }
}
