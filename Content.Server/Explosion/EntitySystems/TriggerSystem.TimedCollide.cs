using System.Linq;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeTimedCollide()
    {
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, StartCollideEvent>(OnTimerCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, EndCollideEvent>(OnTimerEndCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnTimerCollide(EntityUid uid, TriggerOnTimedCollideComponent component, StartCollideEvent args)
    {
        //Ensures the entity trigger will have an active component
        EnsureComp<ActiveTriggerOnTimedCollideComponent>(uid);
        var otherUID = args.OtherFixture.Body.Owner;
        component.Colliding.Add(otherUID, 0);
    }

    private void OnTimerEndCollide(EntityUid uid, TriggerOnTimedCollideComponent component, EndCollideEvent args)
    {
        var otherUID = args.OtherFixture.Body.Owner;
        component.Colliding.Remove(otherUID);

        if (component.Colliding.Count == 0 && HasComp<ActiveTriggerOnTimedCollideComponent>(uid))
            RemComp<ActiveTriggerOnTimedCollideComponent>(uid);
    }

    private void OnComponentRemove(EntityUid uid, TriggerOnTimedCollideComponent component, ComponentRemove args)
    {
        if (HasComp<ActiveTriggerOnTimedCollideComponent>(uid))
            RemComp<ActiveTriggerOnTimedCollideComponent>(uid);
    }

    private void UpdateTimedCollide(float frameTime)
    {
        foreach (var (activeTrigger, triggerOnTimedCollide) in EntityQuery<ActiveTriggerOnTimedCollideComponent, TriggerOnTimedCollideComponent>())
        {
            foreach (var (collidingEntity, collidingTimer) in triggerOnTimedCollide.Colliding)
            {
                triggerOnTimedCollide.Colliding[collidingEntity] += frameTime;
                if (collidingTimer > triggerOnTimedCollide.Threshold)
                {
                    RaiseLocalEvent(activeTrigger.Owner, new TriggerEvent(activeTrigger.Owner, collidingEntity), true);
                    triggerOnTimedCollide.Colliding[collidingEntity] -= triggerOnTimedCollide.Threshold;
                }
            }
        }
    }
}
