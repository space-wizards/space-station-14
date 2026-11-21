using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.Conditions;

/// <summary>
/// Condition that will be successful only if intensity value from context by <see cref="IntensityContextKey"/> key
/// will be between min and max of <see cref="XenoArtifactTriggerIntensityRangeComponent.IntensityRange"/> (borders included).
/// </summary>
public sealed partial class HasArtifactIntensityInRangeCondition : EntityTableCondition
{
    /// <summary>
    /// Key for <see cref="EntityTableContext"/> that can be used to hold current intensity value,
    /// that should be compared against intensity ranges.
    /// </summary>
    public static readonly EntityTableContextKey<float> IntensityContextKey = new("Intensity");

    /// <inheritdoc/>
    protected override bool EvaluateImplementation(
        EntityTableSelector root,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        if (root is not EntSelector entSelector)
            return false;

        if (!ctx.TryGetData(IntensityContextKey, out var intensity))
            return false;

        MinMax? range = null;
        var entityPrototype = proto.Index(entSelector.Id);
        if (entityPrototype.TryComp(out XenoArtifactTriggerIntensityRangeComponent? triggerComp, entMan.ComponentFactory))
        {
            range = triggerComp.IntensityRange;
        }
        else if (entityPrototype.TryComp(out XenoArtifactNodeComponent? nodeComp, entMan.ComponentFactory))
        {
            range = nodeComp.IntensityRange;
        }

        return range.HasValue && intensity >= range.Value.Min && intensity <= range.Value.Max;
    }
}
