
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Events;

[ByRefEvent]
public record struct SolutionUpdatedEvent(
    Entity<SolutionComponent> Solution,
    Entity<SolutionHolderComponent> Container,
    FixedPoint2 VolumeDelta);

[ByRefEvent]
public record struct SolutionAddedEvent(
    Entity<SolutionHolderComponent> Container,
    Entity<SolutionComponent> NewSolution);

[ByRefEvent]
public record struct SolutionRemovedEvent(
    Entity<SolutionHolderComponent> Container,
    Entity<SolutionComponent> SolutionToBeRemoved);
