using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.StepTrigger;

public sealed class StepTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StepTriggerComponent, ComponentGetState>(TriggerGetState);
        SubscribeLocalEvent<StepTriggerComponent, ComponentHandleState>(TriggerHandleState);

        SubscribeLocalEvent<StepTriggerComponent, StartCollideEvent>(HandleCollide);
    }

    public override void Update(float frameTime)
    {
        foreach (var (_, trigger) in EntityQuery<StepTriggerActiveComponent, StepTriggerComponent>())
        {
            if (!Update(trigger))
                continue;

            RemComp<StepTriggerActiveComponent>(trigger.Owner);
        }
    }

    private bool Update(StepTriggerComponent component)
    {
        if (component.Deleted || !component.Active || component.Colliding.Count == 0)
            return true;

        foreach (var otherUid in component.Colliding.ToArray())
        {
            if (!otherUid.IsValid())
            {
                component.Colliding.Remove(otherUid);
                component.CurrentlySteppedOn.Remove(otherUid);
                component.Dirty();
                continue;
            }

            // TODO: This shouldn't be calculating based on world AABBs.
            var ourAabb = _entityLookup.GetWorldAABB(component.Owner);
            var otherAabb = _entityLookup.GetWorldAABB(otherUid);

            if (!TryComp(otherUid, out PhysicsComponent? otherPhysics) || !ourAabb.Intersects(otherAabb))
            {
                component.Colliding.Remove(otherUid);
                component.CurrentlySteppedOn.Remove(otherUid);
                component.Dirty();
                continue;
            }

            if (component.CurrentlySteppedOn.Contains(otherUid))
                continue;

            if (!CanTrigger(component.Owner, otherUid, component))
                continue;

            if (otherPhysics.LinearVelocity.Length < component.RequiredTriggerSpeed)
                continue;

            var percentage = otherAabb.IntersectPercentage(ourAabb);
            if (percentage < component.IntersectRatio)
                continue;

            var ev = new StepTriggeredEvent { Source = component.Owner, Tripper = otherUid };
            RaiseLocalEvent(component.Owner, ref ev);

            component.CurrentlySteppedOn.Add(otherUid);
            component.Dirty();
        }

        return false;
    }

    private bool CanTrigger(EntityUid uid, EntityUid otherUid, StepTriggerComponent component)
    {
        if (!component.Active || component.CurrentlySteppedOn.Contains(otherUid))
            return false;

        var msg = new StepTriggerAttemptEvent { Source = uid, Tripper = otherUid };

        RaiseLocalEvent(uid, ref msg);

        return msg.Continue;
    }

    private void HandleCollide(EntityUid uid, StepTriggerComponent component, StartCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;

        if (!CanTrigger(uid, otherUid, component))
            return;

        EnsureComp<StepTriggerActiveComponent>(uid);

        component.Colliding.Add(otherUid);
    }

    private static void TriggerHandleState(EntityUid uid, StepTriggerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StepTriggerComponentState state)
            return;

        component.RequiredTriggerSpeed = state.RequiredTriggerSpeed;
        component.IntersectRatio = state.IntersectRatio;
        component.CurrentlySteppedOn.Clear();

        foreach (var slipped in state.CurrentlySteppedOn)
        {
            component.CurrentlySteppedOn.Add(slipped);
        }
    }

    private static void TriggerGetState(EntityUid uid, StepTriggerComponent component, ref ComponentGetState args)
    {
        args.State = new StepTriggerComponentState(
            component.IntersectRatio,
            component.CurrentlySteppedOn.ToArray(),
            component.RequiredTriggerSpeed);
    }

    public void SetIntersectRatio(EntityUid uid, float ratio, StepTriggerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (MathHelper.CloseToPercent(component.IntersectRatio, ratio))
            return;

        component.IntersectRatio = ratio;
        Dirty(component);
    }

    public void SetRequiredTriggerSpeed(EntityUid uid, float speed, StepTriggerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (MathHelper.CloseToPercent(component.RequiredTriggerSpeed, speed))
            return;

        component.RequiredTriggerSpeed = speed;
        Dirty(component);
    }

    public void SetActive(EntityUid uid, bool active, StepTriggerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (active == component.Active)
            return;

        component.Active = active;
        Dirty(component);
    }
}

[ByRefEvent]
public struct StepTriggerAttemptEvent
{
    public EntityUid Source;
    public EntityUid Tripper;
    public bool Continue;
}

[ByRefEvent]
public struct StepTriggeredEvent
{
    public EntityUid Source;
    public EntityUid Tripper;
}
