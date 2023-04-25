using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    public sealed class PlantShiftTRA : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            plantHolder.EnsureUniqueSeed(args.SolutionEntity, plantHolderComp);

            if (plantHolderComp.Seed == null)
                return;

            var oldTra = plantHolderComp.Seed.TRA;

            plantHolderComp.Seed.TRA = new(
                Math.Clamp(oldTra.T += (int)Amount, 0, 9),
                Math.Clamp(oldTra.R += (int)Amount, 0, 9),
                Math.Clamp(oldTra.A += (int)Amount, 0, 9)
            );
        }
    }
}
