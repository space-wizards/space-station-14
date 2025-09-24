using System.Linq;
using Content.Shared.Medical.Disease;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adds a random disease from the configured pool when the effect triggers.
/// </summary>
public sealed partial class CauseRandomDisease : EventEntityEffect<CauseRandomDisease>
{
    /// <summary>
    /// Pool of disease prototype IDs to pick from.
    /// </summary>
    [DataField(required: true)]
    public List<string> Diseases = default!;

    /// <summary>
    /// If true, the effect only applies when the metabolized scale equals 1 (full metabolism tick).
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
        // Show a comma-separated list of disease names via localization keys
        var names = Diseases.Select(id => Loc.GetString($"disease-name-{id}")).ToList();
        var joined = string.Join(", ", names);
        return Loc.GetString("reagent-effect-guidebook-chem-cause-random-disease", ("chance", Probability), ("diseases", joined));
    }
}
