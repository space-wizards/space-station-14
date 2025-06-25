using Content.Shared.Atmos;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects
{
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
            var ev = new ExtinguishEvent
            {
                FireStacksAdjustment = FireStacksAdjustment,
            };

            if (args is EntityEffectReagentArgs reagentArgs)
            {
                ev.FireStacksAdjustment *= (float)reagentArgs.Quantity;
            }

            args.EntityManager.EventBus.RaiseLocalEvent(args.TargetEntity, ref ev);
        }
    }
}
