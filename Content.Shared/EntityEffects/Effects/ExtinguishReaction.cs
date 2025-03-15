using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class ExtinguishReaction : EntityEffect
{
    /// <summary>
    ///     Amount of firestacks reduced.
    /// </summary>
    [DataField]
    public float FireStacksAdjustment = -1.5f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-extinguish-reaction", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<ExtinguishReaction>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
