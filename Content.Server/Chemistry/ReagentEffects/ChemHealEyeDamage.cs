using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eye.Blinding;
using Content.Shared.Eye.Blinding.Systems;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Heal or apply eye damage
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemHealEyeDamage : ReagentEffect
    {
        /// <summary>
        /// How much eye damage to add.
        /// </summary>
        [DataField("amount")]
        public int Amount = -1;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f) // huh?
                return;

            args.EntityManager.EntitySysManager.GetEntitySystem<BlindableSystem>().AdjustEyeDamage(args.SolutionEntity, Amount);
        }
    }
}
