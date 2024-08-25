using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.NPC.Queries;
using Content.Server.NPC.Queries.Considerations;
using Content.Server.NPC.Queries.Curves;
using Content.Server.NPC.Queries.Queries;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.NPC.Systems;

/// <summary>
/// Handles utility queries for NPCs.
/// </summary>
public sealed class NPCUtilitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DrinkSystem _drink = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly WeldableSystem _weldable = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MobThresholdSystem _thresholdSystem = default!;

    private EntityQuery<PuddleComponent> _puddleQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    private ObjectPool<HashSet<EntityUid>> _entPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new SetPolicy<EntityUid>(), 256);

    // Temporary caches.
    private List<EntityUid> _entityList = new();
    private HashSet<Entity<IComponent>> _entitySet = new();
    private List<EntityPrototype.ComponentRegistryEntry> _compTypes = new();

    public override void Initialize()
    {
        base.Initialize();
        _puddleQuery = GetEntityQuery<PuddleComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    /// Runs the UtilityQueryPrototype and returns the best-matching entities.
    /// </summary>
    /// <param name="bestOnly">Should we only return the entity with the best score.</param>
    public UtilityResult GetEntities(
        NPCBlackboard blackboard,
        string proto,
        bool bestOnly = true)
    {
        // TODO: PickHostilesop or whatever needs to juse be UtilityQueryOperator

        var weh = _proto.Index<UtilityQueryPrototype>(proto);
        var ents = _entPool.Get();

        foreach (var query in weh.Query)
        {
            switch (query)
            {
                case UtilityQueryFilter filter:
                    Filter(blackboard, ents, filter);
                    break;
                default:
                    Add(blackboard, ents, query);
                    break;
            }
        }

        if (ents.Count == 0)
        {
            _entPool.Return(ents);
            return UtilityResult.Empty;
        }

        var results = new Dictionary<EntityUid, float>();
        var highestScore = 0f;

        foreach (var ent in ents)
        {
            if (results.Count > weh.Limit)
                break;

            var score = 1f;

            foreach (var con in weh.Considerations)
            {
                var conScore = GetScore(blackboard, ent, con);
                var curve = con.Curve;
                var curveScore = GetScore(curve, conScore);

                var adjusted = GetAdjustedScore(curveScore, weh.Considerations.Count);
                score *= adjusted;

                // If the score is too low OR we only care about best entity then early out.
                // Due to the adjusted score only being able to decrease it can never exceed the highest from here.
                if (score <= 0f || bestOnly && score <= highestScore)
                {
                    break;
                }
            }

            if (score <= 0f)
                continue;

            highestScore = MathF.Max(score, highestScore);
            results.Add(ent, score);
        }

        var result = new UtilityResult(results);
        blackboard.Remove<EntityUid>(NPCBlackboard.UtilityTarget);
        _entPool.Return(ents);
        return result;
    }

    private float GetScore(IUtilityCurve curve, float conScore)
    {
        switch (curve)
        {
            case BoolCurve:
                return conScore > 0f ? 1f : 0f;
            case InverseBoolCurve:
                return conScore.Equals(0f) ? 1f : 0f;
            case PresetCurve presetCurve:
                return GetScore(_proto.Index<UtilityCurvePresetPrototype>(presetCurve.Preset).Curve, conScore);
            case QuadraticCurve quadraticCurve:
                return Math.Clamp(quadraticCurve.Slope * MathF.Pow(conScore - quadraticCurve.XOffset, quadraticCurve.Exponent) + quadraticCurve.YOffset, 0f, 1f);
            default:
                throw new NotImplementedException();
        }
    }

    private float GetScore(NPCBlackboard blackboard, EntityUid targetUid, UtilityConsideration consideration)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        switch (consideration)
        {
            case FoodValueCon:
            {
                if (!TryComp<FoodComponent>(targetUid, out var food))
                    return 0f;

                // mice can't eat unpeeled bananas, need monkey's help
                if (_openable.IsClosed(targetUid))
                    return 0f;

                if (!_food.IsDigestibleBy(owner, targetUid, food))
                    return 0f;

                var avoidBadFood = !HasComp<IgnoreBadFoodComponent>(owner);

                // only eat when hungry or if it will eat anything
                if (TryComp<HungerComponent>(owner, out var hunger) && hunger.CurrentThreshold > HungerThreshold.Okay && avoidBadFood)
                    return 0f;

                // no mouse don't eat the uranium-235
                if (avoidBadFood && HasComp<BadFoodComponent>(targetUid))
                    return 0f;

                return 1f;
            }
            case DrinkValueCon:
            {
                if (!TryComp<DrinkComponent>(targetUid, out var drink))
                    return 0f;

                // can't drink closed drinks
                if (_openable.IsClosed(targetUid))
                    return 0f;

                // only drink when thirsty
                if (TryComp<ThirstComponent>(owner, out var thirst) && thirst.CurrentThirstThreshold > ThirstThreshold.Okay)
                    return 0f;

                // no janicow don't drink the blood puddle
                if (HasComp<BadDrinkComponent>(targetUid))
                    return 0f;

                // needs to have something that will satiate thirst, mice wont try to drink 100% pure mutagen.
                var hydration = _drink.TotalHydration(targetUid, drink);
                if (hydration <= 1.0f)
                    return 0f;

                return 1f;
            }
            case OrderedTargetCon:
            {
                if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.CurrentOrderedTarget, out var orderedTarget, EntityManager))
                    return 0f;

                if (targetUid != orderedTarget)
                    return 0f;

                return 1f;
            }
            case TargetAccessibleCon:
            {
                if (_container.TryGetContainingContainer(targetUid, out var container))
                {
                    if (TryComp<EntityStorageComponent>(container.Owner, out var storageComponent))
                    {
                        if (storageComponent is { Open: false } && _weldable.IsWelded(container.Owner))
                        {
                            return 0.0f;
                        }
                    }
                    else
                    {
                        // If we're in a container (e.g. held or whatever) then we probably can't get it. Only exception
                        // Is a locker / crate
                        // TODO: Some mobs can break it so consider that.
                        return 0.0f;
                    }
                }

                // TODO: Pathfind there, though probably do it in a separate con.
                return 1f;
            }
            case TargetAmmoMatchesCon:
            {
                if (!blackboard.TryGetValue(NPCBlackboard.ActiveHand, out Hand? activeHand, EntityManager) ||
                    !TryComp<BallisticAmmoProviderComponent>(activeHand.HeldEntity, out var heldGun))
                {
                    return 0f;
                }

                if (_whitelistSystem.IsWhitelistFailOrNull(heldGun.Whitelist, targetUid))
                {
                    return 0f;
                }

                return 1f;
            }
            case TargetDistanceCon:
            {
                var radius = blackboard.GetValueOrDefault<float>(blackboard.GetVisionRadiusKey(EntityManager), EntityManager);

                if (!TryComp(targetUid, out TransformComponent? targetXform) ||
                    !TryComp(owner, out TransformComponent? xform))
                {
                    return 0f;
                }

                if (!targetXform.Coordinates.TryDistance(EntityManager, _transform, xform.Coordinates,
                        out var distance))
                {
                    return 0f;
                }

                return Math.Clamp(distance / radius, 0f, 1f);
            }
            case TargetAmmoCon:
            {
                if (!HasComp<GunComponent>(targetUid))
                    return 0f;

                var ev = new GetAmmoCountEvent();
                RaiseLocalEvent(targetUid, ref ev);

                if (ev.Count == 0)
                    return 0f;

                // Wat
                if (ev.Capacity == 0)
                    return 1f;

                return (float) ev.Count / ev.Capacity;
            }
            case TargetHealthCon con:
            {
                if (!TryComp(targetUid, out DamageableComponent? damage))
                    return 0f;
                if (con.TargetState != MobState.Invalid && _thresholdSystem.TryGetPercentageForState(targetUid, con.TargetState, damage.TotalDamage, out var percentage))
                    return Math.Clamp((float)(1 - percentage), 0f, 1f);
                if (_thresholdSystem.TryGetIncapPercentage(targetUid, damage.TotalDamage, out var incapPercentage))
                    return Math.Clamp((float)(1 - incapPercentage), 0f, 1f);
                return 0f;
            }
            case TargetInLOSCon:
            {
                var radius = blackboard.GetValueOrDefault<float>(blackboard.GetVisionRadiusKey(EntityManager), EntityManager);

                return _examine.InRangeUnOccluded(owner, targetUid, radius + 0.5f, null) ? 1f : 0f;
            }
            case TargetInLOSOrCurrentCon:
            {
                var radius = blackboard.GetValueOrDefault<float>(blackboard.GetVisionRadiusKey(EntityManager), EntityManager);
                const float bufferRange = 0.5f;

                if (blackboard.TryGetValue<EntityUid>("Target", out var currentTarget, EntityManager) &&
                    currentTarget == targetUid &&
                    TryComp(owner, out TransformComponent? xform) &&
                    TryComp(targetUid, out TransformComponent? targetXform) &&
                    xform.Coordinates.TryDistance(EntityManager, _transform, targetXform.Coordinates, out var distance) &&
                    distance <= radius + bufferRange)
                {
                    return 1f;
                }

                return _examine.InRangeUnOccluded(owner, targetUid, radius + bufferRange, null) ? 1f : 0f;
            }
            case TargetIsAliveCon:
            {
                return _mobState.IsAlive(targetUid) ? 1f : 0f;
            }
            case TargetIsCritCon:
            {
                return _mobState.IsCritical(targetUid) ? 1f : 0f;
            }
            case TargetIsDeadCon:
            {
                return _mobState.IsDead(targetUid) ? 1f : 0f;
            }
            case TargetMeleeCon:
            {
                if (TryComp<MeleeWeaponComponent>(targetUid, out var melee))
                {
                    return melee.Damage.GetTotal().Float() * melee.AttackRate / 100f;
                }

                return 0f;
            }
            default:
                throw new NotImplementedException();
        }
    }

    private float GetAdjustedScore(float score, int considerations)
    {
        /*
        * Now using the geometric mean
        * for n scores you take the n-th root of the scores multiplied
        * e.g. a, b, c scores you take Math.Pow(a * b * c, 1/3)
        * To get the ACTUAL geometric mean at any one stage you'd need to divide by the running consideration count
        * however, the downside to this is it will fluctuate up and down over time.
        * For our purposes if we go below the minimum threshold we want to cut it off, thus we take a
        * "running geometric mean" which can only ever go down (and by the final value will equal the actual geometric mean).
        */

        var adjusted = MathF.Pow(score, 1 / (float) considerations);
        return Math.Clamp(adjusted, 0f, 1f);
    }

    private void Add(NPCBlackboard blackboard, HashSet<EntityUid> entities, UtilityQuery query)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var vision = blackboard.GetValueOrDefault<float>(blackboard.GetVisionRadiusKey(EntityManager), EntityManager);

        switch (query)
        {
            case ComponentQuery compQuery:
            {
                if (compQuery.Components.Count == 0)
                    return;

                var mapPos = _transform.GetMapCoordinates(owner, xform: _xformQuery.GetComponent(owner));
                _compTypes.Clear();
                var i = -1;
                EntityPrototype.ComponentRegistryEntry compZero = default!;

                foreach (var compType in compQuery.Components.Values)
                {
                    i++;

                    if (i == 0)
                    {
                        compZero = compType;
                        continue;
                    }

                    _compTypes.Add(compType);
                }

                _entitySet.Clear();
                _lookup.GetEntitiesInRange(compZero.Component.GetType(), mapPos, vision, _entitySet);

                foreach (var comp in _entitySet)
                {
                    var ent = comp.Owner;

                    if (ent == owner)
                        continue;

                    var othersFound = true;

                    foreach (var compOther in _compTypes)
                    {
                        if (!HasComp(ent, compOther.Component.GetType()))
                        {
                            othersFound = false;
                            break;
                        }
                    }

                    if (!othersFound)
                        continue;

                    entities.Add(ent);
                }

                break;
            }
            case InventoryQuery:
            {
                if (!_inventory.TryGetContainerSlotEnumerator(owner, out var enumerator))
                    break;

                while (enumerator.MoveNext(out var slot))
                {
                    foreach (var child in slot.ContainedEntities)
                    {
                        RecursiveAdd(child, entities);
                    }
                }

                break;
            }
            case NearbyHostilesQuery:
            {
                foreach (var ent in _npcFaction.GetNearbyHostiles(owner, vision))
                {
                    entities.Add(ent);
                }
                break;
            }
            default:
                throw new NotImplementedException();
        }
    }

    private void RecursiveAdd(EntityUid uid, HashSet<EntityUid> entities)
    {
        // TODO: Probably need a recursive struct enumerator on engine.
        var xform = _xformQuery.GetComponent(uid);
        var enumerator = xform.ChildEnumerator;
        entities.Add(uid);

        while (enumerator.MoveNext(out var child))
        {
            RecursiveAdd(child, entities);
        }
    }

    private void Filter(NPCBlackboard blackboard, HashSet<EntityUid> entities, UtilityQueryFilter filter)
    {
        switch (filter)
        {
            case ComponentFilter compFilter:
            {
                _entityList.Clear();

                foreach (var ent in entities)
                {
                    foreach (var comp in compFilter.Components)
                    {
                        if (HasComp(ent, comp.Value.Component.GetType()))
                            continue;

                        _entityList.Add(ent);
                        break;
                    }
                }

                foreach (var ent in _entityList)
                {
                    entities.Remove(ent);
                }

                break;
            }
            case RemoveAnchoredFilter:
            {
                _entityList.Clear();

                foreach (var ent in entities)
                {
                    if (!TryComp(ent, out TransformComponent? xform))
                        continue;

                    if (xform.Anchored)
                        _entityList.Add(ent);
                }

                foreach (var ent in _entityList)
                {
                    entities.Remove(ent);
                }

                break;
            }
            case PuddleFilter:
            {
                _entityList.Clear();

                foreach (var ent in entities)
                {
                    if (!_puddleQuery.TryGetComponent(ent, out var puddleComp) ||
                        !_solutions.TryGetSolution(ent, puddleComp.SolutionName, out _, out var sol) ||
                        _puddle.CanFullyEvaporate(sol))
                    {
                        _entityList.Add(ent);
                    }
                }

                foreach (var ent in _entityList)
                {
                    entities.Remove(ent);
                }

                break;
            }
            default:
                throw new NotImplementedException();
        }
    }
}

public readonly record struct UtilityResult(Dictionary<EntityUid, float> Entities)
{
    public static readonly UtilityResult Empty = new(new Dictionary<EntityUid, float>());

    public readonly Dictionary<EntityUid, float> Entities = Entities;

    /// <summary>
    /// Returns the entity with the highest score.
    /// </summary>
    public EntityUid GetHighest()
    {
        if (Entities.Count == 0)
            return EntityUid.Invalid;

        return Entities.MaxBy(x => x.Value).Key;
    }

    /// <summary>
    /// Returns the entity with the lowest score. This does not consider entities with a 0 (invalid) score.
    /// </summary>
    public EntityUid GetLowest()
    {
        if (Entities.Count == 0)
            return EntityUid.Invalid;

        return Entities.MinBy(x => x.Value).Key;
    }
}
