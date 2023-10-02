using Content.Server.Anomaly.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Server.Fluids.EntitySystems;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This component allows the anomaly to create puddles from SolutionContainer.
/// </summary>
public sealed class PuddleCreateAnomalySystem : EntitySystem
{

    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<PuddleCreateAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<PuddleCreateAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical);

        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    private void OnPulse(EntityUid uid, PuddleCreateAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        PulseScalableEffect(uid, component, component.MaxPuddleSize * args.Severity);
    }
    private void OnSupercritical(EntityUid uid, PuddleCreateAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        PulseScalableEffect(uid, component, component.SuperCriticalPuddleSize);
    }

    private void PulseScalableEffect(EntityUid uid, PuddleCreateAnomalyComponent component, float reagentCount)
    {
        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var sol))
            return;

        var xform = _xformQuery.GetComponent(uid);
        var puddleSol = _solutionContainer.SplitSolution(uid, sol, reagentCount);
        _puddle.TrySplashSpillAt(uid, xform.Coordinates, puddleSol, out _);
    }
}
