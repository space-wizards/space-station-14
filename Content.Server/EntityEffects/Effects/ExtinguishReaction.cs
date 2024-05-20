using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ExtinguishReaction : EntityEffect
    {
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-extinguish-reaction", ("chance", Probability));

        public override void Effect(EntityEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.TargetEntity, out FlammableComponent? flammable)) return;

            var flammableSystem = EntitySystem.Get<FlammableSystem>();
            flammableSystem.Extinguish(args.TargetEntity, flammable);
            flammableSystem.AdjustFireStacks(args.TargetEntity, -1.5f * (float) args.Quantity, flammable);
        }
    }
}
