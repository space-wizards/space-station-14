using Content.Shared._Offbrand.Organs;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.Wounds;

public sealed partial class WoundableBodySystem : OffbrandDamageSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private WoundableOrganSystem _woundableOrgan = default!;
    [Dependency] private WoundableSystem _woundable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundableBodyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<WoundableBodyComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<WoundableBodyComponent, RefreshWoundsEvent>(OnRefreshWounds);
    }

    private void OnShutdown(Entity<WoundableBodyComponent> ent, ref ComponentShutdown args)
    {
        if (!_statusEffects.TryEffectsWithComp<WoundComponent>(ent, out var wounds))
            return;

        foreach (var wound in wounds)
        {
            QueueDel(wound);
        }
    }

    public void HealWounds(Entity<WoundableBodyComponent> ent, DamageSpecifier incoming, bool passive, bool refresh)
    {
        var evt = new HealWoundsEvent(incoming, passive);
        RaiseLocalEvent(ent, ref evt);

        if (refresh)
            _woundable.RefreshWounds(ent, false, null);
    }

    private void OnDamageDealt(Entity<WoundableBodyComponent> ent, ref DamageDealtEvent args)
    {
        if (_timing.ApplyingState || !TryComp<DamageableComponent>(ent, out _))
            return;

        if (args.Damage.AnyPositive())
        {
            var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
            var rand = new System.Random(seed);

            var organs = _woundableOrgan.GetWoundableOrgans(ent);
            var target = SharedRandomExtensions.Pick(organs, rand);

            var organEvt = args with { Damage = DamageSpecifier.GetPositive(args.Damage) };
            RaiseLocalEvent(target, ref organEvt);
        }

        if (args.Damage.AnyNegative())
            HealWounds(ent, DamageSpecifier.GetNegative(args.Damage), false, false);

        _woundable.RefreshWounds(ent, args.InterruptsDoAfters, args.Origin);
    }

    private void OnRefreshWounds(Entity<WoundableBodyComponent> ent, ref RefreshWoundsEvent args)
    {
        var damageable = Comp<DamageableComponent>(ent);

        var evt = new WoundGetDamageEvent(new(), null);
        RaiseLocalEvent(ent, ref evt);

        var dict = damageable.Damage.DamageDict;

        var damageDone = new DamageSpecifier();
        foreach (var (type, newValue) in evt.Accumulator.DamageDict)
        {
            var oldValue = dict.GetValueOrDefault(type, FixedPoint2.Zero);

            damageDone.DamageDict[type] = newValue - oldValue;
        }

        damageable.Damage = evt.Accumulator;
        _damageable.OnEntityDamageChanged((ent, damageable), damageDone, args.InterruptsDoAfters, args.Origin);
    }

    public void ClampWounds(Entity<WoundableBodyComponent> ent, float probability)
    {
        var evt = new ClampWoundsEvent(probability);
        RaiseLocalEvent(ent, ref evt);
    }
}
