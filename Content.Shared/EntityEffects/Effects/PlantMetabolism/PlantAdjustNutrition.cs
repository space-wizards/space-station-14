// using Content.Server.Botany.Systems;
using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustNutrition : PlantAdjustAttribute<PlantAdjustNutrition>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-nutrition";
}
