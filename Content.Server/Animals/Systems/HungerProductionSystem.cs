using Content.Server.Animals.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <inheritdoc cref="HungerProductionComponent"/>
public sealed partial class HungerProductionSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HungerProductionComponent>();
        while (query.MoveNext(out var uid, out var producer))
        {
            if (!producer.Automatic)
                continue;

            var owner = GetProducer((uid, producer));
            if (!producer.AutomaticForPlayers && HasComp<ActorComponent>(owner))
                continue;

            if (_timing.CurTime < producer.NextProductionTime)
                continue;

            producer.NextProductionTime += GetDelay(producer);
            TryProduce((uid, producer), out _);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<HungerProductionComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextProductionTime = _timing.CurTime + GetDelay(ent.Comp);
    }

    /// <summary>
    /// Attempts production immediately, independently of the automatic timer.
    /// </summary>
    public bool TryProduce(
        Entity<HungerProductionComponent?> ent,
        out HungerProductionFailure failure)
    {
        failure = HungerProductionFailure.ProductUnavailable;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var owner = GetProducer((ent.Owner, ent.Comp));
        if (_mobState.IsDead(owner))
        {
            failure = HungerProductionFailure.Dead;
            return false;
        }

        if (TryComp(owner, out SatiationComponent? satiation) &&
            satiation.Has(ent.Comp.SatiationType) &&
            !HasEnoughHunger(ent.Comp, (owner, satiation)))
        {
            failure = HungerProductionFailure.InsufficientSatiation;
            return false;
        }

        var ev = new ProductionAttemptEvent(owner);
        RaiseLocalEvent(ent.Owner, ref ev);
        if (!ev.Produced)
            return false;

        if (satiation != null)
            _satiation.ModifyValue((owner, satiation), ent.Comp.SatiationType, -ent.Comp.HungerUsage);

        failure = HungerProductionFailure.None;
        return true;
    }

    private bool HasEnoughHunger(
        HungerProductionComponent component,
        Entity<SatiationComponent> satiation)
    {
        if (component.MinimumHungerThreshold is { } threshold &&
            !_satiation.IsValueInRange(
                satiation,
                component.SatiationType,
                above: threshold,
                hypotheticalValueDelta: -component.HungerUsage))
        {
            return false;
        }

        return component.MinimumHunger is not { } minimum ||
               _satiation.GetValueOrNull(satiation, component.SatiationType) >= minimum;
    }

    private EntityUid GetProducer(Entity<HungerProductionComponent> ent)
    {
        return ent.Comp.Producer switch
        {
            HungerProductionOwner.Parent => Transform(ent).ParentUid,
            _ => ent.Owner
        };
    }

    private TimeSpan GetDelay(HungerProductionComponent component)
    {
        if (component.DelayMax is not { } maximum)
            return component.Delay;

        var seconds = _random.NextDouble(component.Delay.TotalSeconds, maximum.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
