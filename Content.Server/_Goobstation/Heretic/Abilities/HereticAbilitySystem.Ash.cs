using Content.Server.Atmos.Components;
using Content.Shared.Heretic;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Server.Polymorph.Systems;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    private void SubscribeAsh()
    {
        SubscribeLocalEvent<HereticComponent, EventHereticAshenShift>(OnJaunt);
        SubscribeLocalEvent<GhoulComponent, EventHereticAshenShift>(OnJauntGhoul);
        SubscribeLocalEvent<HereticComponent, PolymorphRevertEvent>(OnJauntEnd);

        SubscribeLocalEvent<HereticComponent, EventHereticVolcanoBlast>(OnVolcano);
        SubscribeLocalEvent<HereticComponent, EventHereticNightwatcherRebirth>(OnNWRebirth);
        SubscribeLocalEvent<HereticComponent, EventHereticFlames>(OnFlames);
        SubscribeLocalEvent<HereticComponent, EventHereticCascade>(OnCascade);
    }

    private void OnJaunt(Entity<HereticComponent> ent, ref EventHereticAshenShift args)
    {
        if (TryUseAbility(ent, args) && TryDoJaunt(ent))
            args.Handled = true;
    }
    private void OnJauntGhoul(Entity<GhoulComponent> ent, ref EventHereticAshenShift args)
    {
        if (TryUseAbility(ent, args) && TryDoJaunt(ent))
            args.Handled = true;
    }
    private bool TryDoJaunt(EntityUid ent)
    {
        Spawn("PolymorphAshJauntAnimation", Transform(ent).Coordinates);
        var urist = _poly.PolymorphEntity(ent, "AshJaunt");
        if (urist == null)
            return false;
        return true;
    }
    private void OnJauntEnd(Entity<HereticComponent> ent, ref PolymorphRevertEvent args)
    {
        Spawn("PolymorphAshJauntEndAnimation", Transform(ent).Coordinates);
    }

    private void OnVolcano(Entity<HereticComponent> ent, ref EventHereticVolcanoBlast args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var ignoredTargets = new List<EntityUid>();

        // all ghouls are immune to heretic shittery
        foreach (var e in EntityQuery<GhoulComponent>())
            ignoredTargets.Add(e.Owner);

        // all heretics with the same path are also immune
        foreach (var e in EntityQuery<HereticComponent>())
            if (e.CurrentPath == ent.Comp.CurrentPath)
                ignoredTargets.Add(e.Owner);

        if (!_splitball.Spawn(ent, ignoredTargets))
            return;

        if (ent.Comp.Ascended) // will only work on ash path
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
    private void OnNWRebirth(Entity<HereticComponent> ent, ref EventHereticNightwatcherRebirth args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var lookup = _lookup.GetEntitiesInRange(ent, 5f);

        foreach (var look in lookup)
        {
            if ((TryComp<HereticComponent>(look, out var th) && th.CurrentPath == ent.Comp.CurrentPath)
            || HasComp<GhoulComponent>(look))
                continue;

            if (TryComp<FlammableComponent>(look, out var flam))
            {
                if (flam.OnFire && TryComp<DamageableComponent>(ent, out var dmgc))
                {
                    // heals everything by 10 for each burning target
                    _stam.TryTakeStamina(ent, -10);
                    var dmgdict = dmgc.Damage.DamageDict;
                    foreach (var key in dmgdict.Keys)
                        dmgdict[key] = -10f;

                    var dmgspec = new DamageSpecifier() { DamageDict = dmgdict };
                    _dmg.TryChangeDamage(ent, dmgspec, true, false, dmgc);
                }

                if (!flam.OnFire)
                    _flammable.AdjustFireStacks(look, 5, flam, true);

                if (TryComp<MobStateComponent>(look, out var mobstat))
                    if (mobstat.CurrentState == MobState.Critical)
                        _mobstate.ChangeMobState(look, MobState.Dead, mobstat);
            }
        }

        args.Handled = true;
    }
    private void OnFlames(Entity<HereticComponent> ent, ref EventHereticFlames args)
    {
        if (!TryUseAbility(ent, args))
            return;

        EnsureComp<HereticFlamesComponent>(ent);

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
    private void OnCascade(Entity<HereticComponent> ent, ref EventHereticCascade args)
    {
        if (!TryUseAbility(ent, args) || !Transform(ent).GridUid.HasValue)
            return;

        // yeah. it just generates a ton of plasma which just burns.
        // lame, but we don't have anything fire related atm, so, it works.
        var tilepos = _xform.GetGridOrMapTilePosition(ent, Transform(ent));
        var enumerator = _atmos.GetAdjacentTileMixtures(Transform(ent).GridUid!.Value, tilepos, false, false);
        while (enumerator.MoveNext(out var mix))
        {
            mix.AdjustMoles(Gas.Plasma, 50f);
            mix.Temperature = Atmospherics.T0C + 125f;
        }

        if (ent.Comp.Ascended)
            _flammable.AdjustFireStacks(ent, 20f, ignite: true);

        args.Handled = true;
    }
}
