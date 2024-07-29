using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    /// <summary>
    /// The lowest threshold to be considered for puddle sprite states as well as slipperiness of a puddle.
    /// </summary>
    public const float LowThreshold = 0.3f;

    public const float MediumThreshold = 0.6f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RefillableSolutionComponent, CanDragEvent>(OnRefillableCanDrag);
        SubscribeLocalEvent<DumpableSolutionComponent, CanDropTargetEvent>(OnDumpCanDropTarget);
        SubscribeLocalEvent<DrainableSolutionComponent, CanDropTargetEvent>(OnDrainCanDropTarget);
        SubscribeLocalEvent<RefillableSolutionComponent, CanDropDraggedEvent>(OnRefillableCanDropDragged);
        SubscribeLocalEvent<PuddleComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
        SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);

        InitializeSpillable();
    }

    private void OnRefillableCanDrag(Entity<RefillableSolutionComponent> entity, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnDumpCanDropTarget(Entity<DumpableSolutionComponent> entity, ref CanDropTargetEvent args)
    {
        if (HasComp<DrainableSolutionComponent>(args.Dragged))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnDrainCanDropTarget(Entity<DrainableSolutionComponent> entity, ref CanDropTargetEvent args)
    {
        if (HasComp<RefillableSolutionComponent>(args.Dragged))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    private void OnRefillableCanDropDragged(Entity<RefillableSolutionComponent> entity, ref CanDropDraggedEvent args)
    {
        if (!HasComp<DrainableSolutionComponent>(args.Target) && !HasComp<DumpableSolutionComponent>(args.Target))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnGetFootstepSound(Entity<PuddleComponent> entity, ref GetFootstepSoundEvent args)
    {
        if (!_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution,
                out var solution))
            return;

        var reagentId = solution.GetPrimaryReagentId();
        if (!string.IsNullOrWhiteSpace(reagentId?.Prototype)
            && _prototypeManager.TryIndex(reagentId.Value.Prototype, out ReagentPrototype? proto))
        {
            args.Sound = proto.FootstepSound;
        }
    }

    private void HandlePuddleExamined(Entity<PuddleComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PuddleComponent)))
        {
            if (TryComp<StepTriggerComponent>(entity, out var slippery) && slippery.Active)
            {
                args.PushMarkup(Loc.GetString("puddle-component-examine-is-slippery-text"));
            }

            if (HasComp<EvaporationComponent>(entity) &&
                _solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName,
                    ref entity.Comp.Solution, out var solution))
            {
                if (CanFullyEvaporate(solution))
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating"));
                else if (solution.GetTotalPrototypeQuantity(EvaporationReagents) > FixedPoint2.Zero)
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-partial"));
                else
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
            }
            else
                args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
        }
    }

    #region Spill
    // These methods are in Shared to make it easier to interact with PuddleSystem in Shared code.
    // Note that they always fail when run on the client, not creating a puddle and returning false.
    // Adding proper prediction to this system would require spawning temporary puddle entities on the
    // client and replacing or merging them with the ones spawned by the server when the client goes to
    // replicate those, and I am not enough of a wizard to attempt implementing that.

    /// <summary>
    ///     First splashes reagent on reactive entities near the spilling entity, then spills the rest regularly to a
    ///     puddle. This is intended for 'destructive' spills, like when entities are destroyed or thrown.
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySplashSpillAt(EntityUid uid,
        EntityCoordinates coordinates,
        Solution solution,
        out EntityUid puddleUid,
        bool sound = true,
        EntityUid? user = null);

    /// <summary>
    ///     Spills solution at the specified coordinates.
    /// Will add to an existing puddle if present or create a new one if not.
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(EntityCoordinates coordinates, Solution solution, out EntityUid puddleUid, bool sound = true);

    /// <summary>
    /// <see cref="TrySpillAt(EntityCoordinates, Solution, out EntityUid, bool)"/>
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(EntityUid uid, Solution solution, out EntityUid puddleUid, bool sound = true,
        TransformComponent? transformComponent = null);

    /// <summary>
    /// <see cref="TrySpillAt(EntityCoordinates, Solution, out EntityUid, bool)"/>
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(TileRef tileRef, Solution solution, out EntityUid puddleUid, bool sound = true,
        bool tileReact = true);

    #endregion Spill
}
