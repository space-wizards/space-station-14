using Content.Server.Chemistry.EntitySystems;
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
        SubscribeLocalEvent<SpillableComponent, SolutionOverflowEvent>(OnOverflow);
        SubscribeLocalEvent<SpillableComponent, SpillDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SpillableComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnExamined(EntityUid uid, SpillableComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("spill-examine-is-spillable"));

        if (HasComp<MeleeWeaponComponent>(uid))
            args.PushMarkup(Loc.GetString("spill-examine-spillable-weapon"));
    }

    private void OnOverflow(EntityUid uid, SpillableComponent component, ref SolutionOverflowEvent args)
    {
        if (args.Handled)
            return;

        TrySpillAt(Transform(uid).Coordinates, args.Overflow, out _);
        args.Handled = true;
    }

    private void SplashOnMeleeHit(EntityUid uid, SpillableComponent component, MeleeHitEvent args)
    {
        if (args.Handled)
            return;

        // When attacking someone reactive with a spillable entity,
        // splash a little on them (touch react)
        // If this also has solution transfer, then assume the transfer amount is how much we want to spill.
        // Otherwise let's say they want to spill a quarter of its max volume.

        if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var solution))
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

            var splitSolution = _solutionContainerSystem.SplitSolution(uid, solution, totalSplit / hitCount);

            _adminLogger.Add(LogType.MeleeHit, $"{ToPrettyString(args.User)} splashed {SolutionContainerSystem.ToPrettyString(splitSolution):solution} from {ToPrettyString(uid):entity} onto {ToPrettyString(hit):target}");
            _reactive.DoEntityReaction(hit, splitSolution, ReactionMethod.Touch);

            _popups.PopupEntity(
                Loc.GetString("spill-melee-hit-attacker", ("amount", totalSplit / hitCount), ("spillable", uid),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, args.User);

            _popups.PopupEntity(
                Loc.GetString("spill-melee-hit-others", ("attacker", args.User), ("spillable", uid),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);
        }
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
        if (!isCorrectSlot)
            return;

        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;

        if (solution.Volume == 0)
            return;

        // spill all solution on the player
        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        TrySplashSpillAt(uid, Transform(args.Equipee).Coordinates, drainedSolution, out _);
    }

    private void SpillOnLand(EntityUid uid, SpillableComponent component, ref LandEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;

        if (_openable.IsClosed(uid))
            return;

        if (args.User != null)
        {
            _adminLogger.Add(LogType.Landed,
                $"{ToPrettyString(uid):entity} spilled a solution {SolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.Volume);
        TrySplashSpillAt(uid, Transform(uid).Coordinates, drainedSolution, out _);
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
        if (!_solutionContainerSystem.TryGetSolution(ent, ent.Comp.SolutionName, out var solution))
            return;

        args.Cancel("pacified-cannot-throw-spill");
    }

    private void AddSpillVerb(EntityUid uid, SpillableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetSolution(args.Target, component.SolutionName, out var solution))
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
        if (component.SpillDelay == null)
        {
            verb.Act = () =>
            {
                var puddleSolution = _solutionContainerSystem.SplitSolution(args.Target,
                    solution, solution.Volume);
                TrySpillAt(Transform(args.Target).Coordinates, puddleSolution, out _);
            };
        }
        else
        {
            verb.Act = () =>
            {
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.SpillDelay ?? 0, new SpillDoAfterEvent(), uid, target: uid)
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

    private void OnDoAfter(EntityUid uid, SpillableComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        //solution gone by other means before doafter completes
        if (!_solutionContainerSystem.TryGetDrainableSolution(uid, out var solution) || solution.Volume == 0)
            return;

        var puddleSolution = _solutionContainerSystem.SplitSolution(uid, solution, solution.Volume);
        TrySpillAt(Transform(uid).Coordinates, puddleSolution, out _);
        args.Handled = true;
    }
}
