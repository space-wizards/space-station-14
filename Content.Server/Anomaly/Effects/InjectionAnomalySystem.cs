using System.Linq;
using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;

namespace Content.Server.Anomaly.Effects;
/// <summary>
/// This component allows the anomaly to inject liquid from the SolutionContainer
/// into the surrounding entities with the InjectionSolution component
/// </summary>
///

/// <see cref="InjectionAnomalyComponent"/>
public sealed class InjectionAnomalySystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<InjectableSolutionComponent> _injectableQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<InjectionAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<InjectionAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical, before: new[] { typeof(SolutionContainerSystem) });

        _injectableQuery = GetEntityQuery<InjectableSolutionComponent>();
    }

    private void OnPulse(EntityUid uid, InjectionAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        PulseScalableEffect(uid, component, component.InjectRadius, component.MaxSolutionInjection * args.Severity);
    }

    private void OnSupercritical(EntityUid uid, InjectionAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        PulseScalableEffect(uid, component, component.SuperCriticalInjectRadius, component.SuperCriticalSolutionInjection);
    }

    private void PulseScalableEffect(EntityUid uid, InjectionAnomalyComponent component, float injectRadius, float maxInject)
    {
        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var sol))
            return;
        //We get all the entity in the radius into which the reagent will be injected.
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var allEnts = _lookup.GetComponentsInRange<InjectableSolutionComponent>(xform.MapPosition, injectRadius)
            .Select(x => x.Owner).ToList();

        //for each matching entity found
        foreach (var ent in allEnts)
        {
            if (!_solutionContainer.TryGetInjectableSolution(ent, out var injectable))
                continue;

            if (_injectableQuery.TryGetComponent(ent, out var injEnt))
            {
                var buffer = sol;
                _solutionContainer.TryTransferSolution(ent, injectable, buffer, maxInject);
                //Spawn Effect
                var uidXform = Transform(ent);
                Spawn(component.VisualEffectPrototype, uidXform.Coordinates);
            }
        }
    }

}
