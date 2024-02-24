using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
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

namespace Content.Shared.Nutrition.EntitySystems;

public abstract partial class SharedPressurizedDrinkSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedOpenableSystem _openable = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressurizedDrinkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PressurizedDrinkComponent, ShakeEvent>(OnShake);
        SubscribeLocalEvent<PressurizedDrinkComponent, OpenableOpenedEvent>(OnOpened);
        SubscribeLocalEvent<PressurizedDrinkComponent, LandEvent>(OnLand);

        SubscribeLocalEvent<PressurizedDrinkComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<PressurizedDrinkComponent> entity, ref MapInitEvent args)
    {
        RollSprayThreshold(entity);
    }

    private void OnExamined(Entity<PressurizedDrinkComponent> entity, ref ExaminedEvent args)
    {
        args.PushText(string.Format("Fizziness: {0}", Fizziness((entity, entity.Comp))));
    }

    private void OnShake(Entity<PressurizedDrinkComponent> entity, ref ShakeEvent args)
    {
        AddFizziness(entity, entity.Comp.FizzinessAddedPerShake);
    }

    private void AddFizziness(Entity<PressurizedDrinkComponent> entity, float amount)
    {
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

    private void OnOpened(Entity<PressurizedDrinkComponent> entity, ref OpenableOpenedEvent args)
    {
        if (Fizziness((entity, entity.Comp)) + entity.Comp.SprayChanceModOpened > entity.Comp.SprayFizzinessThresholdRoll)
        {
            // Make sure the opener is actually holding the drink
            var held = args.User != null && _hands.IsHolding(args.User.Value, entity, out _);

            Spray(entity, held ? args.User : null);
        }

        // Release the fizz!
        ClearFizziness((entity, entity.Comp));
    }

    private void OnLand(Entity<PressurizedDrinkComponent> entity, ref LandEvent args)
    {
        if (Fizziness((entity, entity.Comp)) + entity.Comp.SprayChanceModThrown > entity.Comp.SprayFizzinessThresholdRoll)
        {
            _openable.SetOpen(entity, true);
            Spray(entity);
        }
        else
        {
            RollSprayThreshold(entity);
        }
    }

    private void RollSprayThreshold(Entity<PressurizedDrinkComponent> entity)
    {
        entity.Comp.SprayFizzinessThresholdRoll = _random.NextFloat();
        Dirty(entity, entity.Comp);
    }

    private void Spray(Entity<PressurizedDrinkComponent> entity, EntityUid? user = null)
    {
        if (_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var soln, out var interactions))
        {
            var solution = _solutionContainer.SplitSolution(soln.Value, interactions.Volume);

            var drinkName = Identity.Entity(entity, EntityManager);

            DoSpraySplash(entity, solution, user);

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
        }

        // Can only do it once
        RemCompDeferred(entity, entity.Comp);
    }

    // TODO: When more of PuddleSystem is in Shared, move the rest of this method from Server to Shared
    protected virtual void DoSpraySplash(Entity<PressurizedDrinkComponent> entity, Solution sol, EntityUid? user = null)
    {
        if (user != null)
        {
            var targets = new List<EntityUid>() { user.Value };
            _colorFlash.RaiseEffect(sol.GetColor(_prototypeManager), targets, Filter.Pvs(user.Value, entityManager: EntityManager));
        }
    }

    public double Fizziness(Entity<PressurizedDrinkComponent?> entity)
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

    public void ClearFizziness(Entity<PressurizedDrinkComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.FizzySettleTime = TimeSpan.Zero;

        // Roll a new fizziness threshold
        RollSprayThreshold((entity, entity.Comp));
    }

}
