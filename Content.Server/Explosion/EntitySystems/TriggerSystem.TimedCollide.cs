using System.Linq;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    //Stores the entity as active so the event can resolve for each UID colliding with this entity.
    private readonly List<EntityUid> Active = new();

    private void InitializeTimedCollide()
    {
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, StartCollideEvent>(OnTimerCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, EndCollideEvent>(OnTimerEndCollide);
        SubscribeLocalEvent<TriggerOnTimedCollideComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnTimerCollide(EntityUid uid, TriggerOnTimedCollideComponent component, StartCollideEvent args)
    {
        Active.Add(uid);
        var otherUID = args.OtherFixture.Body.Owner;
        component.Colliding.Add(otherUID, 0);
    }

    private void OnTimerEndCollide(EntityUid uid, TriggerOnTimedCollideComponent component, EndCollideEvent args)
    {
        var otherUID = args.OtherFixture.Body.Owner;
        component.Colliding.Remove(otherUID);

        if (component.Colliding.Count == 0)
        {
            Active.Remove(uid);
        }
    }

    private void OnComponentRemove(EntityUid uid, TriggerOnTimedCollideComponent component, ComponentRemove args)
    {
        Active.Remove(uid);
    }

    private void UpdateTimedCollide(float frameTime)
    {
        foreach (var trigger in Active)
        {
            if (!TryComp(trigger, out TriggerOnTimedCollideComponent? component))
                continue;
            foreach (var (collidingEntity, collidingTimer) in component.Colliding)
            {
                component.Colliding[collidingEntity] += frameTime;
                if (collidingTimer > component.Threshold)
                {
                    RaiseLocalEvent(trigger, new TriggerEvent(trigger, collidingEntity));
                }
            }
        }
    }
}
