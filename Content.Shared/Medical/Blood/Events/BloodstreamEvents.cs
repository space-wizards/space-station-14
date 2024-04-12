using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Medical.Blood.Components;

namespace Content.Shared.Medical.Blood.Events;


[ByRefEvent]
public record struct BloodstreamUpdatedEvent(Entity<BloodstreamComponent, SolutionContainerManagerComponent> Bloodstream);
