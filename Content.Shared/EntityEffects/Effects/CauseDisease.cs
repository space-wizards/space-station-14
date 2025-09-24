using Content.Shared.Medical.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds a specific disease to the target when a reagent effect triggers.
/// </summary>
public sealed partial class CauseDisease : EventEntityEffect<CauseDisease>
{
    /// <summary>
    /// Disease prototype ID to infect the target with.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
    public string Disease = default!;

    /// <summary>
    /// If true, the effect only applies when the metabolized scale equals 1 (full metabolism tick).
    /// Mirrors behavior of some existing chemistry effects.
    /// </summary>
    [DataField]
    public bool RequireFullScale = true;

    /// <summary>
    /// If true, the effect will be skipped when the target already has at least one active disease.
    /// </summary>
    [DataField]
    public bool SkipIfAlreadyDiseased = false;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var key = $"disease-name-{Disease}";
        var name = Loc.GetString(key);
        return Loc.GetString("reagent-effect-guidebook-chem-cause-disease", ("chance", Probability), ("disease", name));
    }
}
