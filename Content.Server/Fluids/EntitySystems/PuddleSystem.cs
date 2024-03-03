using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Spreader;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Friction;
using Content.Shared.IdentityManagement;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Server.Audio;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles solutions on floors. Also handles the spreader logic for where the solution overflows a specified volume.
/// </summary>
public sealed partial class PuddleSystem : SharedPuddleSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _color = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
    [Dependency] private readonly SlowContactsSystem _slowContacts = default!;
    [Dependency] private readonly TileFrictionController _tile = default!;

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Blood = "Blood";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Slime = "Slime";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string CopperBlood = "CopperBlood";

    private static string[] _standoutReagents = [Blood, Slime, CopperBlood];

    public static readonly float PuddleVolume = 1000;

    // Using local deletion queue instead of the standard queue so that we can easily "undelete" if a puddle
    // loses & then gains reagents in a single tick.
    private HashSet<EntityUid> _deletionQueue = [];

    private EntityQuery<PuddleComponent> _puddleQuery;

    /*
     * TODO: Need some sort of way to do blood slash / vomit solution spill on its own
     * This would then evaporate into the puddle tile below
     */

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _puddleQuery = GetEntityQuery<PuddleComponent>();

        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<PuddleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<PuddleComponent, SolutionContainerChangedEvent>(OnSolutionUpdate);
        SubscribeLocalEvent<PuddleComponent, ComponentInit>(OnPuddleInit);
        SubscribeLocalEvent<PuddleComponent, SpreadNeighborsEvent>(OnPuddleSpread);
        SubscribeLocalEvent<PuddleComponent, SlipEvent>(OnPuddleSlip);

        SubscribeLocalEvent<EvaporationComponent, MapInitEvent>(OnEvaporationMapInit);

        InitializeTransfers();
    }

    private void OnPuddleSpread(Entity<PuddleComponent> entity, ref SpreadNeighborsEvent args)
    {
        // Overflow is the source of the overflowing liquid. This contains the excess fluid above overflow limit (20u)
        var overflow = GetOverflowSolution(entity.Owner, entity.Comp);

        if (overflow.Volume == FixedPoint2.Zero)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
            return;
        }

        // For overflows, we never go to a fully evaporative tile just to avoid continuously having to mop it.

        // First we go to free tiles.
        // Need to go even if we have a little remainder to avoid solution sploshing around internally
        // for ages.
        if (args.NeighborFreeTiles.Count > 0 && args.Updates > 0)
        {
            _random.Shuffle(args.NeighborFreeTiles);
            var spillAmount = overflow.Volume / args.NeighborFreeTiles.Count;

            foreach (var neighbor in args.NeighborFreeTiles)
            {
                var split = overflow.SplitSolution(spillAmount);
                TrySpillAt(neighbor.Grid.GridTileToLocal(neighbor.Tile), split, out _, false);
                args.Updates--;

                if (args.Updates <= 0)
                    break;
            }

            RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
            return;
        }

        // Then we overflow to neighbors with overflow capacity
        if (args.Neighbors.Count > 0)
        {
            var resolvedNeighbourSolutions = new ValueList<(Solution neighborSolution, PuddleComponent puddle, EntityUid neighbor)>();

            // Resolve all our neighbours first, so we can use their properties to decide who to operate on first.
            foreach (var neighbor in args.Neighbors)
            {
                if (!_puddleQuery.TryGetComponent(neighbor, out var puddle) ||
                    !_solutionContainerSystem.ResolveSolution(neighbor, puddle.SolutionName, ref puddle.Solution,
                        out var neighborSolution) ||
                    CanFullyEvaporate(neighborSolution))
                {
                    continue;
                }

                resolvedNeighbourSolutions.Add(
                    (neighborSolution, puddle, neighbor)
                );
            }

            // We want to deal with our neighbours by lowest current volume to highest, as this allows us to fill up our low points quickly.
            resolvedNeighbourSolutions.Sort(
                (x, y) =>
                    x.neighborSolution.Volume.CompareTo(y.neighborSolution.Volume));

            // Overflow to neighbors with remaining space.
            foreach (var (neighborSolution, puddle, neighbor) in resolvedNeighbourSolutions)
            {
                // Water doesn't flow uphill
                if (neighborSolution.Volume >= (overflow.Volume + puddle.OverflowVolume))
                {
                    continue;
                }

                // Work out how much we could send into this neighbour without overflowing it, and send up to that much
                var remaining = puddle.OverflowVolume - neighborSolution.Volume;

                // If we can't send anything, then skip this neighbour
                if (remaining <= FixedPoint2.Zero)
                    continue;

                // We don't want to spill over to make high points either.
                if (neighborSolution.Volume + remaining >= (overflow.Volume + puddle.OverflowVolume))
                {
                    continue;
                }

                var split = overflow.SplitSolution(remaining);

                if (puddle.Solution != null && !_solutionContainerSystem.TryAddSolution(puddle.Solution.Value, split))
                    continue;

                args.Updates--;
                EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);

                if (args.Updates <= 0)
                    break;
            }

            // If there is nothing left to overflow from our tile, then we'll stop this tile being a active spreader
            if (overflow.Volume == FixedPoint2.Zero)
            {
                RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
                return;
            }
        }

        // Then we go to anything else.
        if (overflow.Volume > FixedPoint2.Zero && args.Neighbors.Count > 0 && args.Updates > 0)
        {
            var resolvedNeighbourSolutions =
                new ValueList<(Solution neighborSolution, PuddleComponent puddle, EntityUid neighbor)>();

            // Keep track of the total volume in the area
            FixedPoint2 totalVolume = 0;

            // Resolve all our neighbours so that we can use their properties to decide who to act on first
            foreach (var neighbor in args.Neighbors)
            {
                if (!_puddleQuery.TryGetComponent(neighbor, out var puddle) ||
                    !_solutionContainerSystem.ResolveSolution(neighbor, puddle.SolutionName, ref puddle.Solution,
                        out var neighborSolution) ||
                    CanFullyEvaporate(neighborSolution))
                {
                    continue;
                }

                resolvedNeighbourSolutions.Add((neighborSolution, puddle, neighbor));
                totalVolume += neighborSolution.Volume;
            }

            // We should act on neighbours by their total volume.
            resolvedNeighbourSolutions.Sort(
                (x, y) =>
                    x.neighborSolution.Volume.CompareTo(y.neighborSolution.Volume)
            );

            // Overflow to neighbors with remaining total allowed space (1000u) above the overflow volume (20u).
            foreach (var (neighborSolution, puddle, neighbor) in resolvedNeighbourSolutions)
            {
                // What the source tiles current volume is.
                var sourceCurrentVolume = overflow.Volume + puddle.OverflowVolume;

                // Water doesn't flow uphill
                if (neighborSolution.Volume >= sourceCurrentVolume)
                {
                    continue;
                }

                // We're in the low point in this area, let the neighbour tiles have a chance to spread to us first.
                var idealAverageVolume =
                    (totalVolume + overflow.Volume + puddle.OverflowVolume) / (args.Neighbors.Count + 1);

                if (idealAverageVolume > sourceCurrentVolume)
                {
                    continue;
                }

                // Work our how far off the ideal average this neighbour is.
                var spillThisNeighbor = idealAverageVolume - neighborSolution.Volume;

                // Skip if we want to spill negative amounts of fluid to this neighbour
                if (spillThisNeighbor < FixedPoint2.Zero)
                {
                    continue;
                }

                // Try to send them as much towards the average ideal as we can
                var split = overflow.SplitSolution(spillThisNeighbor);

                // If we can't do it, move on.
                if (puddle.Solution != null && !_solutionContainerSystem.TryAddSolution(puddle.Solution.Value, split))
                    continue;

                // If we succeed, then ensure that this neighbour is also able to spread it's overflow onwards
                EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);
                args.Updates--;

                if (args.Updates <= 0)
                    break;
            }
        }

        // Add the remainder back
        if (_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution))
        {
            _solutionContainerSystem.TryAddSolution(entity.Comp.Solution.Value, overflow);
        }
    }

    private void OnPuddleSlip(Entity<PuddleComponent> entity, ref SlipEvent args)
    {
        // Reactive entities have a chance to get a touch reaction from slipping on a puddle
        // (i.e. it is implied they fell face first onto it or something)
        if (!HasComp<ReactiveComponent>(args.Slipped) || HasComp<SlidingComponent>(args.Slipped))
            return;

        // Eventually probably have some system of 'body coverage' to tweak the probability but for now just 0.5
        // (implying that spacemen have a 50% chance to either land on their ass or their face)
        if (!_random.Prob(0.5f))
            return;

        if (!_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution,
                out var solution))
            return;

        _popups.PopupEntity(Loc.GetString("puddle-component-slipped-touch-reaction", ("puddle", entity.Owner)),
            args.Slipped, args.Slipped, PopupType.SmallCaution);

        // Take 15% of the puddle solution
        var splitSol = _solutionContainerSystem.SplitSolution(entity.Comp.Solution.Value, solution.Volume * 0.15f);
        _reactive.DoEntityReaction(args.Slipped, splitSol, ReactionMethod.Touch);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var ent in _deletionQueue)
        {
            Del(ent);
        }

        _deletionQueue.Clear();

        TickEvaporation();
    }

    private void OnPuddleInit(Entity<PuddleComponent> entity, ref ComponentInit args)
    {
        _solutionContainerSystem.EnsureSolution(entity.Owner, entity.Comp.SolutionName, FixedPoint2.New(PuddleVolume),
            out _);
    }

    private void OnSolutionUpdate(Entity<PuddleComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != entity.Comp.SolutionName)
            return;

        if (args.Solution.Volume <= 0)
        {
            _deletionQueue.Add(entity);
            return;
        }

        _deletionQueue.Remove(entity);
        UpdateSlip(entity, entity.Comp, args.Solution);
        UpdateSlow(entity, args.Solution);
        UpdateEvaporation(entity, args.Solution);
        UpdateAppearance(entity, entity.Comp);
    }

    private void UpdateAppearance(EntityUid uid, PuddleComponent? puddleComponent = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref puddleComponent, ref appearance, false))
        {
            return;
        }

        var volume = FixedPoint2.Zero;
        Color color = Color.White;

        if (_solutionContainerSystem.ResolveSolution(uid, puddleComponent.SolutionName, ref puddleComponent.Solution,
                out var solution))
        {
            volume = solution.Volume / puddleComponent.OverflowVolume;

            // Make blood stand out more
            // Kinda EH
            // Could potentially do alpha per-solution but future problem.

            color = solution.GetColorWithout(_prototypeManager, _standoutReagents);
            color = color.WithAlpha(0.7f);

            foreach (var standout in _standoutReagents)
            {
                var quantity = solution.GetTotalPrototypeQuantity(standout);
                if (quantity <= FixedPoint2.Zero)
                    continue;

                var interpolateValue = quantity.Float() / solution.Volume.Float();
                color = Color.InterpolateBetween(color,
                    _prototypeManager.Index<ReagentPrototype>(standout).SubstanceColor, interpolateValue);
            }
        }

        _appearance.SetData(uid, PuddleVisuals.CurrentVolume, volume.Float(), appearance);
        _appearance.SetData(uid, PuddleVisuals.SolutionColor, color, appearance);
    }

    private void UpdateSlip(EntityUid entityUid, PuddleComponent component, Solution solution)
    {
        var isSlippery = false;
        // The base sprite is currently at 0.3 so we require at least 2nd tier to be slippery or else it's too hard to see.
        var amountRequired = FixedPoint2.New(component.OverflowVolume.Float() * LowThreshold);
        var slipperyAmount = FixedPoint2.Zero;

        foreach (var (reagent, quantity) in solution.Contents)
        {
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);

            if (reagentProto.Slippery)
            {
                slipperyAmount += quantity;

                if (slipperyAmount > amountRequired)
                {
                    isSlippery = true;
                    break;
                }
            }
        }

        if (isSlippery)
        {
            var comp = EnsureComp<StepTriggerComponent>(entityUid);
            _stepTrigger.SetActive(entityUid, true, comp);
            var friction = EnsureComp<TileFrictionModifierComponent>(entityUid);
            _tile.SetModifier(entityUid, TileFrictionController.DefaultFriction * 0.5f, friction);
        }
        else if (TryComp<StepTriggerComponent>(entityUid, out var comp))
        {
            _stepTrigger.SetActive(entityUid, false, comp);
            RemCompDeferred<TileFrictionModifierComponent>(entityUid);
        }
    }

    private void UpdateSlow(EntityUid uid, Solution solution)
    {
        var maxViscosity = 0f;
        foreach (var (reagent, _) in solution.Contents)
        {
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);
            maxViscosity = Math.Max(maxViscosity, reagentProto.Viscosity);
        }

        if (maxViscosity > 0)
        {
            var comp = EnsureComp<SlowContactsComponent>(uid);
            var speed = 1 - maxViscosity;
            _slowContacts.ChangeModifiers(uid, speed, comp);
        }
        else
        {
            RemComp<SlowContactsComponent>(uid);
        }
    }

    private void OnAnchorChanged(Entity<PuddleComponent> entity, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            QueueDel(entity);
    }

    /// <summary>
    ///     Gets the current volume of the given puddle, which may not necessarily be PuddleVolume.
    /// </summary>
    public FixedPoint2 CurrentVolume(EntityUid uid, PuddleComponent? puddleComponent = null)
    {
        if (!Resolve(uid, ref puddleComponent))
            return FixedPoint2.Zero;

        return _solutionContainerSystem.ResolveSolution(uid, puddleComponent.SolutionName, ref puddleComponent.Solution,
            out var solution)
            ? solution.Volume
            : FixedPoint2.Zero;
    }

    /// <summary>
    /// Try to add solution to <paramref name="puddleUid"/>.
    /// </summary>
    /// <param name="puddleUid">Puddle to which we add</param>
    /// <param name="addedSolution">Solution that is added to puddleComponent</param>
    /// <param name="sound">Play sound on overflow</param>
    /// <param name="checkForOverflow">Overflow on encountered values</param>
    /// <param name="puddleComponent">Optional resolved PuddleComponent</param>
    /// <returns></returns>
    public bool TryAddSolution(EntityUid puddleUid,
        Solution addedSolution,
        bool sound = true,
        bool checkForOverflow = true,
        PuddleComponent? puddleComponent = null,
        SolutionContainerManagerComponent? sol = null)
    {
        if (!Resolve(puddleUid, ref puddleComponent, ref sol))
            return false;

        _solutionContainerSystem.EnsureAllSolutions((puddleUid, sol));

        if (addedSolution.Volume == 0 ||
            !_solutionContainerSystem.ResolveSolution(puddleUid, puddleComponent.SolutionName,
                ref puddleComponent.Solution))
        {
            return false;
        }

        _solutionContainerSystem.AddSolution(puddleComponent.Solution.Value, addedSolution);

        if (checkForOverflow && IsOverflowing(puddleUid, puddleComponent))
        {
            EnsureComp<ActiveEdgeSpreaderComponent>(puddleUid);
        }

        if (!sound)
        {
            return true;
        }

        _audio.PlayPvs(puddleComponent.SpillSound, puddleUid);
        return true;
    }

    /// <summary>
    ///     Whether adding this solution to this puddle would overflow.
    /// </summary>
    public bool WouldOverflow(EntityUid uid, Solution solution, PuddleComponent? puddle = null)
    {
        if (!Resolve(uid, ref puddle))
            return false;

        return CurrentVolume(uid, puddle) + solution.Volume > puddle.OverflowVolume;
    }

    /// <summary>
    ///     Whether adding this solution to this puddle would overflow.
    /// </summary>
    private bool IsOverflowing(EntityUid uid, PuddleComponent? puddle = null)
    {
        if (!Resolve(uid, ref puddle))
            return false;

        return CurrentVolume(uid, puddle) > puddle.OverflowVolume;
    }

    /// <summary>
    /// Gets the solution amount above the overflow threshold for the puddle.
    /// </summary>
    public Solution GetOverflowSolution(EntityUid uid, PuddleComponent? puddle = null)
    {
        if (!Resolve(uid, ref puddle) ||
            !_solutionContainerSystem.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution))
        {
            return new Solution(0);
        }

        // TODO: This is going to fail with struct solutions.
        var remaining = puddle.OverflowVolume;
        var split = _solutionContainerSystem.SplitSolution(puddle.Solution.Value,
            CurrentVolume(uid, puddle) - remaining);
        return split;
    }

    #region Spill

    /// <summary>
    ///     First splashes reagent on reactive entities near the spilling entity, then spills the rest regularly to a
    ///     puddle. This is intended for 'destructive' spills, like when entities are destroyed or thrown.
    /// </summary>
    public bool TrySplashSpillAt(EntityUid uid,
        EntityCoordinates coordinates,
        Solution solution,
        out EntityUid puddleUid,
        bool sound = true,
        EntityUid? user = null)
    {
        puddleUid = EntityUid.Invalid;

        if (solution.Volume == 0)
            return false;

        var targets = new List<EntityUid>();
        var reactive = new HashSet<Entity<ReactiveComponent>>();
        _lookup.GetEntitiesInRange(coordinates, 1.0f, reactive);

        // Get reactive entities nearby--if there are some, it'll spill a bit on them instead.
        foreach (var ent in reactive)
        {
            // sorry! no overload for returning uid, so .owner must be used
            var owner = ent.Owner;

            // between 5 and 30%
            var splitAmount = solution.Volume * _random.NextFloat(0.05f, 0.30f);
            var splitSolution = solution.SplitSolution(splitAmount);

            if (user != null)
            {
                _adminLogger.Add(LogType.Landed,
                    $"{ToPrettyString(user.Value):user} threw {ToPrettyString(uid):entity} which splashed a solution {SolutionContainerSystem.ToPrettyString(solution):solution} onto {ToPrettyString(owner):target}");
            }

            targets.Add(owner);
            _reactive.DoEntityReaction(owner, splitSolution, ReactionMethod.Touch);
            _popups.PopupEntity(
                Loc.GetString("spill-land-spilled-on-other", ("spillable", uid),
                    ("target", Identity.Entity(owner, EntityManager))), owner, PopupType.SmallCaution);
        }

        _color.RaiseEffect(solution.GetColor(_prototypeManager), targets,
            Filter.Pvs(uid, entityManager: EntityManager));

        return TrySpillAt(coordinates, solution, out puddleUid, sound);
    }

    /// <summary>
    ///     Spills solution at the specified coordinates.
    /// Will add to an existing puddle if present or create a new one if not.
    /// </summary>
    public bool TrySpillAt(EntityCoordinates coordinates, Solution solution, out EntityUid puddleUid, bool sound = true)
    {
        if (solution.Volume == 0)
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        if (!_mapManager.TryGetGrid(coordinates.GetGridUid(EntityManager), out var mapGrid))
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        return TrySpillAt(mapGrid.GetTileRef(coordinates), solution, out puddleUid, sound);
    }

    /// <summary>
    /// <see cref="TrySpillAt(Robust.Shared.Map.EntityCoordinates,Content.Shared.Chemistry.Components.Solution,out Robust.Shared.GameObjects.EntityUid,bool)"/>
    /// </summary>
    public bool TrySpillAt(EntityUid uid, Solution solution, out EntityUid puddleUid, bool sound = true,
        TransformComponent? transformComponent = null)
    {
        if (!Resolve(uid, ref transformComponent, false))
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        return TrySpillAt(transformComponent.Coordinates, solution, out puddleUid, sound: sound);
    }

    /// <summary>
    /// <see cref="TrySpillAt(Robust.Shared.Map.EntityCoordinates,Content.Shared.Chemistry.Components.Solution,out Robust.Shared.GameObjects.EntityUid,bool)"/>
    /// </summary>
    public bool TrySpillAt(TileRef tileRef, Solution solution, out EntityUid puddleUid, bool sound = true,
        bool tileReact = true)
    {
        if (solution.Volume <= 0)
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        // If space return early, let that spill go out into the void
        if (tileRef.Tile.IsEmpty || tileRef.IsSpace(_tileDefMan))
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        // Let's not spill to invalid grids.
        var gridId = tileRef.GridUid;
        if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        if (tileReact)
        {
            // First, do all tile reactions
            DoTileReactions(tileRef, solution);
        }

        // Tile reactions used up everything.
        if (solution.Volume == FixedPoint2.Zero)
        {
            puddleUid = EntityUid.Invalid;
            return false;
        }

        // Get normalized co-ordinate for spill location and spill it in the centre
        // TODO: Does SnapGrid or something else already do this?
        var anchored = mapGrid.GetAnchoredEntitiesEnumerator(tileRef.GridIndices);
        var puddleQuery = GetEntityQuery<PuddleComponent>();
        var sparklesQuery = GetEntityQuery<EvaporationSparkleComponent>();

        while (anchored.MoveNext(out var ent))
        {
            // If there's existing sparkles then delete it
            if (sparklesQuery.TryGetComponent(ent, out var sparkles))
            {
                QueueDel(ent.Value);
                continue;
            }

            if (!puddleQuery.TryGetComponent(ent, out var puddle))
                continue;

            if (TryAddSolution(ent.Value, solution, sound, puddleComponent: puddle))
            {
                EnsureComp<ActiveEdgeSpreaderComponent>(ent.Value);
            }

            puddleUid = ent.Value;
            return true;
        }

        var coords = mapGrid.GridTileToLocal(tileRef.GridIndices);
        puddleUid = EntityManager.SpawnEntity("Puddle", coords);
        EnsureComp<PuddleComponent>(puddleUid);
        if (TryAddSolution(puddleUid, solution, sound))
        {
            EnsureComp<ActiveEdgeSpreaderComponent>(puddleUid);
        }

        return true;
    }

    #endregion

    public void DoTileReactions(TileRef tileRef, Solution solution)
    {
        for (var i = solution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagent, quantity) = solution.Contents[i];
            var proto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);
            var removed = proto.ReactionTile(tileRef, quantity);
            if (removed <= FixedPoint2.Zero)
                continue;

            solution.RemoveReagent(reagent, removed);
        }
    }

    /// <summary>
    /// Tries to get the relevant puddle entity for a tile.
    /// </summary>
    public bool TryGetPuddle(TileRef tile, out EntityUid puddleUid)
    {
        puddleUid = EntityUid.Invalid;

        if (!TryComp<MapGridComponent>(tile.GridUid, out var grid))
            return false;

        var anc = grid.GetAnchoredEntitiesEnumerator(tile.GridIndices);
        var puddleQuery = GetEntityQuery<PuddleComponent>();

        while (anc.MoveNext(out var ent))
        {
            if (!puddleQuery.HasComponent(ent.Value))
                continue;

            puddleUid = ent.Value;
            return true;
        }

        return false;
    }
}
