using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Sprite;
using Robust.Server.GameObjects;

namespace Content.Server.Anomaly.Effects;
/// <summary>
/// This component allows the anomaly to generate a random type of reagent in the specified SolutionContainer.
/// With the increasing severity of the anomaly, the type of reagent produced may change.
/// The higher the severity of the anomaly, the higher the chance of dangerous or useful reagents.
/// </summary>

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

    //Other:
    //All reagents that exist in the game, with the exception of those prescribed in other lists and the blacklist.
    //They have low chances of appearing due to the fact that most things are boring and do not bring a
    //significant effect on the game.

    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AnomalySystem _anomaly = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ReagentProducerAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<ReagentProducerAnomalyComponent, MapInitEvent>(OnMapInit);

    }

    //reagent realtime generation
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ReagentProducerAnomalyComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.AccumulatedFrametime += frameTime;

            if (component.AccumulatedFrametime < component.UpdateInterval)
                continue;

            if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var producerSol))
                continue;

            Solution newSol = new();
            newSol.AddReagent(component.ProducingReagent, component.RealReagentProducing * component.AccumulatedFrametime);
            _solutionContainer.TryAddSolution(uid, producerSol, newSol); //TO DO - the container is not fully filled. 

            component.AccumulatedFrametime = 0;

            /// <summary>
            /// The component will repaint the sprites of the object to match the current color of the solution,
            /// if the RandomSprite component is hung correctly.
            /// Ideally, this should be put into a separate component, but I suffered for 4 hours,
            /// and nothing worked out for me. So for now it will be like this.
            /// </summary>
            if (component.NeedRecolor)
            {
                var color = producerSol.GetColor(_prototypeManager);
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

    private void OnMapInit(EntityUid uid, ReagentProducerAnomalyComponent component, MapInitEvent args)
    {
        _anomaly.ChangeAnomalyStability(uid, 0.01f); //Very bad code: I do not know how to get severity to call a "GetRandomReagentType" function here
    }

    private void OnSeverityChanged(EntityUid uid, ReagentProducerAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        component.RealReagentProducing = component.MaxReagentProducing * args.Severity;
        if (args.Severity >= 0.95)
            component.RealReagentProducing *= component.SupecriticalReagentProducingModifier;
        //If after the severity change, its level has exceeded the threshold, the type of reagent changes, and the threshold increases.
        if (args.Severity >= component.NextChangeThreshold)
        {
            component.NextChangeThreshold = args.Severity + component.ReagentChangeStep;

            var reagent = GetRandomReagentType(uid, component, ref args);
            component.ProducingReagent = reagent;
            return;
        }
    }

    /// <summary>
    /// returns a random reagent based on a system of random weights.
    /// First, the category is selected: The category has a minimum and maximum weight,
    /// the current value depends on severity.
    /// Accordingly, with the strengthening of the anomaly,
    /// the chances of falling out of some categories grow, and some fall.
    ///
    /// After that, a random reagent in the selected category is selected.
    ///
    /// Such a system is made to control the danger and interest of the anomaly more.
    ///
    /// ATTENTION! All new reagents appearing in the game will automatically appear in the "Other" category
    /// </summary>
    private string GetRandomReagentType(EntityUid uid, ReagentProducerAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        //Category Weight Randomization
        var currentWeightDangerous = Lerp(component.WeightSpreadDangerous.X, component.WeightSpreadDangerous.Y, args.Severity);
        var currentWeightFun = Lerp(component.WeightSpreadFun.X, component.WeightSpreadFun.Y, args.Severity);
        var currentWeightUseful = Lerp(component.WeightSpreadUseful.X, component.WeightSpreadUseful.Y, args.Severity);
        var currentWeightOther = Lerp(component.WeightSpreadOther.X, component.WeightSpreadOther.Y, args.Severity);

        var sumWeight = currentWeightDangerous + currentWeightFun + currentWeightUseful + currentWeightOther;
        var rnd = _random.NextFloat(0f, sumWeight);


        if (rnd <= currentWeightDangerous && component.DangerousChemicals.Count > 0)
        {
            var reagent = _random.Pick(component.DangerousChemicals);
            return reagent;
        }
        else rnd -= currentWeightDangerous;

        if (rnd <= currentWeightFun && component.FunChemicals.Count > 0)
        {
            var reagent = _random.Pick(component.FunChemicals);
            return reagent;
        }
        else rnd -= currentWeightFun;

        if (rnd <= currentWeightUseful && component.UsefulChemicals.Count > 0)
        {
            var reagent = _random.Pick(component.UsefulChemicals);
            return reagent;
        }
        else //Pickup other
        {
            var allReagents = _proto.EnumeratePrototypes<ReagentPrototype>().Select(proto => proto.ID).ToHashSet();

            foreach (var chem in component.BlacklistChemicals)
            {
                allReagents.Remove(chem);
            }
            foreach (var chem in component.DangerousChemicals)
            {
                allReagents.Remove(chem);
            }
            foreach (var chem in component.FunChemicals)
            {
                allReagents.Remove(chem);
            }
            foreach (var chem in component.UsefulChemicals)
            {
                allReagents.Remove(chem);
            }
            if (allReagents.Count == 0) return "Water"; // Error catcher

            var reagent = _random.Pick(allReagents);
            return reagent;
        }
    }
    private float Lerp(float a, float b, float t) //maybe this function is in the engine, but I didn't find it
    {
        t = (t < 0) ? 0 : (t > 1) ? 1 : t;
        return a + (b - a) * t;
    }
}
