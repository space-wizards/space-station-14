using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Spillable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private void InitializeSpillable()
    {
        SubscribeLocalEvent<SpillableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        // openable handles the event if its closed
        SubscribeLocalEvent<SpillableComponent, MeleeHitEvent>(SplashOnMeleeHit, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
        SubscribeLocalEvent<SpillableComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<SpillableComponent, SolutionContainerOverflowEvent>(OnOverflow);
        SubscribeLocalEvent<SpillableComponent, SpillDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SpillableComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnExamined(Entity<SpillableComponent> entity, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("spill-examine-is-spillable"));

        if (HasComp<MeleeWeaponComponent>(entity))
            args.PushMarkup(Loc.GetString("spill-examine-spillable-weapon"));
    }

    private void OnOverflow(Entity<SpillableComponent> entity, ref SolutionContainerOverflowEvent args)
    {
        if (args.Handled)
            return;

        TrySpillAt(Transform(entity).Coordinates, args.Overflow, out _);
        args.Handled = true;
    }

    private void SplashOnMeleeHit(Entity<SpillableComponent> entity, ref MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        // When attacking someone reactive with a spillable entity,
        // splash a little on them (touch react)
        // If this also has solution transfer, then assume the transfer amount is how much we want to spill.
        // Otherwise let's say they want to spill a quarter of its max volume.

        if (!_solutionContainerSystem.TryGetDrainableSolution(entity.Owner, out var soln, out var solution))
            return;

        var hitCount = args.HitEntities.Count;

        var totalSplit = FixedPoint2.Min(solution.MaxVolume * 0.25, solution.Volume);
        if (TryComp<SolutionTransferComponent>(entity, out var transfer))
        {
            totalSplit = FixedPoint2.Min(transfer.TransferAmount, solution.Volume);
        }

        // a little lame, but reagent quantity is not very balanced and we don't want people
        // spilling like 100u of reagent on someone at once!
        totalSplit = FixedPoint2.Min(totalSplit, entity.Comp.MaxMeleeSpillAmount);

        if (totalSplit == 0)
            return;

        args.Handled = true;
        foreach (var hit in args.HitEntities)
        {
            if (!HasComp<ReactiveComponent>(hit))
            {
                hitCount -= 1; // so we don't undershoot solution calculation for actual reactive entities
                continue;
            }

            var splitSolution = _solutionContainerSystem.SplitSolution(soln.Value, totalSplit / hitCount);

            _adminLogger.Add(LogType.MeleeHit, $"{ToPrettyString(args.User)} splashed {SolutionContainerSystem.ToPrettyString(splitSolution):solution} from {ToPrettyString(entity.Owner):entity} onto {ToPrettyString(hit):target}");
            _reactive.DoEntityReaction(hit, splitSolution, ReactionMethod.Touch);

            _popups.PopupEntity(
                Loc.GetString("spill-melee-hit-attacker", ("amount", totalSplit / hitCount), ("spillable", entity.Owner),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, args.User);

            _popups.PopupEntity(
                Loc.GetString("spill-melee-hit-others", ("attacker", args.User), ("spillable", entity.Owner),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);
        }
    }

    private void OnGotEquipped(Entity<SpillableComponent> entity, ref GotEquippedEvent args)
    {
        if (!entity.Comp.SpillWorn)
            return;

        if (!TryComp(entity, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln, out var solution))
            return;

        if (solution.Volume == 0)
            return;

        // spill all solution on the player
        var drainedSolution = _solutionContainerSystem.Drain(entity.Owner, soln.Value, solution.Volume);
        TrySplashSpillAt(entity.Owner, Transform(args.Equipee).Coordinates, drainedSolution, out _);
    }

    private void SpillOnLand(Entity<SpillableComponent> entity, ref LandEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(entity.Owner, entity.Comp.SolutionName, out var soln, out var solution))
            return;

        if (_openable.IsClosed(entity.Owner))
            return;

        if (args.User != null)
        {
            _adminLogger.Add(LogType.Landed,
                $"{ToPrettyString(entity.Owner):entity} spilled a solution {SolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(entity.Owner, soln.Value, solution.Volume);
        TrySplashSpillAt(entity.Owner, Transform(entity).Coordinates, drainedSolution, out _);
    }

    /// <summary>
    /// Prevent Pacified entities from throwing items that can spill liquids.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<SpillableComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        // Don’t care about closed containers.
        if (_openable.IsClosed(ent))
            return;

        // Don’t care about empty containers.
        if (!_solutionContainerSystem.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var solution) || solution.Volume <= 0)
            return;

        args.Cancel("pacified-cannot-throw-spill");
    }

    private void AddSpillVerb(Entity<SpillableComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetSolution(args.Target, entity.Comp.SolutionName, out var soln, out var solution))
            return;

        if (_openable.IsClosed(args.Target))
            return;

        if (solution.Volume == FixedPoint2.Zero)
            return;

        if (_entityManager.HasComponent<PreventSpillerComponent>(args.User))
            return;


        Verb verb = new()
        {
            Text = Loc.GetString("spill-target-verb-get-data-text")
        };

        // TODO VERB ICONS spill icon? pouring out a glass/beaker?
        if (entity.Comp.SpillDelay == null)
        {
            var target = args.Target;
            verb.Act = () =>
            {
                var puddleSolution = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume);
                TrySpillAt(Transform(target).Coordinates, puddleSolution, out _);
            };
        }
        else
        {
            var user = args.User;
            verb.Act = () =>
            {
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, entity.Comp.SpillDelay ?? 0, new SpillDoAfterEvent(), entity.Owner, target: entity.Owner)
                {
                    BreakOnTargetMove = true,
                    BreakOnUserMove = true,
                    BreakOnDamage = true,
                    NeedHand = true,
                });
            };
        }
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        verb.DoContactInteraction = true;
        args.Verbs.Add(verb);
    }

    private void OnDoAfter(Entity<SpillableComponent> entity, ref SpillDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //solution gone by other means before doafter completes
        if (!_solutionContainerSystem.TryGetDrainableSolution(entity.Owner, out var soln, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(soln.Value, solution.Volume);
        TrySpillAt(Transform(entity).Coordinates, puddleSolution, out _);
        args.Handled = true;
    }
}
