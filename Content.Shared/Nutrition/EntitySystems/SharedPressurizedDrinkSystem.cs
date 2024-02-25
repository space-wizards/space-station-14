using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Throwing;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Chemistry.Components;
using Content.Shared.Effects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedPressurizedSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedOpenableSystem _openable = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
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

    private void OnMapInit(Entity<PressurizedSolutionComponent> entity, ref MapInitEvent args)
    {
        RollSprayThreshold(entity);
    }

    private void OnShake(Entity<PressurizedSolutionComponent> entity, ref ShakeEvent args)
    {
        AddFizziness(entity, entity.Comp.FizzinessAddedOnShake);
    }

    private void AddFizziness(Entity<PressurizedSolutionComponent> entity, float amount)
    {
        if (!SolutionIsFizzy(entity))
            return;

        // Convert fizziness to time
        var duration = amount * entity.Comp.FizzyMaxDuration;

        // Add to the existing settle time, if one exists. Otherwise, add to the current time
        var start = entity.Comp.FizzySettleTime > _timing.CurTime ? entity.Comp.FizzySettleTime : _timing.CurTime;
        var newTime = start + duration;

        // Cap the maximum fizziness
        var maxEnd = _timing.CurTime + entity.Comp.FizzyMaxDuration;
        if (newTime > maxEnd)
            newTime = maxEnd;

        entity.Comp.FizzySettleTime = newTime;

        // Roll a new fizziness threshold
        RollSprayThreshold(entity);
    }

    private void OnOpened(Entity<PressurizedSolutionComponent> entity, ref OpenableOpenedEvent args)
    {
        if (SprayCheck(entity, entity.Comp.SprayChanceModOpened))
        {
            // Make sure the opener is actually holding the drink
            var held = args.User != null && _hands.IsHolding(args.User.Value, entity, out _);

            TrySpray((entity, entity.Comp), held ? args.User : null);
        }

        // Release the fizz!
        TryClearFizziness((entity, entity.Comp));
    }

    private void OnLand(Entity<PressurizedSolutionComponent> entity, ref LandEvent args)
    {
        SprayOrAddFizziness(entity, entity.Comp.SprayChanceModThrown, entity.Comp.FizzinessAddedOnLand);
    }

    private void OnSolutionUpdate(Entity<PressurizedSolutionComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != entity.Comp.Solution)
            return;

        if (!SolutionIsFizzy(entity))
            TryClearFizziness((entity, entity.Comp));
    }

    private bool SprayCheck(Entity<PressurizedSolutionComponent> entity, float chanceMod = 0)
    {
        return Fizziness((entity, entity.Comp)) + chanceMod > entity.Comp.SprayFizzinessThresholdRoll;
    }

    private bool SolutionIsFizzy(Entity<PressurizedSolutionComponent> entity)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solutionComp, out var solution))
            return false;

        if (solution.Volume <= 0)
            return false;

        foreach (var reagent in solution.Contents)
        {
            if (_prototypeManager.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? reagentProto))
            {
                if (reagentProto != null && reagentProto.Fizzy)
                    return true;
            }
        }
        return false;
    }

    private void SprayOrAddFizziness(Entity<PressurizedSolutionComponent> entity, float chanceMod = 0, float fizzinessToAdd = 0, EntityUid? user = null)
    {
        if (SprayCheck(entity, chanceMod))
        {
            _openable.SetOpen(entity, true);
            TrySpray((entity, entity.Comp), user);
        }
        else
        {
            AddFizziness(entity, fizzinessToAdd);
        }
    }

    private void RollSprayThreshold(Entity<PressurizedSolutionComponent> entity)
    {
        // Can't predict random, so we wait for the server to tell us
        if (!_net.IsServer)
            return;

        entity.Comp.SprayFizzinessThresholdRoll = _random.NextFloat();
        Dirty(entity, entity.Comp);
    }

    public bool CanSpray(Entity<PressurizedSolutionComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        return SolutionIsFizzy((entity, entity.Comp));
    }

    public bool TrySpray(Entity<PressurizedSolutionComponent?> entity, EntityUid? user = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (!CanSpray(entity))
            return false;

        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var soln, out var interactions))
            return false;

        var solution = _solutionContainer.SplitSolution(soln.Value, interactions.Volume);
        var drinkName = Identity.Entity(entity, EntityManager);

        DoSpraySplash((entity, entity.Comp), solution, user);

        if (user != null)
        {
            _reactive.DoEntityReaction(user.Value, solution, ReactionMethod.Touch);

            var victimName = Identity.Entity(user.Value, EntityManager);

            _popup.PopupEntity(Loc.GetString(entity.Comp.SprayHolderMessageOthers, ("victim", victimName), ("drink", drinkName)), user.Value, Filter.PvsExcept(user.Value), true);
            _popup.PopupClient(Loc.GetString(entity.Comp.SprayHolderMessageSelf, ("victim", victimName), ("drink", drinkName)), user.Value, user.Value);
        }
        else
        {
            if (_timing.IsFirstTimePredicted)
                _popup.PopupEntity(Loc.GetString(entity.Comp.SprayGroundMessage, ("drink", drinkName)), entity);
        }

        _audio.PlayPredicted(entity.Comp.SpraySound, entity, user);
        TryClearFizziness(entity);

        return true;
    }

    // TODO: When more of PuddleSystem is in Shared, move the rest of this method from Server to Shared
    protected virtual void DoSpraySplash(Entity<PressurizedSolutionComponent> entity, Solution sol, EntityUid? user = null)
    {
        if (user != null)
        {
            var targets = new List<EntityUid>() { user.Value };
            _colorFlash.RaiseEffect(sol.GetColor(_prototypeManager), targets, Filter.Pvs(user.Value, entityManager: EntityManager));
        }
    }

    public double Fizziness(Entity<PressurizedSolutionComponent?> entity)
    {
        // No component means no fizz
        if (!Resolve(entity, ref entity.Comp))
            return 0;

        // No negative fizziness
        if (entity.Comp.FizzySettleTime <= _timing.CurTime)
            return 0;

        var currentDuration = entity.Comp.FizzySettleTime - _timing.CurTime;
        return Math.Min(currentDuration / entity.Comp.FizzyMaxDuration, 1);
    }

    public void TryClearFizziness(Entity<PressurizedSolutionComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.FizzySettleTime = TimeSpan.Zero;

        // Roll a new fizziness threshold
        RollSprayThreshold((entity, entity.Comp));
    }

}
