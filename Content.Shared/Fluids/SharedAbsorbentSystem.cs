using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

/// <summary>
/// Mopping logic for interacting with puddle components.
/// </summary>
public abstract class SharedAbsorbentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] protected readonly SharedPuddleSystem Puddle = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AbsorbentComponent, UserActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<AbsorbentComponent, SolutionContainerChangedEvent>(OnAbsorbentSolutionChange);
    }

    private void OnActivateInWorld(Entity<AbsorbentComponent> ent, ref UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        Mop(ent, args.User, args.Target);
        args.Handled = true;
    }

    private void OnAfterInteract(Entity<AbsorbentComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target is not { } target)
            return;

        Mop(ent, args.User, target);
        args.Handled = true;
    }

    private void OnAbsorbentSolutionChange(Entity<AbsorbentComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (!SolutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionName, out _, out var solution))
            return;

        ent.Comp.Progress.Clear();

        var absorbentReagents = Puddle.GetAbsorbentReagents(solution);
        var mopReagent = solution.GetTotalPrototypeQuantity(absorbentReagents);
        if (mopReagent > FixedPoint2.Zero)
            ent.Comp.Progress[solution.GetColorWithOnly(_proto, absorbentReagents)] = mopReagent.Float();

        var otherColor = solution.GetColorWithout(_proto, absorbentReagents);
        var other = solution.Volume - mopReagent;
        if (other > FixedPoint2.Zero)
            ent.Comp.Progress[otherColor] = other.Float();

        if (solution.AvailableVolume > FixedPoint2.Zero)
            ent.Comp.Progress[Color.DarkGray] = solution.AvailableVolume.Float();

        Dirty(ent);
        _item.VisualsChanged(ent);
    }

    [Obsolete("Use Entity<T> variant")]
    public void Mop(EntityUid user, EntityUid target, EntityUid used, AbsorbentComponent component)
    {
        Mop((used, component), user, target);
    }

    public void Mop(Entity<AbsorbentComponent> absorbEnt, EntityUid user, EntityUid target)
    {
        if (!SolutionContainer.TryGetSolution(absorbEnt.Owner, absorbEnt.Comp.SolutionName, out var absorberSoln))
            return;

        // Use the non-optional form of IsDelayed to safe the TryComp in Mop
        if (TryComp<UseDelayComponent>(absorbEnt, out var useDelay)
            && _useDelay.IsDelayed((absorbEnt.Owner, useDelay)))
            return;

        // Try to slurp up the puddle.
        // We're then done if our mop doesn't use absorber solutions, since those don't need refilling.
        if (TryPuddleInteract((absorbEnt.Owner, absorbEnt.Comp, useDelay), absorberSoln.Value, user, target)
            || !absorbEnt.Comp.UseAbsorberSolution)
            return;

        // If it's refillable try to transfer
        TryRefillableInteract((absorbEnt.Owner, absorbEnt.Comp, useDelay), absorberSoln.Value, user, target);
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a refillable.
    /// </summary>
    private bool TryRefillableInteract(Entity<AbsorbentComponent, UseDelayComponent?> absorbEnt,
        Entity<SolutionComponent> absorbentSoln,
        EntityUid user,
        EntityUid target)
    {
        if (!TryComp<RefillableSolutionComponent>(target, out var refillable))
            return false;

        if (!SolutionContainer.TryGetRefillableSolution((target, refillable, null),
                out var refillableSoln,
                out var refillableSolution))
            return false;

        if (refillableSolution.Volume <= 0)
        {
            // Target empty - only transfer absorbent contents into refillable
            if (!TryTransferFromAbsorbentToRefillable(absorbEnt, absorbentSoln, refillableSoln.Value, user, target))
                return false;
        }
        else
        {
            // Target non-empty - do a two-way transfer
            if (!TryTwoWayAbsorbentRefillableTransfer(absorbEnt, absorbentSoln, refillableSoln.Value, user, target))
                return false;
        }

        var (used, absorber, useDelay) = absorbEnt;
        _audio.PlayPredicted(absorber.TransferSound, target, user);

        if (useDelay != null)
            _useDelay.TryResetDelay((used, useDelay));

        return true;
    }

    /// <summary>
    ///     Logic for an transferring solution from absorber to an empty refillable.
    /// </summary>
    private bool TryTransferFromAbsorbentToRefillable(Entity<AbsorbentComponent> absorbEnt,
        Entity<SolutionComponent> absorbentSoln,
        Entity<SolutionComponent> refillableSoln,
        EntityUid user,
        EntityUid target)
    {
        var absorbentSolution = absorbentSoln.Comp.Solution;
        if (absorbentSolution.Volume <= 0)
        {
            _popups.PopupClient(Loc.GetString("mopping-system-target-container-empty", ("target", target)), user, user);
            return false;
        }

        var refillableSolution = refillableSoln.Comp.Solution;
        var transferAmount = absorbEnt.Comp.PickupAmount < refillableSolution.AvailableVolume
            ? absorbEnt.Comp.PickupAmount
            : refillableSolution.AvailableVolume;

        if (transferAmount <= 0)
        {
            _popups.PopupClient(Loc.GetString("mopping-system-full", ("used", absorbEnt)), absorbEnt, user);
            return false;
        }

        // Prioritize transferring non-evaporatives if absorbent has any
        var contaminants = SolutionContainer.SplitSolutionWithout(absorbentSoln,
            transferAmount,
            Puddle.GetAbsorbentReagents(absorbentSoln.Comp.Solution));

        SolutionContainer.TryAddSolution(refillableSoln,
            contaminants.Volume > 0
                ? contaminants
                : SolutionContainer.SplitSolution(absorbentSoln, transferAmount));

        return true;
    }

    /// <summary>
    ///     Logic for an transferring contaminants to a non-empty refillable & reabsorbing water if any available.
    /// </summary>
    private bool TryTwoWayAbsorbentRefillableTransfer(Entity<AbsorbentComponent> absorbEnt,
        Entity<SolutionComponent> absorbentSoln,
        Entity<SolutionComponent> refillableSoln,
        EntityUid user,
        EntityUid target)
    {
        var contaminantsFromAbsorbent = SolutionContainer.SplitSolutionWithout(absorbentSoln,
            absorbEnt.Comp.PickupAmount,
            Puddle.GetAbsorbentReagents(absorbentSoln.Comp.Solution));

        var absorbentSolution = absorbentSoln.Comp.Solution;
        if (contaminantsFromAbsorbent.Volume == FixedPoint2.Zero
            && absorbentSolution.AvailableVolume == FixedPoint2.Zero)
        {
            // Nothing to transfer to refillable and no room to absorb anything extra
            _popups.PopupClient(Loc.GetString("mopping-system-puddle-space", ("used", absorbEnt)), user, user);

            // We can return cleanly because nothing was split from absorbent solution
            return false;
        }

        var waterPulled = absorbEnt.Comp.PickupAmount < absorbentSolution.AvailableVolume
            ? absorbEnt.Comp.PickupAmount
            : absorbentSolution.AvailableVolume;

        var refillableSolution = refillableSoln.Comp.Solution;
        var waterFromRefillable =
            refillableSolution.SplitSolutionWithOnly(waterPulled,
                Puddle.GetAbsorbentReagents(refillableSoln.Comp.Solution));
        SolutionContainer.UpdateChemicals(refillableSoln);

        if (waterFromRefillable.Volume == FixedPoint2.Zero && contaminantsFromAbsorbent.Volume == FixedPoint2.Zero)
        {
            // Nothing to transfer in either direction
            _popups.PopupClient(Loc.GetString("mopping-system-target-container-empty-water", ("target", target)),
                user,
                user);

            // We can return cleanly because nothing was split from refillable solution
            return false;
        }

        var anyTransferOccurred = false;

        if (waterFromRefillable.Volume > FixedPoint2.Zero)
        {
            // transfer water to absorbent
            SolutionContainer.TryAddSolution(absorbentSoln, waterFromRefillable);
            anyTransferOccurred = true;
        }

        if (contaminantsFromAbsorbent.Volume <= 0)
            return anyTransferOccurred;

        if (refillableSolution.AvailableVolume <= 0)
        {
            _popups.PopupClient(Loc.GetString("mopping-system-full", ("used", target)), user, user);
        }
        else
        {
            // transfer as much contaminants to refillable as will fit
            var contaminantsForRefillable = contaminantsFromAbsorbent.SplitSolution(refillableSolution.AvailableVolume);
            SolutionContainer.TryAddSolution(refillableSoln, contaminantsForRefillable);
            anyTransferOccurred = true;
        }

        // absorb everything that did not fit in the refillable back by the absorbent
        SolutionContainer.TryAddSolution(absorbentSoln, contaminantsFromAbsorbent);

        return anyTransferOccurred;
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a puddle.
    /// </summary>
    private bool TryPuddleInteract(Entity<AbsorbentComponent, UseDelayComponent?> absorbEnt,
        Entity<SolutionComponent> absorberSoln,
        EntityUid user,
        EntityUid target)
    {
        if (!TryComp<PuddleComponent>(target, out var puddle))
            return false;

        if (!SolutionContainer.ResolveSolution(target, puddle.SolutionName, ref puddle.Solution, out var puddleSolution)
            || puddleSolution.Volume <= 0)
            return false;

        var (_, absorber, useDelay) = absorbEnt;

        Solution puddleSplit;
        var isRemoved = false;
        if (absorber.UseAbsorberSolution)
        {
            // No reason to mop something that 1) can evaporate, 2) is an absorber, and 3) is being mopped with
            // something that uses absorbers.
            var puddleAbsorberVolume =
                puddleSolution.GetTotalPrototypeQuantity(Puddle.GetAbsorbentReagents(puddleSolution));
            if (puddleAbsorberVolume == puddleSolution.Volume)
            {
                _popups.PopupClient(Loc.GetString("mopping-system-puddle-already-mopped", ("target", target)),
                    target,
                    user);
                return true;
            }

            // Check if we have any evaporative reagents on our absorber to transfer
            var absorberSolution = absorberSoln.Comp.Solution;
            var available = absorberSolution.GetTotalPrototypeQuantity(Puddle.GetAbsorbentReagents(absorberSolution));

            // No material
            if (available == FixedPoint2.Zero)
            {
                _popups.PopupClient(Loc.GetString("mopping-system-no-water", ("used", absorbEnt)), absorbEnt, user);
                return true;
            }

            var transferMax = absorber.PickupAmount;
            var transferAmount = available > transferMax ? transferMax : available;

            puddleSplit =
                puddleSolution.SplitSolutionWithout(transferAmount, Puddle.GetAbsorbentReagents(puddleSolution));
            var absorberSplit =
                absorberSolution.SplitSolutionWithOnly(puddleSplit.Volume,
                    Puddle.GetAbsorbentReagents(absorberSolution));

            // Do tile reactions first
            var targetXform = Transform(target);
            var gridUid = targetXform.GridUid;
            if (TryComp<MapGridComponent>(gridUid, out var mapGrid))
            {
                var tileRef = _mapSystem.GetTileRef(gridUid.Value, mapGrid, targetXform.Coordinates);
                Puddle.DoTileReactions(tileRef, absorberSplit);
            }
            SolutionContainer.AddSolution(puddle.Solution.Value, absorberSplit);
        }
        else
        {
            // Note: arguably shouldn't this get all solutions?
            puddleSplit = puddleSolution.SplitSolutionWithout(absorber.PickupAmount, Puddle.GetAbsorbentReagents(puddleSolution));
            // Despawn if we're done
            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                // Spawn a *sparkle*
                PredictedSpawnAttachedTo(absorber.MoppedEffect, Transform(target).Coordinates);
                PredictedQueueDel(target);
                isRemoved = true;
            }
        }

        SolutionContainer.AddSolution(absorberSoln, puddleSplit);

        _audio.PlayPredicted(absorber.PickupSound, isRemoved ? absorbEnt : target, user);

        if (useDelay != null)
            _useDelay.TryResetDelay((absorbEnt, useDelay));

        var userXform = Transform(user);
        var targetPos = _transform.GetWorldPosition(target);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);

        _melee.DoLunge(user, absorbEnt, Angle.Zero, localPos, null);

        return true;
    }
}
