using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids.EntitySystems;

/// <summary>
/// Handles drag and drop of various solutions.
/// </summary>
/// <remarks>
/// The thing dragged always "gives" its reagents away for consistent UX.
/// </remarks>
/// <seealso cref="DumpableSolutionComponent" />
/// <seealso cref="DrainableSolutionComponent" />
public sealed class SolutionDumpingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solContainer = default!;

    private EntityQuery<DumpableSolutionComponent> _dumpQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrainableSolutionComponent, CanDragEvent>(OnDrainableCanDrag);
        SubscribeLocalEvent<DrainableSolutionComponent, CanDropDraggedEvent>(OnDrainableCanDragDropped);
        SubscribeLocalEvent<DrainableSolutionComponent, DragDropDraggedEvent>(OnDrainableDragged);

        SubscribeLocalEvent<DumpableSolutionComponent, DrainedTargetEvent>(OnDrainedToDumpableDragged);

        // We use queries for these since CanDropDraggedEvent gets called pretty rapidly
        _dumpQuery = GetEntityQuery<DumpableSolutionComponent>();
    }

    private void OnDrainableCanDrag(Entity<DrainableSolutionComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnDrainableCanDragDropped(Entity<DrainableSolutionComponent> ent, ref CanDropDraggedEvent args)
    {
        // Easily drawn-from thing can be dragged onto easily refillable thing.
        if (!_dumpQuery.HasComp(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    /// <summary>
    /// For when you are pouring something out from the container.
    /// </summary>
    private void OnDrainableDragged(Entity<DrainableSolutionComponent> sourceContainer, ref DragDropDraggedEvent args)
    {
        // Raising an event to be able to drain into various kind of fillable components.
        var ev = new DrainedTargetEvent(args.User, sourceContainer, sourceContainer.Comp.Solution);
        RaiseLocalEvent(args.Target, ref ev);
    }

    // Note: I feel that DumpableSolutionComponent is kind of redundant and only used to support unlimited containers,
    // and even then that should probably be refactored out (see to-do below).
    // It might be worth having the distinction if we want to separate "dump all" vs "pour some" functionalities,
    // but then we probably want to do a proper pass on how RefillableSolutionComponent is handled.
    private void OnDrainedToDumpableDragged(Entity<DumpableSolutionComponent> ent, ref DrainedTargetEvent args)
    {
        if (!_solContainer.TryGetDumpableSolution((ent, ent.Comp),
                out var targetSolEnt,
                out var targetSol))
            return;

        // Check openness, hands, source being empty, and target being full.
        if (!DragInteractionChecks(args.User,
                args.Source,
                ent.Owner,
                args.SourceSolution,
                targetSol,
                out var sourceEnt,
                !ent.Comp.Unlimited))
            return;

        if (ent.Comp.Unlimited)
        {
            // Unlimited means we're dumping into an infinite buffer, so we
            // have to be careful that we don't trigger any reactions. This
            // means SolutionContainerSystem.AddSolution can't be used!
            // TODO: This should be replaced with proper support for unlimited solutions, rather than cheating by bypassing UpdateChemicals using AddSolution. We can already avoid reactions using CanReact = false, this cheat just bypasses solution overflow.
            targetSol.AddSolution(
                _solContainer.SplitSolution(sourceEnt.Value, sourceEnt.Value.Comp.Solution.Volume),
                _protoMan);
            // Solution.AddSolution doesn't dirty targetSol for us
            Dirty(targetSolEnt.Value);
        }
        else
        {
            _solContainer.TryAddSolution(targetSolEnt.Value,
                _solContainer.SplitSolution(sourceEnt.Value, targetSol.AvailableVolume));
        }

        _audio.PlayPredicted(AbsorbentComponent.DefaultTransferSound, ent, args.User);
    }

    // Common checks between dragging handlers.
    private bool DragInteractionChecks(EntityUid user,
        EntityUid sourceContainer,
        EntityUid targetContainer,
        string sourceSolutionName,
        Solution targetSol,
        [NotNullWhen(true)] out Entity<SolutionComponent>? sourceSolEnt,
        bool checkAvailableVolume = true)
    {
        sourceSolEnt = null;
        if (!_actionBlocker.CanComplexInteract(user))
        {
            _popup.PopupClient(Loc.GetString("mopping-system-no-hands"), user, user);
            return false;
        }

        if (!_solContainer.TryGetSolution(sourceContainer, sourceSolutionName, out sourceSolEnt)
            || sourceSolEnt.Value.Comp.Solution.Volume == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("mopping-system-empty", ("used", sourceContainer)),
                sourceContainer,
                user);
            return false;
        }

        if (checkAvailableVolume && targetSol.AvailableVolume == FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("mopping-system-full", ("used", targetContainer)), targetContainer, user);
            return false;
        }

        // Both things need to be open. If the entity has nothing to close, it will count as "open".
        return !_openable.IsClosed(sourceContainer, user, predicted: true)
               && !_openable.IsClosed(targetContainer, user, predicted: true);
    }
}

/// <summary>
/// Raised directed on a target being drained into.
/// </summary>
[ByRefEvent]
public record struct DrainedTargetEvent(EntityUid User, EntityUid Source, string SourceSolution)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Source = Source;
    public readonly string SourceSolution = SourceSolution;
    public bool Handled = false;
}
