using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
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
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] protected readonly ISharedAdminLogManager AdminLogger = default!;
    [Dependency] protected readonly OpenableSystem Openable = default!;
    [Dependency] protected readonly ReactiveSystem Reactive = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly SharedPopupSystem Popups = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly SpeedModifierContactsSystem _speedModContacts = default!;
    [Dependency] private readonly StepTriggerSystem _stepTrigger = default!;
    [Dependency] private readonly TileFrictionController _tile = default!;

    private string[] _standoutReagents = [];

    /// <summary>
    /// The lowest threshold to be considered for puddle sprite states as well as slipperiness of a puddle.
    /// </summary>
    public const float LowThreshold = 0.3f;

    public const float MediumThreshold = 0.6f;

    // Using local deletion queue instead of the standard queue so that we can easily "undelete" if a puddle
    // loses & then gains reagents in a single tick.
    private HashSet<EntityUid> _deletionQueue = [];

    private EntityQuery<StepTriggerComponent> _stepTriggerQuery;
    private EntityQuery<ReactiveComponent> _reactiveQuery;
    private EntityQuery<EvaporationComponent> _evaporationQuery;

    public override void Initialize()
    {
        base.Initialize();
        // Shouldn't need re-anchoring.
        SubscribeLocalEvent<PuddleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<PuddleComponent, SolutionContainerChangedEvent>(OnSolutionUpdate);
        SubscribeLocalEvent<PuddleComponent, GetFootstepSoundEvent>(OnGetFootstepSound);
        SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
        SubscribeLocalEvent<PuddleComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<EvaporationComponent, MapInitEvent>(OnEvaporationMapInit);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        _stepTriggerQuery = GetEntityQuery<StepTriggerComponent>();
        _reactiveQuery = GetEntityQuery<ReactiveComponent>();
        _evaporationQuery = GetEntityQuery<EvaporationComponent>();

        CacheStandsout();
        InitializeSpillable();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _deletionQueue)
        {
            // It's possible to have items in the queue that are already being deleted but threw a
            // SolutionContainerChangedEvent as a part of their shutdown, like during a round restart.
            if (!TerminatingOrDeleted(ent))
                PredictedDel(ent);
        }

        _deletionQueue.Clear();

        TickEvaporation();
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

    private void OnSolutionUpdate(Entity<PuddleComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != entity.Comp.SolutionName)
            return;

        if (args.Solution.Volume <= 0)
        {
            _deletionQueue.Add(entity);
            return;
        }

        _deletionQueue.Remove(entity);
        UpdateSlip((entity, entity.Comp), args.Solution);
        UpdateSlow(entity, args.Solution);
        UpdateEvaporation(entity, args.Solution);
        UpdateAppearance((entity, entity.Comp));
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
            if (_stepTriggerQuery.TryComp(entity, out var slippery) && slippery.Active)
            {
                args.PushMarkup(Loc.GetString("puddle-component-examine-is-slippery-text"));
            }

            if (_evaporationQuery.HasComp(entity) &&
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

    private void OnAnchorChanged(Entity<PuddleComponent> entity, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            PredictedQueueDel(entity.Owner);
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

    private void UpdateSlip(Entity<PuddleComponent> entity, Solution solution)
    {
        if (!_stepTriggerQuery.TryComp(entity, out var comp))
            return;

        // Ensure we actually have the component
        EnsureComp<TileFrictionModifierComponent>(entity);
        EnsureComp<SlipperyComponent>(entity, out var slipComp);

        // This is the base amount of reagent needed before a puddle can be considered slippery. Is defined based on
        // the sprite threshold for a puddle larger than 5 pixels.
        var smallPuddleThreshold = FixedPoint2.New(entity.Comp.OverflowVolume.Float() * LowThreshold);

        // Stores how many units of slippery reagents a puddle has
        var slipperyUnits = FixedPoint2.Zero;
        // Stores how many units of super slippery reagents a puddle has
        var superSlipperyUnits = FixedPoint2.Zero;

        // These three values will be averaged later and all start at zero so the calculations work
        // A cumulative weighted amount of minimum speed to slip values
        var puddleFriction = FixedPoint2.Zero;
        // A cumulative weighted amount of minimum speed to slip values
        var slipStepTrigger = FixedPoint2.Zero;
        // A cumulative weighted amount of launch multipliers from slippery reagents
        var launchMult = FixedPoint2.Zero;
        // A cumulative weighted amount of stun times from slippery reagents
        var stunTimer = TimeSpan.Zero;
        // A cumulative weighted amount of knockdown times from slippery reagents
        var knockdownTimer = TimeSpan.Zero;

        // Check if the puddle is big enough to slip in to avoid doing unnecessary logic
        if (solution.Volume <= smallPuddleThreshold)
        {
            _stepTrigger.SetActive(entity, false, comp);
            _tile.SetModifier(entity, 1f);
            slipComp.SlipData.SlipFriction = 1f;
            slipComp.AffectsSliding = false;
            Dirty(entity, slipComp);
            return;
        }

        slipComp.AffectsSliding = true;

        foreach (var (reagent, quantity) in solution.Contents)
        {
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);

            // Calculate the minimum speed needed to slip in the puddle. Average the overall slip thresholds for all reagents
            var deltaSlipTrigger = reagentProto.SlipData?.RequiredSlipSpeed ?? entity.Comp.DefaultSlippery;
            slipStepTrigger += quantity * deltaSlipTrigger;

            // Aggregate Friction based on quantity
            puddleFriction += reagentProto.Friction * quantity;

            if (reagentProto.SlipData == null)
                continue;

            slipperyUnits += quantity;
            // Aggregate launch speed based on quantity
            launchMult += reagentProto.SlipData.LaunchForwardsMultiplier * quantity;
            // Aggregate stun times based on quantity
            stunTimer += reagentProto.SlipData.StunTime * (float)quantity;
            knockdownTimer += reagentProto.SlipData.KnockdownTime * (float)quantity;

            if (reagentProto.SlipData.SuperSlippery)
                superSlipperyUnits += quantity;
        }

        // Turn on the step trigger if it's slippery
        _stepTrigger.SetActive(entity, slipperyUnits > smallPuddleThreshold, comp);

        // This is based of the total volume and not just the slippery volume because there is a default
        // slippery for all reagents even if they aren't technically slippery.
        slipComp.SlipData.RequiredSlipSpeed = (float)(slipStepTrigger / solution.Volume);
        _stepTrigger.SetRequiredTriggerSpeed(entity, slipComp.SlipData.RequiredSlipSpeed);

        // Divide these both by only total amount of slippery reagents.
        // A puddle with 10 units of lube vs a puddle with 10 of lube and 20 catchup should stun and launch forward the same amount.
        if (slipperyUnits > 0)
        {
            slipComp.SlipData.LaunchForwardsMultiplier = (float)(launchMult/slipperyUnits);
            slipComp.SlipData.StunTime = (stunTimer/(float)slipperyUnits);
            slipComp.SlipData.KnockdownTime = (knockdownTimer/(float)slipperyUnits);
        }

        // Only make it super slippery if there is enough super slippery units for its own puddle
        slipComp.SlipData.SuperSlippery = superSlipperyUnits >= smallPuddleThreshold;

        // Lower tile friction based on how slippery it is, lets items slide across a puddle of lube
        slipComp.SlipData.SlipFriction = (float)(puddleFriction/solution.Volume);
        _tile.SetModifier(entity, slipComp.SlipData.SlipFriction);

        Dirty(entity, slipComp);
    }

    private void UpdateSlow(EntityUid uid, Solution solution)
    {
        var maxViscosity = 0f;
        foreach (var (reagent, _) in solution.Contents)
        {
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);
            maxViscosity = Math.Max(maxViscosity, reagentProto.Viscosity);
        }

        if (maxViscosity > 0)
        {
            var comp = EnsureComp<SpeedModifierContactsComponent>(uid);
            var speed = 1 - maxViscosity;
            _speedModContacts.ChangeSpeedModifiers(uid, speed, comp);
        }
        else
        {
            RemComp<SpeedModifierContactsComponent>(uid);
        }
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
    /// First splashes reagent on reactive entities near the spilling entity, then spills the rest regularly to a
    /// puddle. This is intended for 'destructive' spills, like when entities are destroyed or thrown.
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"/> and return false.
    /// </remarks>
    public abstract bool TrySplashSpillAt(EntityUid uid,
        EntityCoordinates coordinates,
        Solution solution,
        out EntityUid puddleUid,
        bool sound = true,
        EntityUid? user = null);

    /// <summary>
    /// Spills solution at the specified coordinates.
    /// Will add to an existing puddle if present or create a new one if not.
    /// </summary>
    /// <remarks>
    /// On the client, this will always set <paramref name="puddleUid"/> to <see cref="EntityUid.Invalid"/> and return false.
    /// </remarks>
    public abstract bool TrySpillAt(EntityCoordinates coordinates, Solution solution, out EntityUid puddleUid, bool sound = true);

    /// <inheritdoc cref="TrySpillAt(EntityCoordinates, Solution, out EntityUid, bool)"/>
    public abstract bool TrySpillAt(EntityUid uid, Solution solution, out EntityUid puddleUid, bool sound = true,
        TransformComponent? transformComponent = null);

    /// <inheritdoc cref="TrySpillAt(EntityCoordinates, Solution, out EntityUid, bool)"/>
    public abstract bool TrySpillAt(TileRef tileRef, Solution solution, out EntityUid puddleUid, bool sound = true,
        bool tileReact = true);

    #endregion Spill
}
