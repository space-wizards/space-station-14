namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustHealth : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-health";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantAdjustHealth>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}

