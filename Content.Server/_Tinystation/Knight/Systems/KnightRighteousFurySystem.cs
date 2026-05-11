using Content.Shared._Tinystation.Knight.Components;
using Content.Shared._Tinystation.Knight.Events;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server._Tinystation.Knight.Systems;

public sealed class KnightRighteousFurySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnightRighteousFuryComponent, KnightRighteousFuryEvent>(OnFury);
        SubscribeLocalEvent<KnightRighteousFuryComponent, DamageDealtEvent>(OnDamageDealt);
        SubscribeLocalEvent<KnightRighteousFuryComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnDamageDealt(Entity<KnightRighteousFuryComponent> ent, ref DamageDealtEvent args)
    {
        if (ent.Comp.FuryActive || ent.Comp.BerserkActive)
            return;

        if (!TryComp<DamageableComponent>(ent, out _))
            return;

        var totalDamage = _damageable.GetTotalDamage((ent, null));
        var maxDamage = 200f;

        if (totalDamage / maxDamage >= ent.Comp.BerserkThreshold)
        {
            ActivateBerserk(ent);
            return;
        }

        if (totalDamage / maxDamage >= ent.Comp.AutoTriggerThreshold)
        {
            ActivateFury(ent);
        }
    }

    private void OnRefreshSpeed(EntityUid uid, KnightRighteousFuryComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (comp.BerserkActive)
            args.ModifySpeed(comp.BerserkSpeedMultiplier);
        else if (comp.FuryActive)
            args.ModifySpeed(comp.FurySpeedMultiplier);
    }

    private void OnMeleeHit(EntityUid uid, MeleeWeaponComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!TryComp<KnightRighteousFuryComponent>(args.User, out var fury))
            return;

        if (!fury.FuryActive && !fury.BerserkActive)
            return;

        float multiplier = fury.BerserkActive ? fury.BerserkDamageMultiplier : fury.FuryDamageMultiplier;
        args.BonusDamage += args.BaseDamage * (multiplier - 1f);
    }

    private void OnFury(Entity<KnightRighteousFuryComponent> ent, ref KnightRighteousFuryEvent args)
    {
        if (ent.Comp.FuryActive || ent.Comp.BerserkActive)
            return;

        if (!TryComp<DamageableComponent>(ent, out _))
            return;

        var totalDamage = _damageable.GetTotalDamage((ent, null));
        var maxDamage = 200f;

        if (totalDamage / maxDamage >= ent.Comp.BerserkThreshold)
        {
            ActivateBerserk(ent);
        }
        else
        {
            ActivateFury(ent);
        }

        args.Handled = true;
    }

    private void ActivateFury(Entity<KnightRighteousFuryComponent> ent)
    {
        var (uid, comp) = ent;
        comp.FuryActive = true;
        comp.BerserkActive = false;
        comp.FuryEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.FuryDuration);
        Dirty(uid, comp);

        _speed.RefreshMovementSpeedModifiers(uid);
        _popup.PopupEntity(Loc.GetString("knight-righteous-fury-activate"), uid, uid, PopupType.Large);
    }

    private void ActivateBerserk(Entity<KnightRighteousFuryComponent> ent)
    {
        var (uid, comp) = ent;
        comp.FuryActive = false;
        comp.BerserkActive = true;
        comp.FuryEndTime = _timing.CurTime + TimeSpan.FromSeconds(comp.FuryDuration);
        Dirty(uid, comp);

        _speed.RefreshMovementSpeedModifiers(uid);

        _stun.TryUnstun(uid);
        _stun.ForceStandUp(uid);

        _popup.PopupEntity(Loc.GetString("knight-berserk-activate"), uid, uid, PopupType.LargeCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<KnightRighteousFuryComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.FuryActive && !comp.BerserkActive)
                continue;

            if (_timing.CurTime < comp.FuryEndTime)
                continue;

            var wasBerserk = comp.BerserkActive;
            comp.FuryActive = false;
            comp.BerserkActive = false;
            Dirty(uid, comp);
            _speed.RefreshMovementSpeedModifiers(uid);

            if (wasBerserk)
            {
                _popup.PopupEntity(Loc.GetString("knight-berserk-end"), uid, uid, PopupType.Small);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("knight-righteous-fury-end"), uid, uid, PopupType.Small);
            }
        }
    }
}
