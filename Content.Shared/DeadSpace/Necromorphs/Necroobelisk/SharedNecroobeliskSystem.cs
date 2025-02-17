// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.DeadSpace.Necromorphs.Sanity;
using Content.Shared.Mobs.Components;

namespace Content.Shared.DeadSpace.Necromorphs.Necroobelisk;

public abstract class SharedNecroobeliskSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSanitySystem _sharedSanity = default!;
    private bool _isSanityCheckExecuted = false;

    public override void Initialize()
    {
        SubscribeLocalEvent<NecroobeliskComponent, EntityUnpausedEvent>(OnNecroobeliskUnpause);
    }

    private void OnNecroobeliskUnpause(EntityUid uid, NecroobeliskComponent component, ref EntityUnpausedEvent args)
    {
        component.NextPulseTime += args.PausedTime;
        component.NextCheckTimeSanity += args.PausedTime;
        Dirty(uid, component);
    }

    private void SanityCheckOrConvergence(EntityUid uid, NecroobeliskComponent component)
    {
        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(uid, Transform(uid)), component.RangeSanity);

        foreach (var (entity, _) in entities)
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

            if (component.IsActive)
                _sharedSanity.TryAddSanityLvl(entity, -component.SanityDamage, sanityComponent);

            if (sanityComponent.SanityLevel <= 0)
            {
                var sanityLostEvent = new SanityLostEvent(entity);
                RaiseLocalEvent(uid, ref sanityLostEvent);
            }
        }

        if (component.MobsAbsorbed >= component.MobsForStageConvergence)
        {
            var necroMoonAppearanceEvent = new NecroMoonAppearanceEvent();
            RaiseLocalEvent(uid, ref necroMoonAppearanceEvent);
        }

        component.NextCheckTimeSanity = _gameTiming.CurTime + component.CheckDurationSanity;
    }

    private void NecroobeliskPulse(EntityUid uid, NecroobeliskComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!component.IsActive)
            return;

        _audio.PlayPvs(component.Sound, uid, AudioParams.Default.WithVariation(1f).WithVolume(15f));

        var ev = new NecroobeliskPulseEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var necroobeliskQuery = EntityQueryEnumerator<NecroobeliskComponent>();
        while (necroobeliskQuery.MoveNext(out var ent, out var necroobelisk))
        {
            if (_isSanityCheckExecuted)
            {
                necroobelisk.NextCheckTimeSanity = _gameTiming.CurTime + necroobelisk.CheckDurationSanity;
            }
            if (_gameTiming.CurTime > necroobelisk.NextPulseTime)
            {
                NecroobeliskPulse(ent, necroobelisk);
                necroobelisk.NextPulseTime = _gameTiming.CurTime + necroobelisk.TimeUtilPulse;
            }
            if (!_isSanityCheckExecuted && _gameTiming.CurTime > necroobelisk.NextCheckTimeSanity)
            {
                SanityCheckOrConvergence(ent, necroobelisk);
                _isSanityCheckExecuted = true;
            }

        }
        _isSanityCheckExecuted = false;
    }

    public virtual void UpdateState(EntityUid uid, NecroobeliskComponent component)
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
