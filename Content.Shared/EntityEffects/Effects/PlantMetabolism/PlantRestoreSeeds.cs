using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles restoral of seeds on a plant.
/// </summary>
public sealed partial class PlantRestoreSeeds : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantRestoreSeeds>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-seeds-add", ("chance", Probability));
}
