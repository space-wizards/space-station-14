using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids;

/// <summary>
/// Mopping logic for interacting with puddle components.
/// </summary>
public abstract class SharedAbsorbentSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] protected readonly SharedPuddleSystem Puddle = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private static readonly EntProtoId Sparkles = "PuddleSparkle";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AbsorbentComponent, UserActivateInWorldEvent>(OnActivateInWorld);
    }

    private void OnActivateInWorld(EntityUid uid, AbsorbentComponent component, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        Mop(uid, args.Target, uid, component);
        args.Handled = true;
    }

    private void OnAfterInteract(EntityUid uid, AbsorbentComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target == null)
            return;

        Mop(args.User, args.Target.Value, args.Used, component);
        args.Handled = true;
    }

    public void Mop(EntityUid user, EntityUid target, EntityUid used, AbsorbentComponent component)
    {
        if (!SolutionContainer.TryGetSolution(used, component.SolutionName, out var absorberSoln))
            return;

        if (TryComp<UseDelayComponent>(used, out var useDelay)
            && _useDelay.IsDelayed((used, useDelay)))
            return;

        // If it's a puddle try to grab from
        if (!TryPuddleInteract(user, used, target, component, useDelay, absorberSoln.Value) && component.UseAbsorberSolution)
        {
            // If it's refillable try to transfer
            if (!TryRefillableInteract(user, used, target, component, useDelay, absorberSoln.Value))
                return;
        }
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a refillable.
    /// </summary>
    private bool TryRefillableInteract(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, UseDelayComponent? useDelay, Entity<SolutionComponent> absorbentSoln)
    {
        if (!TryComp(target, out RefillableSolutionComponent? refillable))
            return false;

        if (!SolutionContainer.TryGetRefillableSolution((target, refillable, null), out var refillableSoln, out var refillableSolution))
            return false;

        if (refillableSolution.Volume <= 0)
        {
            // Target empty - only transfer absorbent contents into refillable
            if (!TryTransferFromAbsorbentToRefillable(user, used, target, component, absorbentSoln, refillableSoln.Value))
                return false;
        }
        else
        {
            // Target non-empty - do a two-way transfer
            if (!TryTwoWayAbsorbentRefillableTransfer(user, used, target, component, absorbentSoln, refillableSoln.Value))
                return false;
        }

        _audio.PlayPvs(component.TransferSound, target);
        if (useDelay != null)
            _useDelay.TryResetDelay((used, useDelay));
        return true;
    }

    /// <summary>
    ///     Logic for an transferring solution from absorber to an empty refillable.
    /// </summary>
    private bool TryTransferFromAbsorbentToRefillable(
        EntityUid user,
        EntityUid used,
        EntityUid target,
        AbsorbentComponent component,
        Entity<SolutionComponent> absorbentSoln,
        Entity<SolutionComponent> refillableSoln)
    {
        var absorbentSolution = absorbentSoln.Comp.Solution;
        if (absorbentSolution.Volume <= 0)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-target-container-empty", ("target", target)), user, user);
            return false;
        }

        var refillableSolution = refillableSoln.Comp.Solution;
        var transferAmount = component.PickupAmount < refillableSolution.AvailableVolume ?
            component.PickupAmount :
            refillableSolution.AvailableVolume;

        if (transferAmount <= 0)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-full", ("used", used)), used, user);
            return false;
        }

        // Prioritize transferring non-evaporatives if absorbent has any
        var contaminants = SolutionContainer.SplitSolutionWithout(absorbentSoln, transferAmount, Puddle.GetAbsorbentReagents(absorbentSoln.Comp.Solution));
        if (contaminants.Volume > 0)
        {
            SolutionContainer.TryAddSolution(refillableSoln, contaminants);
        }
        else
        {
            var evaporatives = SolutionContainer.SplitSolution(absorbentSoln, transferAmount);
            SolutionContainer.TryAddSolution(refillableSoln, evaporatives);
        }

        return true;
    }

    /// <summary>
    ///     Logic for an transferring contaminants to a non-empty refillable & reabsorbing water if any available.
    /// </summary>
    private bool TryTwoWayAbsorbentRefillableTransfer(
        EntityUid user,
        EntityUid used,
        EntityUid target,
        AbsorbentComponent component,
        Entity<SolutionComponent> absorbentSoln,
        Entity<SolutionComponent> refillableSoln)
    {
        var contaminantsFromAbsorbent = SolutionContainer.SplitSolutionWithout(absorbentSoln, component.PickupAmount, Puddle.GetAbsorbentReagents(absorbentSoln.Comp.Solution));

        var absorbentSolution = absorbentSoln.Comp.Solution;
        if (contaminantsFromAbsorbent.Volume == FixedPoint2.Zero && absorbentSolution.AvailableVolume == FixedPoint2.Zero)
        {
            // Nothing to transfer to refillable and no room to absorb anything extra
            _popups.PopupEntity(Loc.GetString("mopping-system-puddle-space", ("used", used)), user, user);

            // We can return cleanly because nothing was split from absorbent solution
            return false;
        }

        var waterPulled = component.PickupAmount < absorbentSolution.AvailableVolume ?
            component.PickupAmount :
            absorbentSolution.AvailableVolume;

        var refillableSolution = refillableSoln.Comp.Solution;
        var waterFromRefillable = refillableSolution.SplitSolutionWithOnly(waterPulled, Puddle.GetAbsorbentReagents(refillableSoln.Comp.Solution));
        SolutionContainer.UpdateChemicals(refillableSoln);

        if (waterFromRefillable.Volume == FixedPoint2.Zero && contaminantsFromAbsorbent.Volume == FixedPoint2.Zero)
        {
            // Nothing to transfer in either direction
            _popups.PopupEntity(Loc.GetString("mopping-system-target-container-empty-water", ("target", target)), user, user);

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

        if (contaminantsFromAbsorbent.Volume > 0)
        {
            if (refillableSolution.AvailableVolume <= 0)
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-full", ("used", target)), user, user);
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
        }

        return anyTransferOccurred;
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a puddle.
    /// </summary>
    private bool TryPuddleInteract(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent absorber, UseDelayComponent? useDelay, Entity<SolutionComponent> absorberSoln)
    {
        if (!TryComp(target, out PuddleComponent? puddle))
            return false;

        if (!SolutionContainer.ResolveSolution(target, puddle.SolutionName, ref puddle.Solution, out var puddleSolution) || puddleSolution.Volume <= 0)
            return false;

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
                _popups.PopupEntity(Loc.GetString("mopping-system-puddle-already-mopped", ("target", target)),
                    user,
                    user);
                return true;
            }

            // Check if we have any evaporative reagents on our absorber to transfer
            var absorberSolution = absorberSoln.Comp.Solution;
            var available = absorberSolution.GetTotalPrototypeQuantity(Puddle.GetAbsorbentReagents(absorberSolution));

            // No material
            if (available == FixedPoint2.Zero)
            {
                _popups.PopupEntity(Loc.GetString("mopping-system-no-water", ("used", used)), user, user);
                return true;
            }

            var transferMax = absorber.PickupAmount;
            var transferAmount = available > transferMax ? transferMax : available;

            puddleSplit = puddleSolution.SplitSolutionWithout(transferAmount, Puddle.GetAbsorbentReagents(puddleSolution));
            var absorberSplit = absorberSolution.SplitSolutionWithOnly(puddleSplit.Volume, Puddle.GetAbsorbentReagents(absorberSolution));

            // Do tile reactions first
            var transform = Transform(target);
            var gridUid = transform.GridUid;
            if (TryComp(gridUid, out MapGridComponent? mapGrid))
            {
                var tileRef = _mapSystem.GetTileRef(gridUid.Value, mapGrid, transform.Coordinates);
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
                Spawn(Sparkles, GetEntityQuery<TransformComponent>().GetComponent(target).Coordinates);
                QueueDel(target);
                isRemoved = true;
            }
        }

        SolutionContainer.AddSolution(absorberSoln, puddleSplit);

        _audio.PlayPvs(absorber.PickupSound, isRemoved ? used : target);
        if (useDelay != null)
            _useDelay.TryResetDelay((used, useDelay));

        var userXform = Transform(user);
        var targetPos = _transform.GetWorldPosition(target);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);

        _melee.DoLunge(user, used, Angle.Zero, localPos, null, false);

        return true;
    }

    [Serializable, NetSerializable]
    protected sealed class AbsorbentComponentState : ComponentState
    {
        public Dictionary<Color, float> Progress;

        public AbsorbentComponentState(Dictionary<Color, float> progress)
        {
            Progress = progress;
        }
    }
}
