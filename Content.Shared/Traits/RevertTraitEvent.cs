using Robust.Shared.Prototypes;

namespace Content.Shared.Traits;

/// <summary>
/// Raised on an entity when their traits are reverted.
/// </summary>
[ByRefEvent]
public record struct RevertTraitEvent(ProtoId<TraitPrototype> Trait, List<Type> Components)
{
}
