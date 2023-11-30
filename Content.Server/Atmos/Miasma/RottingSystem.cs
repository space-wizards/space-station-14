using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos.Miasma;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Miasma;

public sealed class RottingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PerishableComponent, MapInitEvent>(OnPerishableMapInit);
        SubscribeLocalEvent<PerishableComponent, EntityUnpausedEvent>(OnPerishableUnpaused);
        SubscribeLocalEvent<PerishableComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<RottingComponent, EntityUnpausedEvent>(OnRottingUnpaused);
        SubscribeLocalEvent<RottingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RottingComponent, MobStateChangedEvent>(OnRottingMobStateChanged);
        SubscribeLocalEvent<RottingComponent, BeingGibbedEvent>(OnGibbed);
        SubscribeLocalEvent<RottingComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<RottingComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<TemperatureComponent, IsRottingEvent>(OnTempIsRotting);
    }

    private void OnPerishableMapInit(EntityUid uid, PerishableComponent component, MapInitEvent args)
    {
        component.NextPerishUpdate = _timing.CurTime + component.PerishUpdateRate;
    }

    private void OnPerishableUnpaused(EntityUid uid, PerishableComponent component, ref EntityUnpausedEvent args)
    {
        component.NextPerishUpdate += args.PausedTime;
    }

    private void OnMobStateChanged(EntityUid uid, PerishableComponent component, MobStateChangedEvent args)
    {
        if (!_mobState.IsDead(uid))
            return;

        component.RotAccumulator = TimeSpan.Zero;
        component.NextPerishUpdate = _timing.CurTime + component.PerishUpdateRate;
    }

    private void OnRottingUnpaused(EntityUid uid, RottingComponent component, ref EntityUnpausedEvent args)
    {
        component.NextRotUpdate += args.PausedTime;
    }

    private void OnShutdown(EntityUid uid, RottingComponent component, ComponentShutdown args)
    {
        if (TryComp<PerishableComponent>(uid, out var perishable))
        {
            perishable.NextPerishUpdate = TimeSpan.Zero;
        }
    }

    private void OnRottingMobStateChanged(EntityUid uid, RottingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            return;
        RemCompDeferred(uid, component);
    }

    public bool IsRotProgressing(EntityUid uid, PerishableComponent? perishable)
    {
        // things don't perish by default.
        if (!Resolve(uid, ref perishable, false))
            return false;

        // only dead things or inanimate objects can rot
        if (TryComp<MobStateComponent>(uid, out var mobState) && !_mobState.IsDead(uid, mobState))
            return false;

        if (_container.TryGetOuterContainer(uid, Transform(uid), out var container) &&
            HasComp<AntiRottingContainerComponent>(container.Owner))
        {
            return false;
        }

        var ev = new IsRottingEvent();
        RaiseLocalEvent(uid, ref ev);

        return ev.Handled;
    }

    public bool IsRotten(EntityUid uid, RottingComponent? rotting = null)
    {
        return Resolve(uid, ref rotting, false);
    }

    private void OnGibbed(EntityUid uid, RottingComponent component, BeingGibbedEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physics))
            return;

        if (!TryComp<PerishableComponent>(uid, out var perishable))
            return;

        var molsToDump = perishable.MolsPerSecondPerUnitMass * physics.FixturesMass * (float) component.TotalRotTime.TotalSeconds;
        var tileMix = _atmosphere.GetTileMixture(uid, excite: true);
        tileMix?.AdjustMoles(Gas.Miasma, molsToDump);
    }

    private void OnExamined(EntityUid uid, RottingComponent component, ExaminedEvent args)
    {
        var stage = RotStage(uid, component);
        var description = stage switch
        {
            >= 2 => "miasma-extremely-bloated",
            >= 1 => "miasma-bloated",
               _ => "miasma-rotting"
        };
        args.PushMarkup(Loc.GetString(description));
    }

    /// <summary>
    /// Return the rot stage, usually from 0 to 2 inclusive.
    /// </summary>
    public int RotStage(EntityUid uid, RottingComponent? comp = null, PerishableComponent? perishable = null)
    {
        if (!Resolve(uid, ref comp, ref perishable))
            return 0;

        return (int) (comp.TotalRotTime.TotalSeconds / perishable.RotAfter.TotalSeconds);
    }

    private void OnRejuvenate(EntityUid uid, RottingComponent component, RejuvenateEvent args)
    {
        RemCompDeferred<RottingComponent>(uid);
    }

    private void OnTempIsRotting(EntityUid uid, TemperatureComponent component, ref IsRottingEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = component.CurrentTemperature > Atmospherics.T0C + 0.85f;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var perishQuery = EntityQueryEnumerator<PerishableComponent>();
        while (perishQuery.MoveNext(out var uid, out var perishable))
        {
            if (_timing.CurTime < perishable.NextPerishUpdate)
                continue;
            perishable.NextPerishUpdate += perishable.PerishUpdateRate;

            if (IsRotten(uid) || !IsRotProgressing(uid, perishable))
                continue;

            perishable.RotAccumulator += perishable.PerishUpdateRate;
            if (perishable.RotAccumulator >= perishable.RotAfter)
            {
                var rot = AddComp<RottingComponent>(uid);
                rot.NextRotUpdate = _timing.CurTime + rot.RotUpdateRate;
            }
        }

        var rotQuery = EntityQueryEnumerator<RottingComponent, PerishableComponent, TransformComponent>();
        while (rotQuery.MoveNext(out var uid, out var rotting, out var perishable, out var xform))
        {
            if (_timing.CurTime < rotting.NextRotUpdate) // This is where it starts to get noticable on larger animals, no need to run every second
                continue;
            rotting.NextRotUpdate += rotting.RotUpdateRate;

            if (!IsRotProgressing(uid, perishable))
                continue;
            rotting.TotalRotTime += rotting.RotUpdateRate;

            if (rotting.DealDamage)
            {
                var damage = rotting.Damage * rotting.RotUpdateRate.TotalSeconds;
                _damageable.TryChangeDamage(uid, damage, true, false);
            }

            if (TryComp<RotIntoComponent>(uid, out var rotInto))
            {
                var stage = RotStage(uid, rotting, perishable);
                if (stage >= rotInto.Stage)
                {
                    Spawn(rotInto.Entity, xform.Coordinates);
                    QueueDel(uid);
                    continue;
                }
            }

            if (!TryComp<PhysicsComponent>(uid, out var physics))
                continue;
            // We need a way to get the mass of the mob alone without armor etc in the future
            // or just remove the mass mechanics altogether because they aren't good.
            var molRate = perishable.MolsPerSecondPerUnitMass * (float) rotting.RotUpdateRate.TotalSeconds;
            var tileMix = _atmosphere.GetTileMixture(uid, excite: true);
            tileMix?.AdjustMoles(Gas.Miasma, molRate * physics.FixturesMass);
        }
    }
}
