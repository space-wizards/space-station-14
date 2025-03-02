using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAffectGrowth : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-growth";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantAffectGrowth>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}

