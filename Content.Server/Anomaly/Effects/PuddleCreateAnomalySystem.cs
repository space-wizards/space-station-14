using Content.Server.Anomaly.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Anomaly.Components;

namespace Content.Server.Anomaly.Effects;

/// <summary>
/// This component allows the anomaly to create puddles from SolutionContainer.
/// </summary>
public sealed class PuddleCreateAnomalySystem : EntitySystem
{
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PuddleCreateAnomalyComponent, AnomalyPulseEvent>(OnPulse);
        SubscribeLocalEvent<PuddleCreateAnomalyComponent, AnomalySupercriticalEvent>(OnSupercritical, before: new[] { typeof(InjectionAnomalySystem) });
    }

    private void OnPulse(Entity<PuddleCreateAnomalyComponent> entity, ref AnomalyPulseEvent args)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var sol, out _))
            return;

        var xform = Transform(entity.Owner);
        var puddleSol = _solutionContainer.SplitSolution(sol.Value, entity.Comp.MaxPuddleSize * args.Severity * args.PowerModifier);
        _puddle.TrySplashSpillAt(entity.Owner, xform.Coordinates, puddleSol, out _);
    }

    private void OnSupercritical(Entity<PuddleCreateAnomalyComponent> entity, ref AnomalySupercriticalEvent args)
    {
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var sol))
            return;

        var xform = Transform(entity.Owner);
        _puddle.TrySpillAt(xform.Coordinates, sol, out _);
    }
}
