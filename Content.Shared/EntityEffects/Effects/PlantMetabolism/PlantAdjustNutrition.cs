// using Content.Server.Botany.Systems;
using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustNutrition : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-nutrition";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantAdjustNutrition>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
