using System.Diagnostics.CodeAnalysis;
using Content.Shared._MACRO.CCVar;
using Content.Shared._MACRO.Species.Kodepiia.Consume.Components;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Systems;
using Content.Shared.Gibbing;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared._MACRO.Species.Kodepiia.Consume;

/// <summary>
///     System that handles entities that consume other entities... It's entity cannibalism.
/// </summary>
public abstract partial class SharedConsumeSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;

    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ForensicsSystem _forensics = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private StomachSystem _stomach = default!;

    private int _gibThreshold;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ConsumeGetLargestStomachEvent>(_body.RelayEvent);

        Subs.CVar(_config,
            MacroCCVars.ConsumptionGibThreshold,
            value => _gibThreshold = value,
            invokeImmediately: true);
    }

    /// <summary>
    ///     Give consumers an action to targeting entities for consumption.
    /// </summary>
    /// <param name="ent">The consumer entity.</param>
    [SubscribeLocalEvent]
    private void OnStartup(Entity<ConsumeActionComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.ConsumeAction, ent.Comp.ConsumeActionId);
    }

    /// <summary>
    ///     Remove the "consume" action from consumers.
    /// </summary>
    /// <param name="ent">The consumer entity.</param>
    [SubscribeLocalEvent]
    private void OnShutdown(Entity<ConsumeActionComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ConsumeAction);
    }

    /// <summary>
    ///     Attempt to begin consuming a target if a valid target is selected.
    /// </summary>
    /// <param name="ent">The consumer entity.</param>
    [SubscribeLocalEvent]
    private void OnConsumeAction(Entity<ConsumeActionComponent> ent, ref ConsumeEvent args)
    {
        var target = args.Target;
        var targetName = Identity.Entity(target, EntityManager);

        // Ensure the consumer has a mouth.
        if (!_ingestion.HasMouthAvailable(ent.Owner, ent.Owner))
        {
            var popupText = Loc.GetString(ent.Comp.ConsumeFailByBlock);
            _popup.PopupEntity(popupText, ent, ent);
            return;
        }

        // Ensure the target passes the whitelist and blacklist.
        if (!_whitelist.CheckBoth(target, ent.Comp.Blacklist, ent.Comp.Whitelist))
        {
            var popupText = Loc.GetString(ent.Comp.ConsumeFailByInedible, ("target", targetName));
            _popup.PopupEntity(popupText, ent, ent);
            return;
        }

        // Ensure the target is incapacitated.
        if (!_mobState.IsIncapacitated(target))
        {
            var popupText = Loc.GetString(ent.Comp.ConsumeFailByIncapacitated, ("target", targetName));
            _popup.PopupEntity(popupText, ent, ent);
            return;
        }

        // Begin our attempt to consume the target
        var consumeTime = GetConsumeTime(ent, target);
        var ev = new ConsumeDoAfterEvent();
        var doargs = new DoAfterArgs(EntityManager,
            user: ent,
            seconds: consumeTime,
            @event: ev,
            eventTarget: ent,
            target: target);

        _doAfter.TryStartDoAfter(doargs);
        DoConsumeStartPopup(ent, target);
        PlayConsumeSound(ent);
        args.Handled = true;
    }

    /// <summary>
    ///     Take a bite out of a valid target if we successfully finish consumption.
    /// </summary>
    /// <param name="ent">The consumer entity.</param>
    [SubscribeLocalEvent]
    private void OnConsumeDoAfter(Entity<ConsumeActionComponent> ent, ref ConsumeDoAfterEvent args)
    {
        if (args.Cancelled
            || args.Target == null
            || !_ingestion.HasMouthAvailable(ent.Owner, ent.Owner)
            || !_whitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist)
            || !_mobState.IsIncapacitated(args.Target.Value))
            return;

        var ev = new ConsumeGetLargestStomachEvent();
        RaiseLocalEvent(ent, ref ev);

        // All stomachs are full or we have no stomachs
        if (ev.LargestStomach == null)
        {
            var verb = Loc.GetString(ent.Comp.ConsumeVerb);
            var popupText = Loc.GetString(ent.Comp.ConsumeFailByFullStomach, ("verb", verb));
            _popup.PopupEntity(popupText, ent, ent);
            return;
        }

        Consume(ent, args.Target.Value, ev.LargestStomach.Value.AsNullable());
        DoConsumeSuccessPopup(ent, args.Target.Value);
    }

    /// <summary>
    ///     Update the largest stomach if this stomach is larger than the previous one.
    /// </summary>
    /// <param name="ent">The stomach entity.</param>
    [SubscribeLocalEvent]
    private void OnGetLargestStomach(Entity<StomachComponent> ent, ref BodyRelayedEvent<ConsumeGetLargestStomachEvent> args)
    {
        if (!TryGetStomachSolution(ent.AsNullable(), out var stomachSol))
            return;

        // If this stomach is larger than the previous, then we replace the largest stomach with this one
        var largest = args.Args.LargestStomach;
        if (largest == null
            || (TryGetStomachSolution(largest.Value.Owner, out var largestSol)
                && stomachSol.AvailableVolume > largestSol.AvailableVolume))
            args.Args = new ConsumeGetLargestStomachEvent(LargestStomach: ent);
    }

    /// <summary>
    ///     Show popups for a consumer attempting to bite a target.
    /// </summary>
    /// <param name="ent">The entity that is consuming the target.</param>
    /// <param name="target">The target of consumption.</param>
    private void DoConsumeStartPopup(Entity<ConsumeActionComponent> ent, EntityUid target)
    {
        var consumerName = Identity.Entity(ent, EntityManager);
        var targetName = Identity.Entity(target, EntityManager);

        // Do the popup for ourselves.
        if (ent.Comp.PopupSelfStart != null)
        {
            var popupSelf = Loc.GetString(ent.Comp.PopupSelfStart,
                ("user", consumerName),
                ("target", targetName));
            _popup.PopupEntity(popupSelf, ent, ent);
        }

        // Do the popup for others.
        if (ent.Comp.PopupOthersStart != null)
        {
            var allButSelf = Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent);
            var popupOthers = Loc.GetString(ent.Comp.PopupOthersStart,
                ("user", consumerName),
                ("target", targetName));
            _popup.PopupEntity(popupOthers, ent, allButSelf, true, PopupType.MediumCaution);
        }
    }

    /// <summary>
    ///     Show popups for a successful "consume" operation.
    /// </summary>
    /// <param name="ent">The entity that is consuming the target.</param>
    /// <param name="target">The target of consumption.</param>
    private void DoConsumeSuccessPopup(Entity<ConsumeActionComponent> ent, EntityUid target)
    {
        var consumerName = Identity.Entity(ent, EntityManager);
        var targetName = Identity.Entity(target, EntityManager);

        if (ent.Comp.PopupSelfEnd != null)
        {
            var popupSelf = Loc.GetString(ent.Comp.PopupSelfEnd,
                ("user", consumerName),
                ("target", targetName));

            _popup.PopupEntity(popupSelf, ent, ent);
        }

        if (ent.Comp.PopupOthersEnd != null)
        {
            var allButSelf = Filter.Pvs(ent).RemovePlayersByAttachedEntity(ent);
            var popupOthers = Loc.GetString(ent.Comp.PopupOthersEnd,
                ("user", consumerName),
                ("target", targetName));

            _popup.PopupEntity(popupOthers, ent, allButSelf, true, PopupType.MediumCaution);
        }
    }

    /// <summary>
    /// Have an entity consume another entity.
    /// </summary>
    /// <param name="consumer">Entity that consumes.</param>
    /// <param name="target">Entity that IS consumed.</param>
    /// <param name="stomach">The consumer's largest available stomach.</param>
    private void Consume(Entity<ConsumeActionComponent> consumer,
        EntityUid target,
        Entity<StomachComponent?> stomach)
    {
        IngestTargetContents(consumer, target, stomach);
        TakeABite(consumer, target);
    }

    /// <summary>
    ///     Make a consumer ingest a portion of blood and food reagents (e.g. uncooked proteins)
    ///     from a target entity.
    /// </summary>
    /// <param name="consumer">The entity that is consuming our target.</param>
    /// <param name="target">The poor guy who's getting nibbled on.</param>
    /// <param name="stomach">The consumer's largest available stomach.</param>
    private void IngestTargetContents(Entity<ConsumeActionComponent> consumer,
        EntityUid target,
        Entity<StomachComponent?> stomach)
    {
        if (!Resolve(stomach.Owner, ref stomach.Comp))
            return;

        // Get the solution to ingest from the target.
        var consumedSolution = GetConsumedSolution(consumer, target);

        // Spill excess reagents on the floor.
        TryGetStomachSolution(stomach, out var stomachSol);
        var stomachVol = stomachSol?.AvailableVolume ?? 0.0f;
        if (consumedSolution.Volume > stomachVol)
        {
            var split = consumedSolution.SplitSolution(consumedSolution.Volume - stomachVol);
            _puddle.TrySpillAt(consumer.Owner, split, out _);
        }

        // Add the ingested solution to the stomach.
        _stomach.TryTransferSolution(stomach.AsNullable(), consumedSolution);
    }

    /// <summary>
    ///     Inflict a single consumption "bite" on a target, damaging the body.
    /// </summary>
    /// <param name="consumer">The entity consuming the target.</param>
    /// <param name="target">The target being consumed.</param>
    private void TakeABite(Entity<ConsumeActionComponent> consumer, EntityUid target)
    {
        // Increase consumption amount of the victim
        EnsureComp<ConsumedComponent>(target, out var consumed);
        consumed.ConsumedValue += consumer.Comp.ConsumptionAmount;
        Dirty(target, consumed);

        // Gib if we exceed the threshold
        if (_gibThreshold >= 0 && consumed.ConsumedValue >= _gibThreshold)
        {
            _gibbing.Gib(target);
            return;
        }

        // I take a bite
        _forensics.TransferDna(target, consumer, false);
        _damage.TryChangeDamage(target, consumer.Comp.Damage, true, false);
        PlayConsumeSound(consumer);
    }

    /// <summary>
    /// Play the consume sound defined by an entity.
    /// </summary>
    /// <param name="ent">Entity to get the sound from and to play on.</param>
    private void PlayConsumeSound(Entity<ConsumeActionComponent> ent)
    {
        _audio.PlayPredicted(ent.Comp.ConsumptionSound, ent, ent);
    }

    /// <summary>
    ///     Get the duration in seconds it'll take to consume a target.
    /// </summary>
    /// <param name="ent">The entity that is consuming the target.</param>
    /// <param name="target">The target of consumption.</param>
    /// <returns>duration in seconds that it will take for a consumer to consume the target.</returns>
    private float GetConsumeTime(Entity<ConsumeActionComponent> ent, EntityUid target)
    {
        var consumeTime = ent.Comp.BaseConsumeTime;

        // Multiply by mass ratio, if applicable
        if (TryComp<PhysicsComponent>(target, out var targetPhysics)
            && TryComp<PhysicsComponent>(ent.Owner, out var consumerPhysics))
        {
            var massRatio = targetPhysics.Mass / consumerPhysics.Mass;
            consumeTime *= massRatio;
        }

        return (float)consumeTime.TotalSeconds;
    }

    /// <summary>
    ///     Construct a portion of blood, food reagents, and potential toxins for our consumer to ingest
    ///     from a target's body.
    /// </summary>
    /// <param name="consumer">The entity that is consuming our target.</param>
    /// <param name="target">The poor guy who's getting nibbled on.</param>
    /// <returns>The solution to ingest on consumption.</returns>
    private Solution GetConsumedSolution(Entity<ConsumeActionComponent> consumer, Entity<PhysicsComponent?> target)
    {
        // The solution that our consumer is going to ingest.
        var consumedSolution = new Solution();

        // The quantity of food reagents (e.g. uncooked proteins) we are gonna ingest.
        var mass = Resolve(target.Owner, ref target.Comp)
            ? target.Comp.Mass
            : 0.0f;
        var ingestedFoodVolume = mass * consumer.Comp.MeatMultiplier;

        // Add toxin to the ingested solution if the target is rotting.
        if (_rotting.IsRotten(target.Owner))
        {
            var toxinVolume = ingestedFoodVolume * consumer.Comp.ToxinRatio;
            var cleanSolutionRatio = 1 - consumer.Comp.ToxinRatio;
            ingestedFoodVolume *= cleanSolutionRatio;
            consumedSolution.AddReagent(consumer.Comp.Toxin, toxinVolume); // yummers
        }

        // I take a sip
        if (_solutionContainer.TryGetSolution(target.Owner,
                consumer.Comp.SolutionToDrinkFrom,
                out var bloodSolutionComp,
                out var targetBloodstream))
        {
            var ingestedBloodVolume = targetBloodstream.Volume * consumer.Comp.PortionDrunk;
            var ingestedBlood = _solutionContainer.SplitSolution(bloodSolutionComp.Value, ingestedBloodVolume);
            consumedSolution.AddSolution(ingestedBlood, ProtoMan);
        }

        // Finally, food reagents.
        // We do this at the end because other factors might change this quantity.
        consumedSolution.AddReagent(consumer.Comp.FoodReagentPrototype, ingestedFoodVolume);

        return consumedSolution;
    }

    private bool TryGetStomachSolution(Entity<StomachComponent?> ent, [NotNullWhen(true)] out Solution? solution)
    {
        solution = null;

        if (!Resolve(ent.Owner, ref ent.Comp)
            || !_solutionContainer.ResolveSolution(ent.Owner,
                StomachSystem.DefaultSolutionName,
                ref ent.Comp.Solution,
                out solution))
            return false;

        return solution != null;
    }
}

/// <summary>
///     Raised to get the consumer's largest stomach by available volume.
/// </summary>
[ByRefEvent]
// TODO: ingestion system really needs a refactor huh
public record struct ConsumeGetLargestStomachEvent(Entity<StomachComponent>? LargestStomach);

/// <summary>
///     Raised when the consumer targets an entity for consumption.
/// </summary>
public sealed partial class ConsumeEvent : EntityTargetActionEvent;

/// <summary>
///     Raised when a consumer successfully attempts to consume a target.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ConsumeDoAfterEvent : SimpleDoAfterEvent;
