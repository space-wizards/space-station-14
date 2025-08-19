using System.Linq;
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
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;

    private string[] _standoutReagents = [];

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

        SubscribeLocalEvent<PuddleComponent, SolutionContainerChangedEvent>(OnSolutionUpdate);
        SubscribeLocalEvent<PuddleComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
        SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
        SubscribeLocalEvent<PuddleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        CacheStandsout();
        InitializeSpillable();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<ReagentPrototype>())
            CacheStandsout();
    }

    /// <summary>
    /// Used to cache standout reagents for future use.
    /// </summary>
    private void CacheStandsout()
    {
        _standoutReagents = [.. _prototypeManager.EnumeratePrototypes<ReagentPrototype>().Where(x => x.Standsout).Select(x => x.ID)];
    }

    protected virtual void OnSolutionUpdate(Entity<PuddleComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != entity.Comp.SolutionName)
            return;

        UpdateAppearance((entity, entity.Comp));
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
                else if (solution.GetTotalPrototypeQuantity(GetEvaporatingReagents(solution)) > FixedPoint2.Zero)
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-partial"));
                else
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
            }
            else
                args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
        }
    }

    // Workaround for https://github.com/space-wizards/space-station-14/pull/35314
    private void OnEntRemoved(Entity<PuddleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution and clear our cached reference
        if (args.Entity == ent.Comp.Solution?.Owner)
            ent.Comp.Solution = null;
    }

    private void UpdateAppearance(Entity<PuddleComponent?, AppearanceComponent?> ent)
    {
        var (uid, puddle, appearance) = ent;
        if (!Resolve(ent, ref puddle, ref appearance))
            return;

        var volume = FixedPoint2.Zero;
        var color = Color.White;

        if (_solutionContainerSystem.ResolveSolution(uid,
                puddle.SolutionName,
                ref puddle.Solution,
                out var solution))
        {
            volume = solution.Volume / puddle.OverflowVolume;

            // Make blood stand out more
            // Kinda EH
            // Could potentially do alpha per-solution but future problem.

            color = solution.GetColorWithout(_prototypeManager, _standoutReagents);
            color = color.WithAlpha(0.7f);

            foreach (var standout in _standoutReagents)
            {
                var quantity = solution.GetTotalPrototypeQuantity(standout);
                if (quantity <= FixedPoint2.Zero)
                    continue;

                var interpolateValue = quantity.Float() / solution.Volume.Float();
                color = Color.InterpolateBetween(color,
                    _prototypeManager.Index<ReagentPrototype>(standout).SubstanceColor,
                    interpolateValue);
            }
        }

        _appearance.SetData(ent, PuddleVisuals.CurrentVolume, volume.Float(), appearance);
        _appearance.SetData(ent, PuddleVisuals.SolutionColor, color, appearance);
    }

    public void DoTileReactions(TileRef tileRef, Solution solution)
    {
        for (var i = solution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagent, quantity) = solution.Contents[i];
            var proto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);
            var removed = proto.ReactionTile(tileRef, quantity, EntityManager, reagent.Data);
            if (removed <= FixedPoint2.Zero)
                continue;

            solution.RemoveReagent(reagent, removed);
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
