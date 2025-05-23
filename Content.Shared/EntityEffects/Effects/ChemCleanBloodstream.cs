using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Basically smoke and foam reactions.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemCleanBloodstream : EntityEffect
{
    [DataField]
    public float CleanseRate = 3.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-clean-bloodstream", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<ChemCleanBloodstream>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
