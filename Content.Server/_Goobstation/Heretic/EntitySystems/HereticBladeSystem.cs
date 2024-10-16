using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Heretic.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Temperature.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticBladeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly HereticCombatMarkSystem _combatMark = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly TemperatureSystem _temp = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private HashSet<Entity<MapGridComponent>> _targetGrids = [];

    public void ApplySpecialEffect(EntityUid performer, EntityUid target)
    {
        if (!TryComp<HereticComponent>(performer, out var hereticComp))
            return;

        switch (hereticComp.CurrentPath)
        {
            case "Ash":
                _flammable.AdjustFireStacks(target, 2.5f, ignite: true);
                break;

            case "Blade":
                // todo: double it's damage
                break;

            case "Flesh":
                // ultra bleed
                _blood.TryModifyBleedAmount(target, 1.5f);
                break;

            case "Lock":
                // todo: do something that has weeping and avulsion in it
                if (_random.Next(0, 10) >= 8)
                    _blood.TryModifyBleedAmount(target, 10f);
                break;

            case "Rust":
                var dmgProt = _proto.Index((ProtoId<DamageGroupPrototype>) "Poison");
                var dmgSpec = new DamageSpecifier(dmgProt, 7.5f);
                _damage.TryChangeDamage(target, dmgSpec);
                break;

            case "Void":
                if (TryComp<TemperatureComponent>(target, out var temp))
                    _temp.ForceChangeTemperature(target, temp.CurrentTemperature - 5f, temp);
                break;

            default:
                return;
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<HereticBladeComponent, UseInHandEvent>(OnInteract);
        SubscribeLocalEvent<HereticBladeComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<HereticBladeComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnInteract(Entity<HereticBladeComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<HereticComponent>(args.User, out var heretic))
            return;

        var xform = Transform(args.User);
        // 250 because for some reason it counts "10" as 1 tile
        var targetCoords = SelectRandomTileInRange(xform, 250f);
        var queuedel = true;

        // void path exxclusive
        if (heretic.CurrentPath == "Void" && heretic.PathStage >= 7)
        {
            var look = _lookupSystem.GetEntitiesInRange<HereticCombatMarkComponent>(Transform(ent).Coordinates, 15f);
            if (look.Count > 0)
            {
                targetCoords = Transform(look.ToList()[0]).Coordinates;
                queuedel = false;
            }
        }

        if (targetCoords != null)
        {
            _xform.SetCoordinates(args.User, targetCoords.Value);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/tesla_consume.ogg"), args.User);
            args.Handled = true;
        }

        _popup.PopupEntity(Loc.GetString("heretic-blade-use"), args.User, args.User);

        if (queuedel)
            QueueDel(ent);
    }

    private void OnExamine(Entity<HereticBladeComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<HereticComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("heretic-blade-examine"));
    }

    private void OnMeleeHit(Entity<HereticBladeComponent> ent, ref MeleeHitEvent args)
    {
        if (string.IsNullOrWhiteSpace(ent.Comp.Path))
            return;

        if (!TryComp<HereticComponent>(args.User, out var hereticComp))
            return;

        foreach (var hit in args.HitEntities)
        {
            // does not work on other heretics
            if (HasComp<HereticComponent>(hit))
                continue;

            if (TryComp<HereticCombatMarkComponent>(hit, out var mark))
            {
                _combatMark.ApplyMarkEffect(hit, ent.Comp.Path);
                RemComp(hit, mark);
            }

            if (hereticComp.PathStage >= 7)
                ApplySpecialEffect(args.User, hit);
        }
    }

    private EntityCoordinates? SelectRandomTileInRange(TransformComponent userXform, float radius)
    {
        var userCoords = userXform.Coordinates.ToMap(EntityManager, _xform);
        _targetGrids.Clear();
        _lookupSystem.GetEntitiesInRange(userCoords, radius, _targetGrids);
        Entity<MapGridComponent>? targetGrid = null;

        if (_targetGrids.Count == 0)
            return null;

        // Give preference to the grid the entity is currently on.
        // This does not guarantee that if the probability fails that the owner's grid won't be picked.
        // In reality the probability is higher and depends on the number of grids.
        if (userXform.GridUid != null && TryComp<MapGridComponent>(userXform.GridUid, out var gridComp))
        {
            var userGrid = new Entity<MapGridComponent>(userXform.GridUid.Value, gridComp);
            if (_random.Prob(0.5f))
            {
                _targetGrids.Remove(userGrid);
                targetGrid = userGrid;
            }
        }

        if (targetGrid == null)
            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);

        EntityCoordinates? targetCoords = null;

        do
        {
            var valid = false;

            var range = (float) Math.Sqrt(radius);
            var box = Box2.CenteredAround(userCoords.Position, new Vector2(range, range));
            var tilesInRange = _mapSystem.GetTilesEnumerator(targetGrid.Value.Owner, targetGrid.Value.Comp, box, false);
            var tileList = new ValueList<Vector2i>();

            while (tilesInRange.MoveNext(out var tile))
            {
                tileList.Add(tile.GridIndices);
            }

            while (tileList.Count != 0)
            {
                var tile = tileList.RemoveSwap(_random.Next(tileList.Count));
                valid = true;
                foreach (var entity in _mapSystem.GetAnchoredEntities(targetGrid.Value.Owner, targetGrid.Value.Comp,
                             tile))
                {
                    if (!_physicsQuery.TryGetComponent(entity, out var body))
                        continue;

                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.MobMask) == 0)
                        continue;

                    valid = false;
                    break;
                }

                if (valid)
                {
                    targetCoords = new EntityCoordinates(targetGrid.Value.Owner,
                        _mapSystem.TileCenterToVector(targetGrid.Value, tile));
                    break;
                }
            }

            if (valid || _targetGrids.Count == 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                break;

            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);
        } while (true);

        return targetCoords;
    }

}
