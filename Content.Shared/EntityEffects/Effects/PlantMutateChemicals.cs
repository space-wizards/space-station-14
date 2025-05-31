using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     changes the chemicals available in a plant's produce
/// </summary>
public sealed partial class PlantMutateChemicals : EventEntityEffect<PlantMutateChemicals>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
