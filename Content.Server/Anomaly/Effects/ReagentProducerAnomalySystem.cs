using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Sprite;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Anomaly.Effects;

/// <see cref="ReagentProducerAnomalyComponent"/>

public sealed class ReagentProducerAnomalySystem : EntitySystem
{
    //The idea is to divide substances into several categories.
    //The anomaly will choose one of the categories with a given chance based on severity.
    //Then a random substance will be selected from the selected category.
    //There are the following categories:

    //Dangerous:
    //selected most often. A list of substances that are extremely unpleasant for injection.

    //Fun:
    //Funny things have an increased chance of appearing in an anomaly.

    //Useful:
    //Those reagents that the players are hunting for. Very low percentage of loss.

    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public const string FallbackReagent = "Water";

    public override void Initialize()
    {
        SubscribeLocalEvent<ReagentProducerAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<ReagentProducerAnomalyComponent, MapInitEvent>(OnMapInit);
    }

    private void OnPulse(Entity<ReagentProducerAnomalyComponent> entity, ref AnomalyPulseEvent args)
    {
        if (_random.NextFloat(0.0f, 1.0f) > args.Stability)
            ChangeReagent(entity, args.Severity);
    }

    private void ChangeReagent(Entity<ReagentProducerAnomalyComponent> entity, float severity)
    {
        var reagent = GetRandomReagentType(entity, severity);
        entity.Comp.ProducingReagent = reagent;
        _audio.PlayPvs(entity.Comp.ChangeSound, entity);
    }

    //reagent realtime generation
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ReagentProducerAnomalyComponent, AnomalyComponent>();
        while (query.MoveNext(out var uid, out var component, out var anomaly))
        {
            component.AccumulatedFrametime += frameTime;

            if (component.AccumulatedFrametime < component.UpdateInterval)
                continue;

            if (!_solutionContainer.ResolveSolution(uid, component.SolutionName, ref component.Solution, out var producerSolution))
                continue;

            Solution newSol = new();
            var reagentProducingAmount = anomaly.Stability * component.MaxReagentProducing * component.AccumulatedFrametime;
            if (anomaly.Severity >= 0.97) reagentProducingAmount *= component.SupercriticalReagentProducingModifier;

            newSol.AddReagent(component.ProducingReagent, reagentProducingAmount);
            _solutionContainer.TryAddSolution(component.Solution.Value, newSol); // TODO - the container is not fully filled.

            component.AccumulatedFrametime = 0;

            // The component will repaint the sprites of the object to match the current color of the solution,
            // if the RandomSprite component is hung correctly.

            // Ideally, this should be put into a separate component, but I suffered for 4 hours,
            // and nothing worked out for me. So for now it will be like this.
            if (component.NeedRecolor)
            {
                var color = producerSolution.GetColor(_prototypeManager);
                _light.SetColor(uid, color);
                if (TryComp<RandomSpriteComponent>(uid, out var randomSprite))
                {
                    foreach (var ent in randomSprite.Selected)
                    {
                        var state = randomSprite.Selected[ent.Key];
                        state.Color = color;
                        randomSprite.Selected[ent.Key] = state;
                    }
                    Dirty(uid, randomSprite);
                }
            }
        }
    }

    private void OnMapInit(Entity<ReagentProducerAnomalyComponent> entity, ref MapInitEvent args)
    {
        ChangeReagent(entity, 0.1f); //MapInit Reagent 100% change
    }

    // returns a random reagent based on a system of random weights.
    // First, the category is selected: The category has a minimum and maximum weight,
    // the current value depends on severity.
    // Accordingly, with the strengthening of the anomaly,
    // the chances of falling out of some categories grow, and some fall.
    //
    // After that, a random reagent in the selected category is selected.
    //
    // Such a system is made to control the danger and interest of the anomaly more.
    private string GetRandomReagentType(Entity<ReagentProducerAnomalyComponent> entity, float severity)
    {
        //Category Weight Randomization
        var currentWeightDangerous = MathHelper.Lerp(entity.Comp.WeightSpreadDangerous.X, entity.Comp.WeightSpreadDangerous.Y, severity);
        var currentWeightFun = MathHelper.Lerp(entity.Comp.WeightSpreadFun.X, entity.Comp.WeightSpreadFun.Y, severity);
        var currentWeightUseful = MathHelper.Lerp(entity.Comp.WeightSpreadUseful.X, entity.Comp.WeightSpreadUseful.Y, severity);

        var sumWeight = currentWeightDangerous + currentWeightFun + currentWeightUseful;
        var rnd = _random.NextFloat(0f, sumWeight);
        //Dangerous
        if (rnd <= currentWeightDangerous && entity.Comp.DangerousChemicals.Count > 0)
        {
            var reagent = _random.Pick(entity.Comp.DangerousChemicals);
            return reagent;
        }
        else rnd -= currentWeightDangerous;
        //Fun
        if (rnd <= currentWeightFun && entity.Comp.FunChemicals.Count > 0)
        {
            var reagent = _random.Pick(entity.Comp.FunChemicals);
            return reagent;
        }
        else rnd -= currentWeightFun;
        //Useful
        if (rnd <= currentWeightUseful && entity.Comp.UsefulChemicals.Count > 0)
        {
            var reagent = _random.Pick(entity.Comp.UsefulChemicals);
            return reagent;
        }
        //We should never end up here.
        //Maybe Log Error?
        return FallbackReagent;
    }
}
