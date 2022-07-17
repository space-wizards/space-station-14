using Content.Shared.Damage;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Robust.Shared.Player;
using Content.Shared.Tag;
using Content.Server.Storage.Components;
using Content.Shared.Throwing;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Map;
using Content.Shared.Maps;
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Robust.Shared.Physics;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private void OnDefileAction(EntityUid uid, RevenantComponent component, RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!ChangeEssenceAmount(uid, component.DefileUseCost, component, false))
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, Filter.Entities(uid));
            return;
        }

        args.Handled = true;

        _statusEffects.TryAddStatusEffect<CorporealComponent>(uid, "Corporeal", TimeSpan.FromSeconds(component.DefileCorporealDuration), false);
        _stun.TryStun(uid, TimeSpan.FromSeconds(component.DefileStunDuration), false);

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
            {
                var ev = new GhostBooEvent();
                RaiseLocalEvent(ent, ev);
            }
        }
    }
}
