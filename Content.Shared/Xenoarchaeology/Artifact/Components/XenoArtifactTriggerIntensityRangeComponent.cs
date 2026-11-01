using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.Conditions;

namespace Content.Shared.Xenoarchaeology.Artifact.Components;

/// <summary>
/// Component for keeping track of complexity for chains of artifact nodes and their triggers,
/// and aligning them with corresponding, more valuable effects as a resulting effect.
/// Budget is roughly estimated based on depth of node and 'Actual value'
/// of direct descendant effects and triggers picked.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedXenoArtifactSystem), typeof(HasArtifactIntensityInRangeCondition))]
public sealed partial class XenoArtifactTriggerIntensityRangeComponent : Component
{
    /// <summary>
    /// Minimum and maximum borders for intensity.
    /// </summary>
    [DataField(required: true)]
    public MinMax IntensityRange;

    /// <summary>
    /// Actual intensity for trigger or effect.
    /// </summary>
    [DataField(required: true)]
    public float ActualIntensity;
}
