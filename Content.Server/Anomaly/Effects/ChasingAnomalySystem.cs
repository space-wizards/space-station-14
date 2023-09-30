using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;
/// <summary>
/// This component allows the anomaly to chase a random instance of the selected type component within a radius.
/// </summary>
public sealed class ChasingAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChasingAnomalyComponent, AnomalyPulseEvent>(OnPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (anom, trans) in EntityManager.EntityQuery<ChasingAnomalyComponent, TransformComponent>(true))
        {

            if (Deleted(anom.ChasingEntity)) continue;
            if (!anom.ChasingEntity.IsValid()) continue;
            if (anom.ChasingEntity == default!) continue;

            //Calculating direction to the target.
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(anom.ChasingEntity);

            var currPos = new Vector2(trans.MapPosition.X, trans.MapPosition.Y);
            var targetPos = new Vector2(xform.MapPosition.X, xform.MapPosition.Y);
            var delta = targetPos - currPos;

            //Avoiding "shaking" when the anomaly approaches close to the target.
            if (delta.Length() < 1f) continue;

            //Changing the direction of the anomaly.
            var speed = delta.Normalized() * anom.CurrentSpeed;
            _physics.SetLinearVelocity(anom.Owner, speed);
        }

    }

    private void OnPulse(EntityUid uid, ChasingAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        //Speed updating.
        component.CurrentSpeed = args.Severity * component.MaxChasingSpeed;

        //Speed up on supercritical
        if (args.Severity >= 1)
            component.CurrentSpeed *= component.SuperCriticalSpeedModifier;

        //We find our coordinates and calculate the radius of the target search.
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var range = component.MaxChaseRadius * args.Severity;
        var compType = EntityManager.ComponentFactory.GetRegistration(component.ChasingComponent).Type;
        var allEnts = _lookup.GetComponentsInRange(compType, xform.MapPosition, range)
            .Select(x => x.Owner).ToList();

        //If there are no required components in the radius, the pulsation does not work.
        if (allEnts.Count <= 0) return;

        //In the case of finding required components, we choose a random one of them and remember its uid.
        int randomIndex = _random.Next(0, allEnts.Count);
        var randomTarget = allEnts[randomIndex];

        if (xformQuery.TryGetComponent(randomTarget, out var xf)) component.ChasingEntity = xf.Owner;
        else return;
    }
}
