using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Spreader;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Examine;
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
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Maps;
using Content.Shared.Effects;
using Robust.Server.Audio;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles solutions on floors. Also handles the spreader logic for where the solution overflows a specified volume.
/// </summary>
public sealed partial class PuddleSystem : SharedPuddleSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger= default!;
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
    private const string SpiderBlood = "SpiderBlood";

    private static string[] _standoutReagents = new[] { Blood, Slime, SpiderBlood };

    public static float PuddleVolume = 1000;

    // Using local deletion queue instead of the standard queue so that we can easily "undelete" if a puddle
    // loses & then gains reagents in a single tick.
    private HashSet<EntityUid> _deletionQueue = new();

    /*
     * TODO: Need some sort of way to do blood slash / vomit solution spill on its own
     * This would then evaporate into the puddle tile below
     */

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<PuddleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
        SubscribeLocalEvent<PuddleComponent, SolutionChangedEvent>(OnSolutionUpdate);
        SubscribeLocalEvent<PuddleComponent, ComponentInit>(OnPuddleInit);
        SubscribeLocalEvent<PuddleComponent, SpreadNeighborsEvent>(OnPuddleSpread);
        SubscribeLocalEvent<PuddleComponent, SlipEvent>(OnPuddleSlip);

        SubscribeLocalEvent<EvaporationComponent, MapInitEvent>(OnEvaporationMapInit);

        InitializeSpillable();
        InitializeTransfers();
    }

    private void OnPuddleSpread(EntityUid uid, PuddleComponent component, ref SpreadNeighborsEvent args)
    {
        var overflow = GetOverflowSolution(uid, component);

        if (overflow.Volume == FixedPoint2.Zero)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }

        var puddleQuery = GetEntityQuery<PuddleComponent>();

        // For overflows, we never go to a fully evaporative tile just to avoid continuously having to mop it.

        // First we overflow to neighbors with overflow capacity
        if (args.Neighbors.Count > 0)
        {
            _random.Shuffle(args.Neighbors);

            // Overflow to neighbors with remaining space.
            foreach (var neighbor in args.Neighbors)
            {
                if (!puddleQuery.TryGetComponent(neighbor, out var puddle) ||
                    !_solutionContainerSystem.TryGetSolution(neighbor, puddle.SolutionName, out var neighborSolution) ||
                    CanFullyEvaporate(neighborSolution))
                {
                    continue;
                }

                var remaining = puddle.OverflowVolume - neighborSolution.Volume;

                if (remaining <= FixedPoint2.Zero)
                    continue;

                var split = overflow.SplitSolution(remaining);

                if (!_solutionContainerSystem.TryAddSolution(neighbor, neighborSolution, split))
                    continue;

                args.Updates--;
                EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);

                if (args.Updates <= 0)
                    break;
            }

            if (overflow.Volume == FixedPoint2.Zero)
            {
                RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
                return;
            }
        }

        // Then we go to free tiles.
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

            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }

        // Then we go to anything else.
        if (overflow.Volume > FixedPoint2.Zero && args.Neighbors.Count > 0 && args.Updates > 0)
        {
            var spillPerNeighbor = overflow.Volume / args.Neighbors.Count;

            foreach (var neighbor in args.Neighbors)
            {
                // Overflow to neighbours (unless it's pure water)
                if (!puddleQuery.TryGetComponent(neighbor, out var puddle) ||
                    !_solutionContainerSystem.TryGetSolution(neighbor, puddle.SolutionName, out var neighborSolution) ||
                    CanFullyEvaporate(neighborSolution))
                {
                    continue;
                }

                var split = overflow.SplitSolution(spillPerNeighbor);

                if (!_solutionContainerSystem.TryAddSolution(neighbor, neighborSolution, split))
                    continue;

                EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);
                args.Updates--;

                if (args.Updates <= 0)
                    break;
            }
        }

        // Add the remainder back
        if (_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var puddleSolution))
        {
            _solutionContainerSystem.TryAddSolution(uid, puddleSolution, overflow);
        }
    }

    private void OnPuddleSlip(EntityUid uid, PuddleComponent component, ref SlipEvent args)
    {
        // Reactive entities have a chance to get a touch reaction from slipping on a puddle
        // (i.e. it is implied they fell face first onto it or something)
        if (!HasComp<ReactiveComponent>(args.Slipped))
            return;

        // Eventually probably have some system of 'body coverage' to tweak the probability but for now just 0.5
        // (implying that spacemen have a 50% chance to either land on their ass or their face)
        if (!_random.Prob(0.5f))
            return;

        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;

        _popups.PopupEntity(Loc.GetString("puddle-component-slipped-touch-reaction", ("puddle", uid)),
            args.Slipped, args.Slipped, PopupType.SmallCaution);

        // Take 15% of the puddle solution
        var splitSol = _solutionContainerSystem.SplitSolution(uid, solution, solution.Volume * 0.15f);
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

    private void OnPuddleInit(EntityUid uid, PuddleComponent component, ComponentInit args)
    {
        _solutionContainerSystem.EnsureSolution(uid, component.SolutionName, FixedPoint2.New(PuddleVolume), out _);
    }

    private void OnSolutionUpdate(EntityUid uid, PuddleComponent component, SolutionChangedEvent args)
    {
        if (args.Solution.Name != component.SolutionName)
            return;

        if (args.Solution.Volume <= 0)
        {
            _deletionQueue.Add(uid);
            return;
        }

        _deletionQueue.Remove(uid);
        UpdateSlip(uid, component, args.Solution);
        UpdateSlow(uid, args.Solution);
        UpdateEvaporation(uid, args.Solution);
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, PuddleComponent? puddleComponent = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref puddleComponent, ref appearance, false))
        {
            return;
        }

        var volume = FixedPoint2.Zero;
        Color color = Color.White;

        if (_solutionContainerSystem.TryGetSolution(uid, puddleComponent.SolutionName, out var solution))
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
                color = Color.InterpolateBetween(color, _prototypeManager.Index<ReagentPrototype>(standout).SubstanceColor, interpolateValue);
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

    private void HandlePuddleExamined(EntityUid uid, PuddleComponent component, ExaminedEvent args)
    {
        if (TryComp<StepTriggerComponent>(uid, out var slippery) && slippery.Active)
        {
            args.PushMarkup(Loc.GetString("puddle-component-examine-is-slipper-text"));
        }

        if (HasComp<EvaporationComponent>(uid))
        {
            if (_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution) &&
                CanFullyEvaporate(solution))
            {
                args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating"));
            }
            else if (solution?.ContainsPrototype(EvaporationReagent) == true)
            {
                args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-partial"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
        }
    }

    private void OnAnchorChanged(EntityUid uid, PuddleComponent puddle, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            QueueDel(uid);
    }

    /// <summary>
    ///     Gets the current volume of the given puddle, which may not necessarily be PuddleVolume.
    /// </summary>
    public FixedPoint2 CurrentVolume(EntityUid uid, PuddleComponent? puddleComponent = null)
    {
        if (!Resolve(uid, ref puddleComponent))
            return FixedPoint2.Zero;

        return _solutionContainerSystem.TryGetSolution(uid, puddleComponent.SolutionName,
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
        PuddleComponent? puddleComponent = null)
    {
        if (!Resolve(puddleUid, ref puddleComponent))
            return false;

        if (addedSolution.Volume == 0 ||
            !_solutionContainerSystem.TryGetSolution(puddleUid, puddleComponent.SolutionName,
                out var solution))
        {
            return false;
        }

        solution.AddSolution(addedSolution, _prototypeManager);
        _solutionContainerSystem.UpdateChemicals(puddleUid, solution, true);

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
        if (!Resolve(uid, ref puddle) || !_solutionContainerSystem.TryGetSolution(uid, puddle.SolutionName,
                out var solution))
        {
            return new Solution(0);
        }

        // TODO: This is going to fail with struct solutions.
        var remaining = puddle.OverflowVolume;
        var split = _solutionContainerSystem.SplitSolution(uid, solution, CurrentVolume(uid, puddle) - remaining);
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
            _popups.PopupEntity(Loc.GetString("spill-land-spilled-on-other", ("spillable", uid), ("target", Identity.Entity(owner, EntityManager))), owner, PopupType.SmallCaution);
        }

        _color.RaiseEffect(solution.GetColor(_prototypeManager), targets, Filter.Pvs(uid, entityManager: EntityManager));

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
    public bool TrySpillAt(EntityUid uid, Solution solution, out EntityUid puddleUid, bool sound = true, TransformComponent? transformComponent = null)
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
    public bool TrySpillAt(TileRef tileRef, Solution solution, out EntityUid puddleUid, bool sound = true, bool tileReact = true)
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
