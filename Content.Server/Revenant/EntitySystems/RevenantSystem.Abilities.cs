using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Server.DoAfter;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Robust.Shared.Map;
using Content.Shared.Tag;
using Content.Shared.Maps;
using Content.Server.Storage.Components;
using Content.Shared.Item;
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Robust.Shared.Physics;
using Content.Shared.Throwing;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Emag.Systems;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public void BeginSoulSearchDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
    {
        _popup.PopupEntity(Loc.GetString("revenant-soul-searching", ("target", target)), uid, Filter.Entities(uid), PopupType.Medium);
        var searchDoAfter = new DoAfterEventArgs(uid, revenant.SoulSearchDuration, target: target)
        {
            BreakOnUserMove = true,
            UserFinishedEvent = new SoulSearchDoAfterComplete(target),
        };
        _doAfter.DoAfter(searchDoAfter);
    }

    private void OnSoulSearchComplete(EntityUid uid, RevenantComponent component, SoulSearchDoAfterComplete args)
    {
        if (!TryComp<EssenceComponent>(args.Target, out var essence))
            return;
        essence.SearchComplete = true;

        string message;
        switch (essence.EssenceAmount)
        {
            case <= 30:
                message = "revenant-soul-yield-low";
                break;
            case >= 50:
                message = "revenant-soul-yield-high";
                break;
            default:
                message = "revenant-soul-yield-average";
                break;
        }
        _popup.PopupEntity(Loc.GetString(message, ("target", args.Target)), args.Target, Filter.Entities(uid), PopupType.Medium);
    }

    public void BeginHarvestDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
    {
        if (essence.Harvested)
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-harvested"), target, Filter.Entities(uid), PopupType.SmallCaution);
            return;
        }

        revenant.HarvestCancelToken = new();
        var doAfter = new DoAfterEventArgs(uid, revenant.HarvestDuration, revenant.HarvestCancelToken.Token, target)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = false,
            BreakOnUserMove = true,
            NeedHand = false,
            UserFinishedEvent = new HarvestDoAfterComplete(target),
            UserCancelledEvent = new HarvestDoAfterCancelled(),
        };

        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target, Filter.Pvs(target), PopupType.Large);

        CanUseAbility(uid, revenant, 0, revenant.HarvestDuration, revenant.HarvestDuration);
        _doAfter.DoAfter(doAfter);
    }

    private void OnHarvestComplete(EntityUid uid, RevenantComponent component, HarvestDoAfterComplete args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, false);

        if (!TryComp<EssenceComponent>(args.Target, out var essence))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", args.Target)),
            args.Target, Filter.Pvs(args.Target), PopupType.LargeCaution);

        essence.Harvested = true;
        ChangeEssenceAmount(uid, essence.EssenceAmount, component);

        if (_mobState.IsAlive(args.Target) && _random.Prob(component.PerfectSoulChance))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), uid, Filter.Entities(uid));
            component.MaxEssence = Math.Min(component.MaxEssence + component.MaxEssenceUpgradeAmount, component.EssenceCap);
        }

        if (TryComp<MobStateComponent>(args.Target, out var mobstate))
        {
            var damage = _mobState.GetEarliestDeadState(mobstate, 0)?.threshold;
            if (damage != null)
            {
                DamageSpecifier dspec = new();
                dspec.DamageDict.Add("Cellular", damage.Value);
                _damage.TryChangeDamage(args.Target, dspec, true);
            }
        }
    }

    private void OnHarvestCancelled(EntityUid uid, RevenantComponent component, HarvestDoAfterCancelled args)
    {
        if (TryComp<AppearanceComponent>(uid, out var app))
            app.SetData(RevenantVisuals.Harvesting, false);
    }

    private void OnDefileAction(EntityUid uid, RevenantComponent component, RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CanUseAbility(uid, component, component.DefileUseCost, component.DefileStunDuration, component.DefileCorporealDuration))
            return;

        args.Handled = true;

        var lookup = _lookup.GetEntitiesInRange(uid, component.DefileRadius, LookupFlags.Approximate | LookupFlags.Anchored);

        for (var i = 0; i < component.DefileTilePryAmount; i++)
        {
            //get random coordinates in the radius (technically a square but shut up)
            var coords = new EntityCoordinates(uid,
                (_random.NextFloat(-component.DefileRadius, component.DefileRadius), _random.NextFloat(-component.DefileRadius, component.DefileRadius)));

            var gridID = coords.GetGridUid(EntityManager);
            if (_mapManager.TryGetGrid(gridID, out var map))
                map.GetTileRef(coords).PryTile(_mapManager, entityManager: EntityManager, robustRandom: _random);
        }

        foreach (var ent in lookup)
        {
            //break windows
            if (HasComp<TagComponent>(ent))
            {
                if (_tag.HasAnyTag(ent, "Window"))
                {
                    //hardcoded damage specifiers til i die.
                    var dspec = new DamageSpecifier();
                    dspec.DamageDict.Add("Structural", 15);
                    _damage.TryChangeDamage(ent, dspec);
                }
            }

            //randomly opens some lockers and such.
            if (_random.Prob(component.DefileEffectChance) && HasComp<EntityStorageComponent>(ent))
                _entityStorage.TryOpenStorage(ent, ent, true); //the locker opening itself doesn't matter because of the specific logic.

            //chucks shit
            if (_random.Prob(component.DefileEffectChance) && HasComp<SharedItemComponent>(ent) &&
                TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

            //flicker lights
            if (_random.Prob(component.DefileEffectChance) && HasComp<PoweredLightComponent>(ent))
                RaiseLocalEvent(ent, new GhostBooEvent());
        }
    }

    private void OnOverloadLightsAction(EntityUid uid, RevenantComponent component, RevenantOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CanUseAbility(uid, component, component.OverloadLightsUseCost,
            component.OverloadLightsStunDuration, component.OverloadLightsCorporealDuration))
            return;

        args.Handled = true;

        var poweredLights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in _lookup.GetEntitiesInRange(uid, 5))
        {
            if (!poweredLights.HasComponent(ent))
                continue;

            var ev = new GhostBooEvent(); //light go flicker
            RaiseLocalEvent(ent, ev);

            if (_random.Prob(component.OverloadLightsBreakChance))
            {
                //smack dem lights
                var dspec = new DamageSpecifier();
                dspec.DamageDict.Add("Blunt", 15);
                _damage.TryChangeDamage(ent, dspec);

                if (_random.Prob(component.OverloadLightsProjectileChance))
                {
                    //sparks
                    for (var i = 0; i < _random.Next(1, 3); i++)
                    {
                        var proj = Spawn(component.OverloadLightsProjectileId, Transform(ent).Coordinates);
                        _throwing.TryThrow(proj, _random.NextVector2(), 15);
                    }
                }
            }
        }
    }

    private void OnMalfunctionAction(EntityUid uid, RevenantComponent component, RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!CanUseAbility(uid, component, component.MalfuncitonUseCost, component.MalfunctionStunDuration, component.MalfunctionCorporealDuration))
            return;

        args.Handled = true;

        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.MalfunctionRadius))
            if (_random.Prob(component.MalfunctionEffectChance))
                RaiseLocalEvent(ent, new GotEmaggedEvent(ent)); //it is going to emag itself to bypass popups and weird checks
    }
}
