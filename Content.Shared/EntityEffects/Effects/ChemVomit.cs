using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Forces you to vomit.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemVomit : EntityEffect
{
    /// How many units of thirst to add each time we vomit
    [DataField]
    public float ThirstAmount = -8f;
    /// How many units of hunger to add each time we vomit
    [DataField]
    public float HungerAmount = -8f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-vomit", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<ChemVomit>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
