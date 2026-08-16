using Content.Server.Animals.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <inheritdoc cref="SatiationProductionComponent"/>
public sealed partial class SatiationProductionSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private EntityQuery<ActorComponent> _actorQuery;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SatiationProductionComponent>();
        while (query.MoveNext(out var uid, out var producer))
        {
            if (!producer.Automatic)
                continue;

            var producerUid = GetProducer((uid, producer));
            if (!producer.AutomaticForPlayers && _actorQuery.HasComp(producerUid))
                continue;

            if (_timing.CurTime < producer.NextProductionTime)
                continue;

            producer.NextProductionTime += GetDelay(producer);
            TryProduce((uid, producer), out _);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SatiationProductionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextProductionTime = _timing.CurTime + GetDelay(ent.Comp);
    }

    /// <summary>
    /// Attempts production immediately, independently of the automatic timer.
    /// </summary>
    public bool TryProduce(
        Entity<SatiationProductionComponent?> ent,
        out SatiationProductionFailure failure)
    {
        failure = SatiationProductionFailure.ProductUnavailable;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var owner = GetProducer((ent.Owner, ent.Comp));
        if (_mobState.IsDead(owner))
        {
            failure = SatiationProductionFailure.Dead;
            return false;
        }

        if (_satiationQuery.TryComp(owner, out var satiation) &&
            satiation.Has(ent.Comp.SatiationType) &&
            !HasEnoughSatiation(ent.Comp, (owner, satiation)))
        {
            failure = SatiationProductionFailure.InsufficientSatiation;
            return false;
        }

        var ev = new ProductionAttemptEvent(owner);
        RaiseLocalEvent(ent.Owner, ref ev);
        if (!ev.Produced)
            return false;

        if (satiation != null)
            _satiation.ModifyValue((owner, satiation), ent.Comp.SatiationType, -ent.Comp.SatiationUsage);

        failure = SatiationProductionFailure.None;
        return true;
    }

    private bool HasEnoughSatiation(
        SatiationProductionComponent component,
        Entity<SatiationComponent> satiation)
    {
        if (component.MinimumSatiationThreshold is { } threshold &&
            !_satiation.IsValueInRange(
                satiation,
                component.SatiationType,
                above: threshold,
                hypotheticalValueDelta: -component.SatiationUsage))
        {
            return false;
        }

        return component.MinimumSatiation is not { } minimum ||
               _satiation.GetValueOrNull(satiation, component.SatiationType) >= minimum;
    }

    private EntityUid GetProducer(Entity<SatiationProductionComponent> ent)
    {
        return ent.Comp.Producer switch
        {
            SatiationProductionOwner.Parent => Transform(ent).ParentUid,
            _ => ent.Owner
        };
    }

    private TimeSpan GetDelay(SatiationProductionComponent component)
    {
        if (component.DelayMax is not { } maximum)
            return component.Delay;

        var seconds = _random.NextDouble(component.Delay.TotalSeconds, maximum.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
