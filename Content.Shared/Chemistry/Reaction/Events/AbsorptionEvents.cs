using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction.Components;
using Content.Shared.Chemistry.Reaction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry.Reaction.Events;

[ByRefEvent]
public record struct ChemicalAbsorbAttemptEvent(
    AbsorptionPrototype Reaction,
    Entity<SolutionComponent> Solution,
    Entity<ChemicalAbsorberComponent> Absorber,
    bool Canceled = false);


[ByRefEvent]
public record struct ChemicalAbsorbedEvent(
    AbsorptionPrototype Reaction,
    Entity<SolutionComponent> Solution,
    Entity<ChemicalAbsorberComponent> Absorber);
