using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     Upgrades a plant's harvest type.
/// </summary>
public sealed partial class PlantMutateHarvest : EventEntityEffect<PlantMutateHarvest>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
