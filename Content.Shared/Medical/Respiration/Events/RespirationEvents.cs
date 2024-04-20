using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Medical.Respiration.Components;

namespace Content.Shared.Medical.Respiration.Events;



[ByRefEvent]
public record struct BreathAttemptEvent(
    Entity<RespiratorComponent> Respirator,
    bool Canceled = false);

[ByRefEvent]
public record struct BreatheEvent(
    Entity<RespiratorComponent> Respirator,
    Entity<SolutionComponent> AbsorptionSolutionEnt,
    Solution AbsorptionSolution,
    Entity<SolutionComponent> WasteSolutionEnt,
    Solution WasteSolution);

[ByRefEvent]
public record struct GetRespiratorTargetSolutionEvent(
    Entity<RespiratorComponent> Respirator,
    Entity<SolutionContainerManagerComponent?>? Target = null);
