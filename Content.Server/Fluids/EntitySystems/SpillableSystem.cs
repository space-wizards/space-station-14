using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class SpillableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger= default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
        SubscribeLocalEvent<SpillableComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpillableComponent, SolutionSpikeOverflowEvent>(OnSpikeOverflow);
        SubscribeLocalEvent<SpillableComponent, SpillFinishedEvent>(OnSpillFinished);
        SubscribeLocalEvent<SpillableComponent, SpillCancelledEvent>(OnSpillCancelled);
    }

    private void OnSpikeOverflow(EntityUid uid, SpillableComponent component, SolutionSpikeOverflowEvent args)
    {
        if (!args.Handled)
        {
            SpillAt(args.Overflow, Transform(uid).Coordinates, "PuddleSmear");
        }

        args.Handled = true;
    }

    private void OnGotEquipped(EntityUid uid, SpillableComponent component, GotEquippedEvent args)
    {
        if (!component.SpillWorn)
            return;

        if (!TryComp(uid, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot) return;

        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;
        if (solution.Volume == 0)
            return;

        // spill all solution on the player
        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        SpillAt(args.Equipee, drainedSolution, "PuddleSmear");
    }

    /// <summary>
    ///     Spills the specified solution at the entity's location if possible.
    /// </summary>
    /// <param name="uid">
    ///     The entity to use as a location to spill the solution at.
    /// </param>
    /// <param name="solution">Initial solution for the prototype.</param>
    /// <param name="prototype">The prototype to use.</param>
    /// <param name="sound">Play the spill sound.</param>
    /// <param name="combine">Whether to attempt to merge with existing puddles</param>
    /// <param name="transformComponent">Optional Transform component</param>
    /// <returns>The puddle if one was created, null otherwise.</returns>
    public PuddleComponent? SpillAt(EntityUid uid, Solution solution, string prototype,
        bool sound = true, bool combine = true, TransformComponent? transformComponent = null)
    {
        return !Resolve(uid, ref transformComponent, false)
            ? null
            : SpillAt(solution, transformComponent.Coordinates, prototype, sound: sound, combine: combine);
    }

    private void SpillOnLand(EntityUid uid, SpillableComponent component, ref LandEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution)) return;

        if (TryComp<DrinkComponent>(uid, out var drink) && (!drink.Opened))
            return;

        if (args.User != null)
        {
            _adminLogger.Add(LogType.Landed,
                $"{ToPrettyString(uid):entity} spilled a solution {SolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        SpillAt(drainedSolution, EntityManager.GetComponent<TransformComponent>(uid).Coordinates, "PuddleSmear");
    }

    private void AddSpillVerb(EntityUid uid, SpillableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetDrainableSolution(args.Target, out var solution))
            return;

        if (TryComp<DrinkComponent>(args.Target, out var drink) && (!drink.Opened))
            return;

        if (solution.Volume == FixedPoint2.Zero)
            return;

        Verb verb = new();
        verb.Text = Loc.GetString("spill-target-verb-get-data-text");
        // TODO VERB ICONS spill icon? pouring out a glass/beaker?
        if (component.SpillDelay == null)
        {
            verb.Act = () =>
            {
                var puddleSolution = _solutionContainerSystem.SplitSolution(args.Target,
                    solution, solution.Volume);
                SpillAt(puddleSolution, Transform(args.Target).Coordinates, "PuddleSmear");
            };
        }
        else
        {
            verb.Act = () =>
            {
                if (component.CancelToken == null)
                {
                    component.CancelToken = new CancellationTokenSource();
                    _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.SpillDelay.Value, component.CancelToken.Token, component.Owner)
                    {
                        BreakOnTargetMove = true,
                        BreakOnUserMove = true,
                        BreakOnDamage = true,
                        BreakOnStun = true,
                        NeedHand = true,
                        TargetFinishedEvent = new SpillFinishedEvent(args.User, component.Owner, solution),
                        TargetCancelledEvent = new SpillCancelledEvent(component.Owner)
                    });
                }
            };
        }
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        verb.DoContactInteraction = true;
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Spills solution at the specified grid coordinates.
    /// </summary>
    /// <param name="solution">Initial solution for the prototype.</param>
    /// <param name="coordinates">The coordinates to spill the solution at.</param>
    /// <param name="prototype">The prototype to use.</param>
    /// <param name="overflow">If the puddle overflow will be calculated. Defaults to true.</param>
    /// <param name="sound">Whether or not to play the spill sound.</param>
    /// <param name="combine">Whether to attempt to merge with existing puddles</param>
    /// <returns>The puddle if one was created, null otherwise.</returns>
    public PuddleComponent? SpillAt(Solution solution, EntityCoordinates coordinates, string prototype,
        bool overflow = true, bool sound = true, bool combine = true)
    {
        if (solution.Volume == 0) return null;


        if (!_mapManager.TryGetGrid(coordinates.GetGridUid(EntityManager), out var mapGrid))
            return null; // Let's not spill to space.

        return SpillAt(mapGrid.GetTileRef(coordinates), solution, prototype, overflow, sound,
            combine: combine);
    }

    public bool TryGetPuddle(TileRef tileRef, [NotNullWhen(true)] out PuddleComponent? puddle)
    {
        foreach (var entity in _entityLookup.GetEntitiesIntersecting(tileRef))
        {
            if (EntityManager.TryGetComponent(entity, out PuddleComponent? p))
            {
                puddle = p;
                return true;
            }
        }

        puddle = null;
        return false;
    }

    public PuddleComponent? SpillAt(TileRef tileRef, Solution solution, string prototype,
        bool overflow = true, bool sound = true, bool noTileReact = false, bool combine = true)
    {
        if (solution.Volume <= 0) return null;

        // If space return early, let that spill go out into the void
        if (tileRef.Tile.IsEmpty) return null;

        var gridId = tileRef.GridUid;
        if (!_mapManager.TryGetGrid(gridId, out var mapGrid)) return null; // Let's not spill to invalid grids.

        if (!noTileReact)
        {
            // First, do all tile reactions
            for (var i = 0; i < solution.Contents.Count; i++)
            {
                var (reagentId, quantity) = solution.Contents[i];
                var proto = _prototypeManager.Index<ReagentPrototype>(reagentId);
                var removed = proto.ReactionTile(tileRef, quantity);
                if (removed <= FixedPoint2.Zero) continue;
                solution.RemoveReagent(reagentId, removed);
            }
        }

        // Tile reactions used up everything.
        if (solution.Volume == FixedPoint2.Zero)
            return null;

        // Get normalized co-ordinate for spill location and spill it in the centre
        // TODO: Does SnapGrid or something else already do this?
        var spillGridCoords = mapGrid.GridTileToLocal(tileRef.GridIndices);
        var startEntity = EntityUid.Invalid;
        PuddleComponent? puddleComponent = null;

        if (combine)
        {
            var spillEntities = _entityLookup.GetEntitiesIntersecting(tileRef).ToArray();

            foreach (var spillEntity in spillEntities)
            {
                if (!EntityManager.TryGetComponent(spillEntity, out puddleComponent)) continue;

                if (!overflow && _puddleSystem.WouldOverflow(puddleComponent.Owner, solution, puddleComponent))
                    return null;

                if (!_puddleSystem.TryAddSolution(puddleComponent.Owner, solution, sound, overflow)) continue;

                startEntity = puddleComponent.Owner;
                break;
            }
        }

        if (startEntity != EntityUid.Invalid)
            return puddleComponent;

        startEntity = EntityManager.SpawnEntity(prototype, spillGridCoords);
        puddleComponent = EntityManager.EnsureComponent<PuddleComponent>(startEntity);
        _puddleSystem.TryAddSolution(startEntity, solution, sound, overflow);

        return puddleComponent;
    }

    private void OnSpillFinished(EntityUid uid, SpillableComponent component, SpillFinishedEvent ev)
    {
        component.CancelToken = null;

        //solution gone by other means before doafter completes
        if (ev.Solution == null || ev.Solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(uid,
            ev.Solution, ev.Solution.Volume);

        SpillAt(puddleSolution, Transform(component.Owner).Coordinates, "PuddleSmear");
    }

    private void OnSpillCancelled(EntityUid uid, SpillableComponent component, SpillCancelledEvent ev)
    {
        component.CancelToken = null;
    }

    internal sealed class SpillFinishedEvent : EntityEventArgs
    {
        public SpillFinishedEvent(EntityUid user, EntityUid spillable, Solution solution)
        {
            User = user;
            Spillable = spillable;
            Solution = solution;
        }

        public EntityUid User { get; }
        public EntityUid Spillable { get; }
        public Solution Solution { get; }
    }

    private sealed class SpillCancelledEvent : EntityEventArgs
    {
        public EntityUid Spillable;

        public SpillCancelledEvent(EntityUid spillable)
        {
            Spillable = spillable;
        }
    }
}
