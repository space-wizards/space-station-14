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

    public override void Initialize()
    {
        SubscribeLocalEvent<PuddleCreateAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<PuddleCreateAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical, before: new[] { typeof(InjectionAnomalySystem) });
    }

    private void OnPulse(EntityUid uid, PuddleCreateAnomalyComponent component, ref AnomalyPulseEvent args)
    {
        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var sol))
            return;

        var xform = Transform(uid);
        var puddleSol = _solutionContainer.SplitSolution(uid, sol, component.MaxPuddleSize * args.Severity);
        _puddle.TrySplashSpillAt(uid, xform.Coordinates, puddleSol, out _);
    }
    private void OnSupercritical(EntityUid uid, PuddleCreateAnomalyComponent component, ref AnomalySupercriticalEvent args)
    {
        if (!_solutionContainer.TryGetSolution(uid, component.Solution, out var sol))
            return;
        var buffer = sol;
        var xform = Transform(uid);
        _puddle.TrySpillAt(xform.Coordinates, buffer, out _);
    }
}
