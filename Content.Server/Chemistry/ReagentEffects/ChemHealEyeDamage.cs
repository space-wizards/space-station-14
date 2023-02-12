using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Heal eye damage (or deal)
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemHealEyeDamage : ReagentEffect
    {
        /// <summary>
        /// How much eye damage to remove.
        /// </summary>
        [DataField("amount")]
        public int Amount = -1;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-cure-eye-damage", ("chance", Probability), ("deltasign", MathF.Sign(Amount)));

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            args.EntityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>().AdjustEyeDamage(args.SolutionEntity, Amount);
        }
    }
}
