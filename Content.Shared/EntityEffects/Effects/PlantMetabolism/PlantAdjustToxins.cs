using JetBrains.Annotations;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustToxins : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-toxins";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantAdjustToxins>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}

