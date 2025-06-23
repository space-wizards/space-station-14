using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     Changes a plant into one of the species its able to mutate into.
/// </summary>
public sealed partial class PlantSpeciesChange : EventEntityEffect<PlantSpeciesChange>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
