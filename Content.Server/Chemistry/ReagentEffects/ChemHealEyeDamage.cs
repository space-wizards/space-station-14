using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eye.Blinding;
using JetBrains.Annotations;

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

        public override void Effect(ReagentEffectArgs args)
        {
            args.EntityManager.EntitySysManager.GetEntitySystem<SharedBlindingSystem>().AdjustEyeDamage(args.SolutionEntity, Amount);
        }
    }
}
