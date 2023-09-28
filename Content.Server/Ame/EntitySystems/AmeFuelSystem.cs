using Content.Server.Ame.Components;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Throwing;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.Ame.EntitySystems;

/// <summary>
/// Adds fuel level info to examine on fuel jars and handles fuel leaking.
/// </summary>
public sealed class AmeFuelSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmeFuelContainerComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AmeFuelContainerComponent, ExplosiveComponent>();
        while (query.MoveNext(out var uid, out var comp, out var explosive))
        {
            if (!comp.Leaking || comp.FuelAmount < 1)
                continue;

            var now = _timing.CurTime;
            if (now < comp.NextLeak)
                continue;

            comp.NextLeak = now + comp.LeakDelay;

            // use up fuel
            comp.FuelAmount -= Math.Min(comp.FuelAmount, comp.LeakedFuel);

            // explode but make sure it can explode in the future
            _explosion.TriggerExplosive(uid, explosive);
            explosive.Exploded = false;

            // make it fly in a random direction
            var direction = _random.NextAngle().ToWorldVec();
            _throwing.TryThrow(uid, direction, 5f);
        }
    }

    private void OnExamined(EntityUid uid, AmeFuelContainerComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // less than 25%: amount < capacity / 4 = amount * 4 < capacity
        var low = comp.FuelAmount * 4 < comp.FuelCapacity;
        args.PushMarkup(Loc.GetString("ame-fuel-container-component-on-examine-detailed-message",
            ("colorName", low ? "darkorange" : "orange"),
            ("amount", comp.FuelAmount),
            ("capacity", comp.FuelCapacity)));
    }

    /// <summary>
    /// Makes the fuel jar start leaking antimatter.
    /// </summary>
    public void StartLeaking(EntityUid uid, AmeFuelContainerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp) || comp.Leaking)
            return;

        if (comp.FuelAmount < 1 || !HasComp<ExplosiveComponent>(uid))
            return;

        // don't instantly explode, give the user a little time to react
        comp.Leaking = true;
        comp.NextLeak = _timing.CurTime + comp.LeakDelay;
    }
}
