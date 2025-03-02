using Content.Shared.Database;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class FlammableReaction : EntityEffect
{
    [DataField]
    public float Multiplier = 0.05f;

    // The fire stack multiplier if fire stacks already exist on target, only works if 0 or greater
    [DataField]
    public float MultiplierOnExisting = -1f;

    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-flammable-reaction", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<FlammableReaction>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
