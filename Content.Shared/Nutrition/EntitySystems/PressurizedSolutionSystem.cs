using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Content.Shared.IdentityManagement;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Content.Shared.Fluids;
using Content.Shared.Popups;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class PressurizedSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressurizedSolutionComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PressurizedSolutionComponent, ShakeEvent>(OnShake);
        SubscribeLocalEvent<PressurizedSolutionComponent, OpenableOpenedEvent>(OnOpened);
        SubscribeLocalEvent<PressurizedSolutionComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<PressurizedSolutionComponent, SolutionContainerChangedEvent>(OnSolutionUpdate);
    }

    /// <summary>
    /// Helper method for checking if the solution's fizziness is high enough to spray.
    /// <paramref name="chanceMod"/> is added to the actual fizziness for the comparison.
    /// </summary>
    private bool SprayCheck(Entity<PressurizedSolutionComponent> entity, float chanceMod = 0)
    {
        return Fizziness((entity, entity.Comp)) + chanceMod > entity.Comp.SprayFizzinessThresholdRoll;
    }

    /// <summary>
    /// Calculates how readily the contained solution becomes fizzy.
    /// </summary>
    private float SolutionFizzability(Entity<PressurizedSolutionComponent> entity)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var _, out var solution))
            return 0;

        // An empty solution can't be fizzy
        if (solution.Volume <= 0)
            return 0;

        var totalFizzability = 0f;

        // Check each reagent in the solution
        foreach (var reagent in solution.Contents)
        {
            if (_prototypeManager.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? reagentProto) && reagentProto != null)
            {
                // What portion of the solution is this reagent?
                var proportion = (float) (reagent.Quantity / solution.Volume);
                totalFizzability += reagentProto.Fizziness * proportion;
            }
        }

        return totalFizzability;
    }

    /// <summary>
    /// Increases the fizziness level of the solution by the given amount,
    /// scaled by the solution's fizzability.
    /// 0 will result in no change, and 1 will maximize fizziness.
    /// Also rerolls the spray threshold.
    /// </summary>
    private void AddFizziness(Entity<PressurizedSolutionComponent> entity, float amount)
    {
        var fizzability = SolutionFizzability(entity);

        // Can't add fizziness if the solution isn't fizzy
        if (fizzability <= 0)
            return;

        // Make sure nothing is preventing fizziness from being added
        var attemptEv = new AttemptAddFizzinessEvent(entity, amount);
        RaiseLocalEvent(entity, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        // Scale added fizziness by the solution's fizzability
        amount *= fizzability;

        // Convert fizziness to time
        var duration = amount * entity.Comp.FizzinessMaxDuration;

        // Add to the existing settle time, if one exists. Otherwise, add to the current time
        var start = entity.Comp.FizzySettleTime > _timing.CurTime ? entity.Comp.FizzySettleTime : _timing.CurTime;
        var newTime = start + duration;

        // Cap the maximum fizziness
        var maxEnd = _timing.CurTime + entity.Comp.FizzinessMaxDuration;
        if (newTime > maxEnd)
            newTime = maxEnd;

        entity.Comp.FizzySettleTime = newTime;

        // Roll a new fizziness threshold
        RollSprayThreshold(entity);
    }

    /// <summary>
    /// Helper method. Performs a <see cref="SprayCheck"/>. If it passes, calls <see cref="TrySpray"/>. If it fails, <see cref="AddFizziness"/>.
    /// </summary>
    private void SprayOrAddFizziness(Entity<PressurizedSolutionComponent> entity, float chanceMod = 0, float fizzinessToAdd = 0, EntityUid? user = null)
    {
        if (SprayCheck(entity, chanceMod))
            TrySpray((entity, entity.Comp), user);
        else
            AddFizziness(entity, fizzinessToAdd);
    }

    /// <summary>
    /// Randomly generates a new spray threshold.
    /// This is the value used to compare fizziness against when doing <see cref="SprayCheck"/>.
    /// Since RNG will give different results between client and server, this is run on the server
    /// and synced to the client by marking the component dirty.
    /// We roll this in advance, rather than during <see cref="SprayCheck"/>, so that the value (hopefully)
    /// has time to get synced to the client, so we can try be accurate with prediction.
    /// </summary>
    private void RollSprayThreshold(Entity<PressurizedSolutionComponent> entity)
    {
        // Can't predict random, so we wait for the server to tell us
        if (!_net.IsServer)
            return;

        entity.Comp.SprayFizzinessThresholdRoll = _random.NextFloat();
        Dirty(entity, entity.Comp);
    }

    #region Public API

    /// <summary>
    /// Does the entity contain a solution capable of being fizzy?
    /// </summary>
    public bool CanSpray(Entity<PressurizedSolutionComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        return SolutionFizzability((entity, entity.Comp)) > 0;
    }

    /// <summary>
    /// Attempts to spray the solution onto the given entity, or the ground if none is given.
    /// Fails if the solution isn't able to be sprayed.
    /// </summary>
    public bool TrySpray(Entity<PressurizedSolutionComponent?> entity, EntityUid? target = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanSpray(entity))
            return false;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var soln, out var interactions))
            return false;

        // If the container is openable, open it
        _openable.SetOpen(entity, true);

        // Get the spray solution from the container
        var solution = _solutionContainer.SplitSolution(soln.Value, interactions.Volume);

        // Spray the solution onto the ground and anyone nearby
        var coordinates = Transform(entity).Coordinates;
        _puddle.TrySplashSpillAt(entity.Owner, coordinates, out _, out _, sound: false);

        var drinkName = Identity.Entity(entity, EntityManager);

        if (target != null)
        {
            var victimName = Identity.Entity(target.Value, EntityManager);

            var selfMessage = Loc.GetString(entity.Comp.SprayHolderMessageSelf, ("victim", victimName), ("drink", drinkName));
            var othersMessage = Loc.GetString(entity.Comp.SprayHolderMessageOthers, ("victim", victimName), ("drink", drinkName));
            _popup.PopupPredicted(selfMessage, othersMessage, target.Value, target.Value);
        }
        else
        {
            // Show a popup to everyone in PVS range
            if (_timing.IsFirstTimePredicted)
                _popup.PopupEntity(Loc.GetString(entity.Comp.SprayGroundMessage, ("drink", drinkName)), entity);
        }

        _audio.PlayPredicted(entity.Comp.SpraySound, entity, target);

        // We just used all our fizziness, so clear it
        TryClearFizziness(entity);

        return true;
    }

    /// <summary>
    /// What is the current fizziness level of the solution, from 0 to 1?
    /// </summary>
    public double Fizziness(Entity<PressurizedSolutionComponent?> entity)
    {
        // No component means no fizz
        if (!Resolve(entity, ref entity.Comp, false))
            return 0;

        // No negative fizziness
        if (entity.Comp.FizzySettleTime <= _timing.CurTime)
            return 0;

        var currentDuration = entity.Comp.FizzySettleTime - _timing.CurTime;
        return Easings.InOutCubic((float) Math.Min(currentDuration / entity.Comp.FizzinessMaxDuration, 1));
    }

    /// <summary>
    /// Attempts to clear any fizziness in the solution.
    /// </summary>
    /// <remarks>Rolls a new spray threshold.</remarks>
    public void TryClearFizziness(Entity<PressurizedSolutionComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.FizzySettleTime = TimeSpan.Zero;

        // Roll a new fizziness threshold
        RollSprayThreshold((entity, entity.Comp));
    }

    #endregion

    #region Event Handlers
    private void OnMapInit(Entity<PressurizedSolutionComponent> entity, ref MapInitEvent args)
    {
        RollSprayThreshold(entity);
    }

    private void OnOpened(Entity<PressurizedSolutionComponent> entity, ref OpenableOpenedEvent args)
    {
        // Make sure the opener is actually holding the drink
        var held = args.User != null && _hands.IsHolding(args.User.Value, entity, out _);

        SprayOrAddFizziness(entity, entity.Comp.SprayChanceModOnOpened, -1, held ? args.User : null);
    }

    private void OnShake(Entity<PressurizedSolutionComponent> entity, ref ShakeEvent args)
    {
        SprayOrAddFizziness(entity, entity.Comp.SprayChanceModOnShake, entity.Comp.FizzinessAddedOnShake, args.Shaker);
    }

    private void OnLand(Entity<PressurizedSolutionComponent> entity, ref LandEvent args)
    {
        SprayOrAddFizziness(entity, entity.Comp.SprayChanceModOnLand, entity.Comp.FizzinessAddedOnLand);
    }

    private void OnSolutionUpdate(Entity<PressurizedSolutionComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != entity.Comp.Solution)
            return;

        // If the solution is no longer capable of being fizzy, clear any built up fizziness
        if (SolutionFizzability(entity) <= 0)
            TryClearFizziness((entity, entity.Comp));
    }

    #endregion
}

[ByRefEvent]
public record struct AttemptAddFizzinessEvent(Entity<PressurizedSolutionComponent> Entity, float Amount)
{
    public bool Cancelled;
}
