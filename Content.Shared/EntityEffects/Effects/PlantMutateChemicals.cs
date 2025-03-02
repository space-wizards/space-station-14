using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     changes the chemicals available in a plant's produce
/// </summary>
public sealed partial class PlantMutateChemicals : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<PlantMutateChemicals>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
