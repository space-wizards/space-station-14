using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlantPhalanximine : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantPhalanximine>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-plant-phalanximine", ("chance", Probability));
}
