using System;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Localization;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Cancels solution transfers into a target container,
/// if at least one of the incoming reagents are not whitelisted.
/// </summary>
public sealed class RefillReagentWhitelistSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RefillReagentWhitelistComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
    }

    private void OnTransferAttempt(Entity<RefillReagentWhitelistComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        // Only validate when this entity is the transfer target.
        if (ent.Owner != args.To)
            return;

        // Ensure the target actually has a relevant solution container.
        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out var _))
            return;

        // Identify the guarded solution name
        string? targetSolutionName = null;
        if (TryComp<RefillableSolutionComponent>(ent.Owner, out var refillable))
            targetSolutionName = refillable.Solution;
        else if (TryComp<InjectableSolutionComponent>(ent.Owner, out var injectable))
            targetSolutionName = injectable.Solution;

        if (targetSolutionName == null)
            return;

        // Only enforce for the specific named solution.
        if (!string.Equals(targetSolutionName, ent.Comp.Solution, StringComparison.Ordinal))
            return;

        // Get the source solution to inspect what would be transferred.
        // Normal container transfers: source has DrainableSolutionComponent.
        // Syringes/injectors: don't have DrainableSolutionComponent; resolve via InjectorComponent instead.
        Solution? sourceSoln = null;
        if (_solutions.TryGetDrainableSolution(args.From, out var sourceSolnEnt, out var drainable))
        {
            sourceSoln = drainable;
        }
        else if (TryComp<InjectorComponent>(args.From, out var injector))
        {
            if (_solutions.ResolveSolution(args.From, injector.SolutionName, ref injector.Solution, out var sol))
                sourceSoln = sol;
        }

        if (sourceSoln == null || sourceSoln.Volume == FixedPoint2.Zero)
            return;

        // If whitelist is empty, block everything
        if (ent.Comp.Allowed.Count == 0)
        {
            Cancel(ref args, ent);
            return;
        }

        // Check each reagent present in the source. If any is not allowed, cancel.
        foreach (var rq in sourceSoln.Contents)
        {
            var proto = rq.Reagent.Prototype;
            var allowed = false;
            foreach (var allow in ent.Comp.Allowed)
            {
                if (allow == proto)
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
            {
                Cancel(ref args, ent);
                return;
            }
        }
    }

    private void Cancel(ref SolutionTransferAttemptEvent args, Entity<RefillReagentWhitelistComponent> ent)
    {
        var reason = ent.Comp.Popup ?? "Transfer blocked: reagent not allowed for this container.";
        args.Cancel(reason);
    }
}
