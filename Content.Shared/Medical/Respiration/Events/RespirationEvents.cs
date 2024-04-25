using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Medical.Respiration.Components;

namespace Content.Shared.Medical.Respiration.Events;



[ByRefEvent]
public record struct BreathAttemptEvent(
    Entity<LungsComponent> Lungs,
    bool Canceled = false);

[ByRefEvent]
public record struct BreatheEvent(
    Entity<LungsComponent> Lungs,
    Entity<SolutionComponent> AbsorptionSolutionEnt,
    Solution AbsorptionSolution,
    Entity<SolutionComponent> WasteSolutionEnt,
    Solution WasteSolution);
