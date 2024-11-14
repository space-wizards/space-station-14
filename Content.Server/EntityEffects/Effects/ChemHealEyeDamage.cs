using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Heal or apply eye damage
/// </summary>
[UsedImplicitly]
public sealed partial class ChemHealEyeDamage : EntityEffect
{
    /// <summary>
    /// How much eye damage to add.
    /// </summary>
    [DataField]
    public int Amount = -1;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cure-eye-damage", ("chance", Probability), ("deltasign", MathF.Sign(Amount)));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f) // huh?
                return;

        args.EntityManager.EntitySysManager.GetEntitySystem<BlindableSystem>().AdjustEyeDamage(args.TargetEntity, Amount);
    }
}
