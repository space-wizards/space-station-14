namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustMutationLevel : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-level";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantAdjustMutationLevel>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
