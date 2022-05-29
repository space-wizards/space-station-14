using Content.Shared.Chemistry.Reagent;
using Content.Server.Disease;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemCureDisease : ReagentEffect
    {
        /// <summary>
        /// Chance it has each tick to cure a disease, between 0 and 1
        /// </summary>
        [DataField("cureChance")]
        public float CureChance = 0.15f;

        public override void Effect(ReagentEffectArgs args)
        {
            var ev = new CureDiseaseAttemptEvent(CureChance);
            args.EntityManager.EventBus.RaiseLocalEvent(args.SolutionEntity, ev, false);
        }
    }
}
