// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.Mobs.Components;
using System.Linq;

namespace Content.Shared.DeadSpace.Necromorphs.Necroobelisk;

public abstract class SharedSuperNecroobeliskSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSanitySystem _sharedSanity = default!;



    public override void Initialize()
    {
        SubscribeLocalEvent<SuperMatterialNecroObeliskComponent, EntityUnpausedEvent>(OnNecroobeliskUnpause);
        SubscribeLocalEvent<SuperMatterialNecroObeliskComponent, ComponentShutdown>(OnNecroobeliskStop);
    }

    private void OnNecroobeliskUnpause(EntityUid uid, SuperMatterialNecroObeliskComponent component, ref EntityUnpausedEvent args)
    {
        component.NextPulseTime += args.PausedTime;
        component.NextCheckTimeSanity += args.PausedTime;
        Dirty(uid, component);
    }
    private void OnNecroobeliskStop(EntityUid uid, SuperMatterialNecroObeliskComponent component, ref ComponentShutdown args)
    {
        ClearTrackedOverlays(uid, component.MobsInRange);
    }

    private void SanityCheckOrConvergence(EntityUid uid, SuperMatterialNecroObeliskComponent component)
    {
        if (!component.IsActive)
        {
            ClearTrackedOverlays(uid, component.MobsInRange);
            component.NextCheckTimeSanity = _gameTiming.CurTime + component.CheckDurationSanity;
            return;
        }

        var entities = new HashSet<Entity<MobStateComponent>>();
        foreach (var entity in _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid, Transform(uid)), component.RangeSanity))
        {
            if (TryComp<MobStateComponent>(entity, out var mobState))
                entities.Add((entity, mobState));
        }

        foreach (var entity in component.MobsInRange.ToArray())
        {
            if (!entities.Contains(entity))
            {
                TryRemoveSanityOverlay(uid, entity);
                component.MobsInRange.Remove(entity);
            }
        }
        if (entities.Count > 8 && component.Percents > 20) component.Percents -= 10;
        if (component.Percents < 25)
        {
            ClearTrackedOverlays(uid, component.MobsInRange);
            component.NextCheckTimeSanity = _gameTiming.CurTime + component.CheckDurationSanity;
            return;
        }

        foreach (var (entity, comp) in entities)
        {
            if (component.IsStageConvergence)
            {
                var necroobeliskAbsorbEvent = new NecroobeliskAbsorbEvent(entity);
                RaiseLocalEvent(uid, ref necroobeliskAbsorbEvent);
            }

            if (HasComp<ImmunNecroobeliskComponent>(entity))
                continue;

            if (!TryComp<SanityComponent>(entity, out var sanityComponent))
                continue;

            _sharedSanity.TryAddSanityLvl(entity, -component.SanityDamage / entities.Count, sanityComponent);

            if (sanityComponent.SanityLevel <= 0)
            {
                TryRemoveSanityOverlay(uid, entity);
                component.MobsInRange.Remove((entity, comp));
                var sanityLostEvent = new SanityLostEvent(entity);
                RaiseLocalEvent(uid, ref sanityLostEvent);
                return;
            }
            EnsureComp<SanityOverlayComponent>(entity);
            component.MobsInRange.Add((entity, comp));
        }

        if (component.MobsAbsorbed >= component.MobsForStageConvergence)
        {
            var necroMoonAppearanceEvent = new NecroMoonAppearanceEvent();
            RaiseLocalEvent(uid, ref necroMoonAppearanceEvent);
        }

        component.NextCheckTimeSanity = _gameTiming.CurTime + component.CheckDurationSanity;
    }

    private void NecroobeliskPulse(EntityUid uid, SuperMatterialNecroObeliskComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!component.IsActive)
            return;

        var ev = new NecroobeliskPulseEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var necroobeliskQuery = EntityQueryEnumerator<SuperMatterialNecroObeliskComponent>();
        while (necroobeliskQuery.MoveNext(out var ent, out var necroobelisk))
        {
            if (_gameTiming.CurTime > necroobelisk.NextPulseTime)
            {
                NecroobeliskPulse(ent, necroobelisk);
                necroobelisk.NextPulseTime = _gameTiming.CurTime + necroobelisk.TimeUtilPulse;
            }

            if (_gameTiming.CurTime > necroobelisk.NextCheckTimeSanity)
                SanityCheckOrConvergence(ent, necroobelisk);

        }
    }

    private void ClearTrackedOverlays(EntityUid source, HashSet<Entity<MobStateComponent>> trackedMobs)
    {
        foreach (var entity in trackedMobs.ToArray())
            TryRemoveSanityOverlay(source, entity);

        trackedMobs.Clear();
    }

    private void TryRemoveSanityOverlay(EntityUid source, EntityUid entity)
    {
        if (IsInOtherActiveObeliskRange(source, entity))
            return;

        RemComp<SanityOverlayComponent>(entity);
    }

    private bool IsInOtherActiveObeliskRange(EntityUid source, EntityUid entity)
    {
        if (!TryComp(entity, out TransformComponent? entityXform))
            return false;

        var coords = _transform.GetMapCoordinates(entity, entityXform);

        var necroobeliskQuery = EntityQueryEnumerator<NecroobeliskComponent, TransformComponent>();
        while (necroobeliskQuery.MoveNext(out var obelisk, out var component, out var xform))
        {
            if (obelisk == source || !component.IsActive)
                continue;

            var obeliskCoords = _transform.GetMapCoordinates(obelisk, xform);
            if (obeliskCoords.MapId != coords.MapId)
                continue;

            if ((obeliskCoords.Position - coords.Position).LengthSquared() <= component.RangeSanity * component.RangeSanity)
                return true;
        }

        var superObeliskQuery = EntityQueryEnumerator<SuperMatterialNecroObeliskComponent, TransformComponent>();
        while (superObeliskQuery.MoveNext(out var obelisk, out var component, out var xform))
        {
            if (obelisk == source || !component.IsActive)
                continue;

            var obeliskCoords = _transform.GetMapCoordinates(obelisk, xform);
            if (obeliskCoords.MapId != coords.MapId)
                continue;

            if ((obeliskCoords.Position - coords.Position).LengthSquared() <= component.RangeSanity * component.RangeSanity)
                return true;
        }

        return false;
    }

    public virtual void UpdateState(EntityUid uid, SuperMatterialNecroObeliskComponent component)
    {
        if (component.IsActive)
        {
            _appearance.SetData(uid, NecroobeliskVisuals.Unactive, false);
            _appearance.SetData(uid, NecroobeliskVisuals.Active, true);
        }
        if (!component.IsActive)
        {
            _appearance.SetData(uid, NecroobeliskVisuals.Unactive, true);
            _appearance.SetData(uid, NecroobeliskVisuals.Active, false);
        }
    }
}
