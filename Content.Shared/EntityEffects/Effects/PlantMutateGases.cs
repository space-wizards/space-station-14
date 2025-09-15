using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     changes the gases that a plant or produce create.
/// </summary>
public sealed partial class PlantMutateExudeGasses : EventEntityEffect<PlantMutateExudeGasses>
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}

/// <summary>
///     changes the gases that a plant or produce consumes.
/// </summary>
public sealed partial class PlantMutateConsumeGasses : EventEntityEffect<PlantMutateConsumeGasses>
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
