using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Events;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Systems;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Effects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Friction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly SharedSolutionSystem SolutionSystem = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfterSystem = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly ITileDefinitionManager TileDefManager = default!;
    [Dependency] protected readonly OpenableSystem Openable = default!;
    [Dependency] protected readonly SharedMapSystem MapSystem = default!;
    [Dependency] protected readonly EntityLookupSystem EntityLookup = default!;
    [Dependency] protected readonly SharedChemistryRegistrySystem ChemRegistry = default!;
    [Dependency] protected readonly ReactiveSystem ReactiveSystem = default!;
    [Dependency] protected readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] protected readonly IRobustRandom RobustRandom = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] protected readonly SharedColorFlashEffectSystem ColorFlashSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem AppearanceSystem = default!;
    [Dependency] protected readonly SpeedModifierContactsSystem SpeedContactSystem = default!;
    [Dependency] protected readonly StepTriggerSystem StepTriggerSystem = default!;
    [Dependency] protected readonly TileFrictionController TileFrictionController = default!;

    public const string PuddleSolutionId = "PuddleContents";

    /// <summary>
    /// The lowest threshold to be considered for puddle sprite states as well as slipperiness of a puddle.
    /// </summary>
    protected const float LowThreshold = 0.3f;
    protected const float MediumThreshold = 0.6f;
    protected const float MinSplashPercentage = 0.05f;
    protected const float MaxSplashPercentage  = 0.3f;
    protected const float SlipReactChance = 0.5f;

    protected const float StandoutReagentDucking = 0.3f;
    protected const float PuddleVolumeCap = 1000;

    protected EntityQuery<StandoutReagentComponent> _standoutQuery;
    protected EntityQuery<AppearanceComponent> _appearance;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RefillableSolutionComponent, CanDragEvent>(OnRefillableCanDrag);
        SubscribeLocalEvent<DumpableSolutionComponent, CanDropTargetEvent>(OnDumpCanDropTarget);
        SubscribeLocalEvent<DrainableSolutionComponent, CanDropTargetEvent>(OnDrainCanDropTarget);
        SubscribeLocalEvent<RefillableSolutionComponent, CanDropDraggedEvent>(OnRefillableCanDropDragged);
        SubscribeLocalEvent<PuddleComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
        SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
        SubscribeLocalEvent<PuddleComponent, SlipEvent>(OnPuddleSlip);
        SubscribeLocalEvent<PuddleComponent, SolutionUpdatedEvent>(OnSolutionUpdated);
        SubscribeLocalEvent<PuddleComponent, SolutionAddedEvent>(OnSolutionAdded);
        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<PuddleComponent, AnchorStateChangedEvent>(OnAnchorChanged);

        _standoutQuery = EntityManager.GetEntityQuery<StandoutReagentComponent>();
        _appearance = EntityManager.GetEntityQuery<AppearanceComponent>();
        InitializeSpillable();
    }

    protected void OnPuddleInit(Entity<PuddleComponent> puddle, ref ComponentInit args)
    {
        SolutionSystem.TryEnsureSolution(puddle.Owner, PuddleSolutionId, out _, FixedPoint2.New(PuddleVolumeCap));
    }

    private void OnSolutionAdded(Entity<PuddleComponent> ent, ref SolutionAddedEvent args)
    {
        if (ent.Comp.Solution.Owner != EntityUid.Invalid)
        {
            Log.Error($"Puddle has multiple solutions, this should never happen!");
            return;
        }
        ent.Comp.Solution = args.NewSolution;
    }

    private void OnSolutionUpdated(Entity<PuddleComponent> puddle, ref SolutionUpdatedEvent args)
    {
        if (args.Solution.Comp.Name != PuddleSolutionId)
            return;
        // if (args.Solution.Comp.Volume > 0 && EntityManager.IsQueuedForDeletion(ent))
        // {
        //     //TODO: DeQueue deletion when that gets implemented
        // }
        UpdateOverflow(puddle);
        UpdateSlip(puddle);
        UpdateSlow(puddle);
        UpdateAppearance((puddle,puddle.Comp,null));
        if (args.Solution.Comp.Volume == 0)
            QueueDel(puddle);
    }

    private void OnAnchorChanged(Entity<PuddleComponent> entity, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            QueueDel(entity);
    }

    public bool WillOverflow(Entity<PuddleComponent> puddle, FixedPoint2 quantityToAdd, out FixedPoint2 overflow)
    {
        overflow = FixedPoint2.Max(puddle.Comp.Solution.Comp.Volume + quantityToAdd - PuddleVolumeCap, 0);
        return overflow > 0;
    }

    public bool IsOverflowing(Entity<PuddleComponent> puddle)
    {
        return puddle.Comp.Solution.Comp.Volume > PuddleVolumeCap;
    }

    public bool IsOverflowing(Entity<PuddleComponent> puddle, out FixedPoint2 overflow)
    {
        overflow = FixedPoint2.Max(puddle.Comp.Solution.Comp.Volume - PuddleVolumeCap, 0);
        return overflow > 0;
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
        var reagentQuantity = SolutionSystem.GetPrimaryReagent(entity.Comp.Solution);
        if (reagentQuantity == null)
            return;
        args.Sound = reagentQuantity.Value.Entity.Comp.FootstepSound;
    }

    private void HandlePuddleExamined(Entity<PuddleComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PuddleComponent)))
        {
            if (TryComp<StepTriggerComponent>(entity, out var slippery) && slippery.Active)
            {
                args.PushMarkup(Loc.GetString("puddle-component-examine-is-slippery-text"));
            }

            if (HasComp<EvaporationComponent>(entity))
            {
                if (CanFullyEvaporate(entity.Comp.Solution))
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating"));
                else if (SolutionSystem.GetTotalQuantity(entity.Comp.Solution, EvaporationReagents) > FixedPoint2.Zero)
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-partial"));
                else
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
            }
            else
                args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating-no"));
        }
    }

    private void OnPuddleSlip(Entity<PuddleComponent> entity, ref SlipEvent args)
    {
        // Reactive entities have a chance to get a touch reaction from slipping on a puddle
        // (i.e. it is implied they fell face first onto it or something)
        if (!HasComp<ReactiveComponent>(args.Slipped)
            || HasComp<SlidingComponent>(args.Slipped))
            return;

        if (!SolutionHolderQuery.TryComp(entity, out var solutionHolderComp))
            return;//this should never happen, but we should check for it anyway


        // Eventually probably have some system of 'body coverage' to tweak the probability but for now just 0.5
        // (implying that spacemen have a 50% chance to either land on their ass or their face)
        if (!RobustRandom.Prob(SlipReactChance))
            return;

        if (!SolutionSystem.ResolveSolution(entity.Owner, PuddleSolutionId, ref entity.Comp.Solution))
            return;

        PopupSystem.PopupEntity(Loc.GetString("puddle-component-slipped-touch-reaction",
                ("puddle", entity.Owner)),
            args.Slipped,
            args.Slipped,
            PopupType.SmallCaution);

        // Take 15% of the puddle solution
        var splitSol = SolutionSystem.SplitSolution(entity.Comp.Solution,
            entity.Comp.Solution.Comp.Volume * 0.15f);
        ReactiveSystem.DoEntityReaction(args.Slipped, splitSol, ReactionMethod.Touch);
    }

    private void UpdateAppearance(Entity<PuddleComponent, AppearanceComponent?> puddle)
    {
        if (!Resolve(puddle, ref puddle.Comp2, false))
        {
            return;
        }
        var volume = puddle.Comp1.Solution.Comp.Volume / puddle.Comp1.OverflowVolume;
            // Make blood stand out more
            // Kinda EH
            // Could potentially do alpha per-solution but future problem.
        var color = SolutionSystem.GetSolutionColor(puddle.Comp1.Solution, StandoutReagentDucking);
        AppearanceSystem.SetData(puddle, PuddleVisuals.CurrentVolume, volume.Float(), puddle.Comp2);
        AppearanceSystem.SetData(puddle, PuddleVisuals.SolutionColor, color, puddle.Comp2);
    }

    private void UpdateSlow(Entity<PuddleComponent> puddle)
    {
        var maxViscosity = 0f;
        foreach (var (reagent, _) in SolutionSystem.EnumerateReagents(puddle.Comp.Solution))
        {
            maxViscosity = Math.Max(maxViscosity, reagent.Entity.Comp.Viscosity);
        }

        if (maxViscosity > 0)
        {
            var comp = EnsureComp<SpeedModifierContactsComponent>(puddle);
            var speed = 1 - maxViscosity;
            SpeedContactSystem.ChangeModifiers(puddle, speed, comp);
        }
        else
        {
            RemComp<SpeedModifierContactsComponent>(puddle);
        }
    }

    private void UpdateSlip(Entity<PuddleComponent> puddle)
    {
        var isSlippery = false;
        // The base sprite is currently at 0.3 so we require at least 2nd tier to be slippery or else it's too hard to see.
        var amountRequired = FixedPoint2.New(puddle.Comp.OverflowVolume.Float() * LowThreshold);
        var slipperyAmount = FixedPoint2.Zero;

        foreach (var (reagent, quantity) in SolutionSystem.EnumerateReagents(puddle.Comp.Solution))
        {
            if (!reagent.Entity.Comp.Slippery)
                continue;
            slipperyAmount += quantity;
            if (slipperyAmount <= amountRequired)
                continue;
            isSlippery = true;
            break;
        }

        if (isSlippery)
        {
            var comp = EnsureComp<StepTriggerComponent>(puddle);
            StepTriggerSystem.SetActive(puddle, true, comp);
            var friction = EnsureComp<TileFrictionModifierComponent>(puddle);
            TileFrictionController.SetModifier(puddle, TileFrictionController.DefaultFriction * 0.5f, friction);
        }
        else if (TryComp<StepTriggerComponent>(puddle, out var comp))
        {
            StepTriggerSystem.SetActive(puddle, false, comp);
            RemCompDeferred<TileFrictionModifierComponent>(puddle);
        }
    }

    protected virtual bool UpdateOverflow(Entity<PuddleComponent> puddle)
    {
        if (!IsOverflowing(puddle, out var overflow))
            return false;
        //predict the redistribution
        SolutionSystem.RemoveReagents(puddle.Comp.Solution, overflow);
        return true;
    }
}
