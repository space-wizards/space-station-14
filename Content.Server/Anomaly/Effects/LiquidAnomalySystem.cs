using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Robust.Shared.Random;
using Content.Shared.Chemistry.Reagent;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);
        SubscribeLocalEvent<LiquidAnomalyComponent, AnomalySeverityChangedEvent>(OnSeverityChanged);
        SubscribeLocalEvent<LiquidAnomalyComponent, MapInitEvent>(OnMapInit);
    }
    private void OnMapInit(EntityUid uid, LiquidAnomalyComponent component, MapInitEvent args)
    {
        ChangeReagentType(uid, component);
    }

    private void OnPulse(EntityUid uid, LiquidAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        PulseScalableEffect(
            uid,
            component.MaxSolutionGeneration * args.Severity,
            component.MaxSolutionInjection * args.Severity,
            component.InjectRadius,
            component.Reagent);
    }

    private void OnSupercritical(EntityUid uid, LiquidAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        PulseScalableEffect(
            uid,
            component.SuperCriticalSolutionGeneration,
            component.SuperCriticalSolutionInjection,
            component.SuperCriticalInjectRadius,
            component.Reagent);
    }


    private void OnSeverityChanged(EntityUid uid, LiquidAnomalyComponent component, ref AnomalySeverityChangedEvent args)
    {
        //If after the severity change, its level has exceeded the threshold, the type of reagent changes, and the threshold increases.
        if (args.Severity >= component.NextChangeThreshold)
        {
            component.NextChangeThreshold += component.ReagentChangeStep;

            ChangeReagentType(uid, component);
            return;
        }
    }

    private void ChangeReagentType(EntityUid uid, LiquidAnomalyComponent component)
    {
        if (component.PossibleChemicals.Count == 0) return;

        var reagent = _random.Pick(component.PossibleChemicals);

        component.Reagent = reagent;
        var color = _proto.Index<ReagentPrototype>(reagent).SubstanceColor;
        _light.SetColor(uid, color);

        //Spawn Effect
        var uidXform = Transform(uid);
        Spawn("PuddleSparkle", uidXform.Coordinates);
        _audio.PlayPvs(component.ChangeSound, uid);
    }

    private void PulseScalableEffect(
        EntityUid uid,
        float solutionAmount,
        float maxInject,
        float injectRadius,
        string reagent)
    {
        //We get all the entity in the radius into which the reagent will be injected.
        var injectableQuery = GetEntityQuery<InjectableSolutionComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var allEnts = _lookup.GetComponentsInRange<InjectableSolutionComponent>(xform.MapPosition, injectRadius)
            .Select(x => x.Owner).ToList();

        //for each matching entity found
        foreach (var ent in allEnts)
        {
            if (Deleted(ent)) continue;
            if (!ent.IsValid()) continue;

            if (!_solutionContainerSystem.TryGetInjectableSolution(ent, out var injectable))
                continue;

            if (injectableQuery.TryGetComponent(ent, out var injEnt))
            {
                _solutionContainerSystem.TryAddReagent(ent, injectable, reagent, maxInject, out var accepted);

                //Spawn Effect
                var uidXform = Transform(ent);
                Spawn("PuddleSparkle", uidXform.Coordinates);
            }
        }

        //Create Puddle
        Solution solution = new();
        solution.AddReagent(reagent, solutionAmount);
        _puddleSystem.TrySpillAt(uid, solution, out _);
    }
}
