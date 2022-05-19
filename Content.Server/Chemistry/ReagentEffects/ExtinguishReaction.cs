using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    [UsedImplicitly]
    public sealed class ExtinguishReaction : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out FlammableComponent? flammable)) return;

            var flammableSystem = EntitySystem.Get<FlammableSystem>();
            flammableSystem.Extinguish(args.SolutionEntity, flammable);
            flammableSystem.AdjustFireStacks(args.SolutionEntity, -1.5f * (float) args.Quantity, flammable);
        }
    }
}
