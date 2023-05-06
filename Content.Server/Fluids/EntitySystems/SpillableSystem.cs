using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
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
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Spillable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class SpillableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpillableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        SubscribeLocalEvent<SpillableComponent, MeleeHitEvent>(SplashOnMeleeHit);
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
        SubscribeLocalEvent<SpillableComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpillableComponent, SolutionSpikeOverflowEvent>(OnSpikeOverflow);
        SubscribeLocalEvent<SpillableComponent, SpillDoAfterEvent>(OnDoAfter);
    }

    private void OnExamined(EntityUid uid, SpillableComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("spill-examine-is-spillable"));

        if (HasComp<MeleeWeaponComponent>(uid))
            args.PushMarkup(Loc.GetString("spill-examine-spillable-weapon"));
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
                $"{ToPrettyString(args.User.Value):user} threw {ToPrettyString(uid):entity} which spilled a solution {SolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        SplashSpillAt(uid, drainedSolution, Transform(uid).Coordinates, "PuddleSmear");
    }

    private void SplashOnMeleeHit(EntityUid uid, SpillableComponent component, MeleeHitEvent args)
    {
        // When attacking someone reactive with a spillable entity,
        // splash a little on them (touch react)
        // If this also has solution transfer, then assume the transfer amount is how much we want to spill.
        // Otherwise let's say they want to spill a quarter of its max volume.

        if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var solution))
            return;

        if (TryComp<DrinkComponent>(uid, out var drink) && !drink.Opened)
            return;

        var hitCount = args.HitEntities.Count;

        var totalSplit = FixedPoint2.Min(solution.MaxVolume * 0.25, solution.Volume);
        if (TryComp<SolutionTransferComponent>(uid, out var transfer))
        {
            totalSplit = FixedPoint2.Min(transfer.TransferAmount, solution.Volume);
        }

        // a little lame, but reagent quantity is not very balanced and we don't want people
        // spilling like 100u of reagent on someone at once!
        totalSplit = FixedPoint2.Min(totalSplit, component.MaxMeleeSpillAmount);

        foreach (var hit in args.HitEntities)
        {
            if (!HasComp<ReactiveComponent>(hit))
            {
                hitCount -= 1; // so we don't undershoot solution calculation for actual reactive entities
                continue;
            }

            var splitSolution = _solutionContainerSystem.SplitSolution(uid, solution, totalSplit / hitCount);

            _adminLogger.Add(LogType.MeleeHit, $"{ToPrettyString(args.User)} splashed {SolutionContainerSystem.ToPrettyString(splitSolution):solution} from {ToPrettyString(uid):entity} onto {ToPrettyString(hit):target}");
            _reactive.DoEntityReaction(hit, splitSolution, ReactionMethod.Touch);

            _popup.PopupEntity(
                Loc.GetString("spill-melee-hit-attacker", ("amount", totalSplit / hitCount), ("spillable", uid),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, args.User);

            _popup.PopupEntity(
                Loc.GetString("spill-melee-hit-others", ("attacker", args.User), ("spillable", uid),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);
        }
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

        verb.Act = () =>
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(args.User, component.SpillDelay ?? 0, new SpillDoAfterEvent(), uid, target: uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                NeedHand = true,
            });
        };
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        verb.DoContactInteraction = true;
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     First splashes reagent on reactive entities near the spilling entity, then spills the rest regularly to a
    ///     puddle. This is intended for 'destructive' spills, like when entities are destroyed or thrown.
    /// </summary>
    public PuddleComponent? SplashSpillAt(EntityUid uid, Solution solution, EntityCoordinates coordinates, string prototype,
        bool overflow = true, bool sound = true, bool combine = true, EntityUid? user=null)
    {
        if (solution.Volume == 0)
            return null;

        // Get reactive entities nearby--if there are some, it'll spill a bit on them instead.
        foreach (var ent in _entityLookup.GetComponentsInRange<ReactiveComponent>(coordinates, 1.0f))
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

            _reactive.DoEntityReaction(owner, splitSolution, ReactionMethod.Touch);
            _popup.PopupEntity(Loc.GetString("spill-land-spilled-on-other", ("spillable", uid), ("target", owner)), owner, PopupType.SmallCaution);
        }

        return SpillAt(solution, coordinates, prototype, overflow, sound, combine: combine);
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

    private void OnDoAfter(EntityUid uid, SpillableComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //solution gone by other means before doafter completes
        if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(uid, solution, solution.Volume);

        SpillAt(puddleSolution, Transform(uid).Coordinates, "PuddleSmear");

        args.Handled = true;
    }
}
