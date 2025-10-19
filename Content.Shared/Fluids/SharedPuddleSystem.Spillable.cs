using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Spillable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Player;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    private static readonly FixedPoint2 MeleeHitTransferProportion = 0.25;

    protected virtual void InitializeSpillable()
    {
        SubscribeLocalEvent<SpillableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SpillableComponent, GetVerbsEvent<Verb>>(AddSpillVerb);
        SubscribeLocalEvent<SpillableComponent, MeleeHitEvent>(SplashOnMeleeHit, after: [typeof(OpenableSystem)]);
        SubscribeLocalEvent<SpillableComponent, AttemptPacifiedThrowEvent>(OnAttemptPacifiedThrow);
    }

    private void OnExamined(Entity<SpillableComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(SpillableComponent)))
        {
            args.PushMarkup(Loc.GetString("spill-examine-is-spillable"));

            if (HasComp<MeleeWeaponComponent>(entity))
                args.PushMarkup(Loc.GetString("spill-examine-spillable-weapon"));
        }
    }

    private void AddSpillVerb(Entity<SpillableComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!_solutionContainerSystem.TryGetSolution(args.Target,
                entity.Comp.SolutionName,
                out var soln,
                out var solution))
            return;

        if (Openable.IsClosed(args.Target))
            return;

        if (solution.Volume == FixedPoint2.Zero)
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

                // TODO: Make this an event subscription once spilling puddles is predicted.
                // Injectors should not be hardcoded here.
                if (TryComp<InjectorComponent>(entity, out var injectorComp))
                {
                    injectorComp.ToggleState = InjectorToggleMode.Draw;
                    Dirty(entity, injectorComp);
                }
            };
        }
        else
        {
            var user = args.User;
            verb.Act = () =>
            {
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager,
                    user,
                    entity.Comp.SpillDelay ?? 0,
                    new SpillDoAfterEvent(),
                    entity.Owner,
                    target: entity.Owner)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                });
            };
        }
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        verb.DoContactInteraction = true;
        args.Verbs.Add(verb);
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

        var totalSplit = FixedPoint2.Min(solution.MaxVolume * MeleeHitTransferProportion, solution.Volume);
        if (TryComp<SolutionTransferComponent>(entity, out var transfer))
            totalSplit = FixedPoint2.Min(transfer.TransferAmount, solution.Volume);

        // a little lame, but reagent quantity is not very balanced and we don't want people
        // spilling like 100u of reagent on someone at once!
        totalSplit = FixedPoint2.Min(totalSplit, entity.Comp.MaxMeleeSpillAmount);

        if (totalSplit == 0)
            return;

        // Optionally allow further melee handling occur
        args.Handled = entity.Comp.PreventMelee;

        // First update the hit count so anything that is not reactive wont count towards the total!
        foreach (var hit in args.HitEntities)
        {
            if (!_reactiveQuery.HasComp(hit))
                hitCount -= 1;
        }

        foreach (var hit in args.HitEntities)
        {
            if (!_reactiveQuery.HasComp(hit))
                continue;

            var splitSolution = _solutionContainerSystem.SplitSolution(soln.Value, totalSplit / hitCount);

            AdminLogger.Add(LogType.MeleeHit,
                $"{ToPrettyString(args.User):actor} "
                + $"splashed {SharedSolutionContainerSystem.ToPrettyString(splitSolution):solution} "
                + $"from {ToPrettyString(entity.Owner):entity} onto {ToPrettyString(hit):target}");

            Reactive.DoEntityReaction(hit, splitSolution, ReactionMethod.Touch);

            Popups.PopupClient(Loc.GetString("spill-melee-hit-attacker",
                    ("amount", totalSplit / hitCount),
                    ("spillable", entity.Owner),
                    ("target", Identity.Entity(hit, EntityManager, args.User))),
                hit,
                args.User);
            Popups.PopupEntity(
                Loc.GetString("spill-melee-hit-others",
                    ("attacker", Identity.Entity(args.User, EntityManager)),
                    ("spillable", entity.Owner),
                    ("target", Identity.Entity(hit, EntityManager))),
                hit,
                Filter.PvsExcept(args.User),
                true,
                PopupType.SmallCaution);
        }
    }

    /// <summary>
    /// Prevent Pacified entities from throwing items that can spill liquids.
    /// </summary>
    private void OnAttemptPacifiedThrow(Entity<SpillableComponent> ent, ref AttemptPacifiedThrowEvent args)
    {
        // Don’t care about closed containers.
        if (Openable.IsClosed(ent))
            return;

        // Don’t care about empty containers.
        if (!_solutionContainerSystem.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var solution)
            || solution.Volume <= 0)
            return;

        args.Cancel("pacified-cannot-throw-spill");
    }
}
