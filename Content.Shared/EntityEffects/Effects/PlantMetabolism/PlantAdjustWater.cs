using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustWater : PlantAdjustAttribute<PlantAdjustWater>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-water";
}

