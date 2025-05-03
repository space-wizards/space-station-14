using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAffectGrowth : PlantAdjustAttribute<PlantAffectGrowth>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-growth";
}

