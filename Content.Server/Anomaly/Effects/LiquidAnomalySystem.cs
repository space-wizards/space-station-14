using System.Linq;
using System.Numerics;
using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Reagent;
using Content.Server.Construction.Completions;
using Robust.Server.GameObjects;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Effects;
/// <summary>
/// This component allows the anomaly to chase a random instance of the selected type component within a radius.
/// </summary>
public sealed class LiquidAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<LiquidAnomalyComponent, MapInitEvent>(OnMapInit);
    }
    private void OnMapInit(EntityUid uid, LiquidAnomalyComponent component, MapInitEvent args)
    {
        var rndIndex = _random.Next(0, component.PossibleChemicals.Count);
        var selectedReagent = component.PossibleChemicals[rndIndex];
        component.Reagent = selectedReagent;
        Log.Debug("Я появляюсь! И я работаю с веществом: " + component.Reagent);

        var color = _proto.Index<ReagentPrototype>(component.Reagent).SubstanceColor;
        _light.SetColor(uid, color);
    }

    private void OnSupercritical(EntityUid uid, LiquidAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {

    }

    private void OnPulse(EntityUid uid, LiquidAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        //We get all the containers in the radius into which the reagent will be injected.
        var injectableQuery = GetEntityQuery<InjectableSolutionComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var allEnts = _lookup.GetComponentsInRange<InjectableSolutionComponent>(xform.MapPosition, component.InjectRadius)
            .Select(x => x.Owner).ToList();

        Log.Debug("Радиус впрыска: " + component.InjectRadius);

        //We calculate how much reagent we will inject into each container. 
        var solutionAmount = component.MaxSolutionPerPulse * args.Severity;
        var solutionSplittedAmount = solutionAmount / (allEnts.Count + 1);

        Log.Debug("Найдено " + allEnts.Count + " целей для вспрыска. Объем для каждой = " + solutionSplittedAmount);
        
        foreach (var ent in allEnts)
        {
            if (!ent.IsValid()) continue;

            Log.Debug("Вспрыск в " + ent.ToString());

            if (!_solutionContainerSystem.TryGetInjectableSolution(ent, out var injectable))
            {
                Log.Debug("что-то с компонентом не так");
                continue;
            }


            if (injectableQuery.TryGetComponent(ent, out var injEnt))
            {
                _solutionContainerSystem.TryAddReagent(ent, injectable, component.Reagent, solutionSplittedAmount, out var accepted);
                //TODO излишки выливаются на пол в ту же клетку
            }
        }
    }
}
