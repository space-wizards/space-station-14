using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles removal of seeds on a plant.
/// </summary>

public sealed partial class PlantDestroySeeds : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantDestroySeeds>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-seeds-remove", ("chance", Probability));
}
