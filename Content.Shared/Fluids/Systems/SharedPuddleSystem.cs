using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Systems;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Effects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
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
    protected const float PuddleVolume = 1000;

    protected EntityQuery<StandoutReagentComponent> _standoutQuery;

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

        _standoutQuery = EntityManager.GetEntityQuery<StandoutReagentComponent>();
        InitializeSpillable();
    }

    protected virtual void OnPuddleInit(Entity<PuddleComponent> entity, ref ComponentInit args)
    {
        SolutionSystem.TryEnsureSolution(entity.Owner, PuddleSolutionId, out _, FixedPoint2.New(PuddleVolume));
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
        if (!SolutionSystem.ResolveSolution(entity.Owner, PuddleSolutionId, ref entity.Comp.Solution))
            return;

        var reagentQuantity = SolutionSystem.GetPrimaryReagent(entity.Comp.Solution.Value);
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

            if (HasComp<EvaporationComponent>(entity) &&
                SolutionSystem.ResolveSolution(entity.Owner,
                    PuddleSolutionId,
                    ref entity.Comp.Solution))
            {
                if (CanFullyEvaporate(entity.Comp.Solution.Value))
                    args.PushMarkup(Loc.GetString("puddle-component-examine-evaporating"));
                else if (SolutionSystem.GetTotalQuantity(entity.Comp.Solution.Value, EvaporationReagents) > FixedPoint2.Zero)
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
        var splitSol = SolutionSystem.SplitSolution(entity.Comp.Solution.Value,
            entity.Comp.Solution.Value.Comp.Volume * 0.15f);
        ReactiveSystem.DoEntityReaction(args.Slipped, splitSol, ReactionMethod.Touch);
    }

    private void UpdateAppearance(EntityUid uid,
        PuddleComponent? puddleComponent = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref puddleComponent, ref appearance, false))
        {
            return;
        }

        var volume = FixedPoint2.Zero;
        var color = Color.DeepPink;//Debug color

        if (SolutionSystem.ResolveSolution(uid, PuddleSolutionId, ref puddleComponent.Solution))
        {
            var solution = puddleComponent.Solution.Value;
            volume = solution.Comp.Volume / puddleComponent.OverflowVolume;

            // Make blood stand out more
            // Kinda EH
            // Could potentially do alpha per-solution but future problem.

            color = SolutionSystem.GetSolutionColor(solution, StandoutReagentDucking);
        }

        AppearanceSystem.SetData(uid, PuddleVisuals.CurrentVolume, volume.Float(), appearance);
        AppearanceSystem.SetData(uid, PuddleVisuals.SolutionColor, color, appearance);
    }

    private void UpdateSlow(EntityUid uid, Entity<SolutionComponent> solution)
    {
        var maxViscosity = 0f;
        foreach (var (reagent, _) in SolutionSystem.EnumerateReagents(solution))
        {
            maxViscosity = Math.Max(maxViscosity, reagent.Entity.Comp.Viscosity);
        }

        if (maxViscosity > 0)
        {
            var comp = EnsureComp<SpeedModifierContactsComponent>(uid);
            var speed = 1 - maxViscosity;
            SpeedContactSystem.ChangeModifiers(uid, speed, comp);
        }
        else
        {
            RemComp<SpeedModifierContactsComponent>(uid);
        }
    }
}
