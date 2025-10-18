using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Basically smoke and foam reactions.
/// </summary>
public sealed partial class ChemCleanBloodstream : EventEntityEffect<ChemCleanBloodstream>
{
    [DataField]
    public float CleanseRate = 3.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-clean-bloodstream", ("chance", Probability));
}
