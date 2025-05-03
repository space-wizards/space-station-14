using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustMutationMod : PlantAdjustAttribute<PlantAdjustMutationMod>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-mod";
}
