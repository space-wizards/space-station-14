using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public sealed class MoppingSystem : SharedMoppingSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SpillableSystem _spillableSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly PopupSystem _popups = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    const string PuddlePrototypeId = "PuddleSmear"; // The puddle prototype to use when releasing liquid to the floor, making a new puddle

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AbsorbentComponent, ComponentInit>(OnAbsorbentInit);
        SubscribeLocalEvent<AbsorbentComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AbsorbentComponent, AbsorbantDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AbsorbentComponent, SolutionChangedEvent>(OnAbsorbentSolutionChange);
    }

    private void OnAbsorbentInit(EntityUid uid, AbsorbentComponent component, ComponentInit args)
    {
        // TODO: I know dirty on init but no prediction moment.
        UpdateAbsorbent(uid, component);
    }

    private void OnAbsorbentSolutionChange(EntityUid uid, AbsorbentComponent component, SolutionChangedEvent args)
    {
        UpdateAbsorbent(uid, component);
    }

    private void UpdateAbsorbent(EntityUid uid, AbsorbentComponent component)
    {
        if (!_solutionSystem.TryGetSolution(uid, AbsorbentComponent.SolutionName, out var solution))
            return;

        var oldProgress = component.Progress;

        component.Progress = (float) (solution.Volume / solution.MaxVolume);
        if (component.Progress.Equals(oldProgress))
            return;
        Dirty(component);
    }

    private void OnAfterInteract(EntityUid uid, AbsorbentComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled)
            return;

        if (!_solutionSystem.TryGetSolution(args.Used, AbsorbentComponent.SolutionName, out var absorberSoln))
            return;

        if (args.Target is not { Valid: true } target)
        {
            // Add liquid to an empty floor tile
            args.Handled = TryCreatePuddle(args.User, args.ClickLocation, component, absorberSoln);
            return;
        }

        args.Handled = TryPuddleInteract(args.User, uid, target, component, absorberSoln)
            || TryEmptyAbsorber(args.User, uid, target, component, absorberSoln)
            || TryFillAbsorber(args.User, uid, target, component, absorberSoln);
    }

    /// <summary>
    ///     Tries to create a puddle using solutions stored in the absorber entity.
    /// </summary>
    private bool TryCreatePuddle(EntityUid user, EntityCoordinates clickLocation, AbsorbentComponent absorbent, Solution absorberSoln)
    {
        if (absorberSoln.Volume <= 0)
            return false;

        if (!_mapManager.TryGetGrid(clickLocation.GetGridUid(EntityManager), out var mapGrid))
            return false;

        var releaseAmount = FixedPoint2.Min(absorbent.ResidueAmount, absorberSoln.Volume);
        var releasedSolution = _solutionSystem.SplitSolution(absorbent.Owner, absorberSoln, releaseAmount);
        _spillableSystem.SpillAt(mapGrid.GetTileRef(clickLocation), releasedSolution, PuddlePrototypeId);
        _popups.PopupEntity(Loc.GetString("mopping-system-release-to-floor"), user, user);
        return true;
    }

    /// <summary>
    ///     Attempt to fill an absorber from some drainable solution.
    /// </summary>
    private bool TryFillAbsorber(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, Solution absorberSoln)
    {
        if (absorberSoln.AvailableVolume <= 0 || !TryComp(target, out DrainableSolutionComponent? drainable))
            return false;

        if (!_solutionSystem.TryGetDrainableSolution(target, out var drainableSolution))
            return false;

        if (drainableSolution.Volume <= 0)
        {
            var msg = Loc.GetString("mopping-system-target-container-empty", ("target", target));
            _popups.PopupEntity(msg, user, user);
            return true;
        }

        // Let's transfer up to to half the tool's available capacity to the tool.
        var quantity = FixedPoint2.Max(component.PickupAmount, absorberSoln.AvailableVolume / 2);
        quantity = FixedPoint2.Min(quantity, drainableSolution.Volume);

        DoMopInteraction(user, used, target, component, drainable.Solution, quantity, 1, "mopping-system-drainable-success", component.TransferSound);
        return true;
    }

    /// <summary>
    ///     Empty an absorber into a refillable solution.
    /// </summary>
    private bool TryEmptyAbsorber(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, Solution absorberSoln)
    {
        if (absorberSoln.Volume <= 0 || !TryComp(target, out RefillableSolutionComponent? refillable))
            return false;

        if (!_solutionSystem.TryGetRefillableSolution(target, out var targetSolution))
            return false;

        string msg;
        if (targetSolution.AvailableVolume <= 0)
        {
            msg = Loc.GetString("mopping-system-target-container-full", ("target", target));
            _popups.PopupEntity(msg, user, user);
            return true;
        }

        // check if the target container is too small (e.g. syringe)
        // TODO this should really be a tag or something, not a capacity check.
        if (targetSolution.MaxVolume <= FixedPoint2.New(20))
        {
            msg = Loc.GetString("mopping-system-target-container-too-small", ("target", target));
            _popups.PopupEntity(msg, user, user);
            return true;
        }

        float delay;
        FixedPoint2 quantity = absorberSoln.Volume;

        // TODO this really needs cleaning up. Less magic numbers, more data-fields.

        if (_tagSystem.HasTag(used, "Mop") // if the tool used is a literal mop (and not a sponge, rag, etc.)
            && !_tagSystem.HasTag(target, "Wringer")) // and if the target does not have a wringer for properly drying the mop
        {
            delay = 5.0f; // Should take much longer if you don't have a wringer

            var frac = quantity / absorberSoln.MaxVolume;

            // squeeze up to 60% of the solution from the mop if the mop is more than one-quarter full
            if (frac > 0.25)
                quantity *= 0.6;

            if (frac > 0.5)
                msg = "mopping-system-hand-squeeze-still-wet";
            else if (frac > 0.5)
                msg = "mopping-system-hand-squeeze-little-wet";
            else
                msg = "mopping-system-hand-squeeze-dry";
        }
        else
        {
            msg = "mopping-system-refillable-success";
            delay = 1.0f;
        }

        // negative quantity as we are removing solutions from the mop
        quantity = -FixedPoint2.Min(targetSolution.AvailableVolume, quantity);

        DoMopInteraction(user, used, target, component, refillable.Solution, quantity, delay, msg, component.TransferSound);
        return true;
    }

    /// <summary>
    ///     Logic for an absorbing entity interacting with a puddle.
    /// </summary>
    private bool TryPuddleInteract(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent absorber, Solution absorberSoln)
    {
        if (!TryComp(target, out PuddleComponent? puddle))
            return false;

        if (!_solutionSystem.TryGetSolution(target, puddle.SolutionName, out var puddleSolution) || puddleSolution.Volume <= 0)
            return false;

        FixedPoint2 quantity;

        // Get lower limit for mopping
        FixedPoint2 lowerLimit = FixedPoint2.Zero;
        if (TryComp(target, out EvaporationComponent? evaporation)
            && evaporation.EvaporationToggle
            && evaporation.LowerLimit == 0)
        {
            lowerLimit = absorber.LowerLimit;
        }

        // Can our absorber even absorb any liquid?
        if (puddleSolution.Volume <= lowerLimit)
        {
            // Cannot absorb any more liquid. So clearly the user wants to add liquid to the puddle... right?
            // This is the old behavior and I CBF fixing this, for the record I don't like this.

            quantity = FixedPoint2.Min(absorber.ResidueAmount, absorberSoln.Volume);
            if (quantity <= 0)
                return false;

            // Dilutes the puddle with some solution from the tool
            _solutionSystem.TryTransferSolution(used, target, absorberSoln, puddleSolution, quantity);
            _audio.PlayPvs(absorber.TransferSound, used);
            _popups.PopupEntity(Loc.GetString("mopping-system-puddle-diluted"), user);
            return true;
        }

        if (absorberSoln.AvailableVolume < 0)
        {
            _popups.PopupEntity(Loc.GetString("mopping-system-tool-full", ("used", used)), user, user);
            return true;
        }

        quantity = FixedPoint2.Min(absorber.PickupAmount, puddleSolution.Volume - lowerLimit, absorberSoln.AvailableVolume);
        if (quantity <= 0)
            return false;

        var delay = absorber.PickupAmount.Float() / absorber.Speed;
        DoMopInteraction(user, used, target, absorber, puddle.SolutionName, quantity, delay, "mopping-system-puddle-success", absorber.PickupSound);
        return true;
    }

    private void DoMopInteraction(EntityUid user, EntityUid used, EntityUid target, AbsorbentComponent component, string targetSolution,
                                  FixedPoint2 transferAmount, float delay, string msg, SoundSpecifier sfx)
    {
        // Can't interact with too many entities at once.
        if (component.MaxInteractingEntities < component.InteractingEntities.Count + 1)
            return;

        // Can't interact with the same container multiple times at once.
        if (!component.InteractingEntities.Add(target))
            return;

        var ev = new AbsorbantDoAfterEvent(targetSolution, msg, sfx, transferAmount);

        var doAfterArgs = new DoAfterArgs(user, delay, ev, used, target: target, used: used)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.2f
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid uid, AbsorbentComponent component, AbsorbantDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        component.InteractingEntities.Remove(args.Target.Value);

        if (args.Cancelled || args.Handled)
            return;

        _audio.PlayPvs(args.Sound, uid);
        _popups.PopupEntity(Loc.GetString(args.Message, ("target", args.Target.Value), ("used", uid)), uid);
        _solutionSystem.TryTransferSolution(args.Target.Value, uid, args.TargetSolution,
            AbsorbentComponent.SolutionName, args.TransferAmount);
        component.InteractingEntities.Remove(args.Target.Value);

        args.Handled = true;
    }
}
