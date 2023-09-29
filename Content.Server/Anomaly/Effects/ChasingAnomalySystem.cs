using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

public sealed class ChasingAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChasingAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ChasingAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (anom, trans) in EntityManager.EntityQuery<ChasingAnomalyComponent, TransformComponent>(true))
        {
            if (anom.ChasingEntity == default!) continue;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(anom.ChasingEntity);

            var currPos = new Vector2(trans.MapPosition.X, trans.MapPosition.Y);
            var targetPos = new Vector2(xform.MapPosition.X, xform.MapPosition.Y);
            var delta = targetPos - currPos;
            delta = delta.Normalized();

            if (delta.Length() < 0.5f) continue;

            var speed = delta * anom.ChasingSpeed;
            _physics.SetLinearVelocity(anom.Owner, speed);
        }

    }

    private void OnPulse(EntityUid uid, ChasingAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        //we find our coordinates and calculate the radius of the target search
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var range = component.MaxChaseRadius * args.Severity;
        var compType = EntityManager.ComponentFactory.GetRegistration(component.ChasingComponent).Type;
        var allEnts = _lookup.GetComponentsInRange(compType, xform.MapPosition, range)
            .Select(x => x.Owner).ToList();

        //If there are no required components in the radius, the pulsation does not work
        if (allEnts.Count <= 0) return;

        //In the case of finding required components, we choose a random one of them and remember its coordinates
        Random random = new Random();
        int randomIndex = random.Next(0, allEnts.Count);
        var randomTarget = allEnts[randomIndex];

        if (xformQuery.TryGetComponent(randomTarget, out var xf)) component.ChasingEntity = xf.Owner;
        else return;
    }

    private void OnSupercritical(EntityUid uid, ChasingAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        component.ChasingSpeed *= component.SuperCriticalSpeedModifier;
    }

}
