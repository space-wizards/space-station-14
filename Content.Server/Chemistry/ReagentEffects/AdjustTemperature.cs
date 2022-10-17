using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects
{
    public sealed class AdjustTemperature : ReagentEffect
    {
        [DataField("amount")]
        public float Amount;

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out TemperatureComponent? temp))
            {
                var sys = args.EntityManager.EntitySysManager.GetEntitySystem<TemperatureSystem>();
                var amount = Amount;

                if (args.MetabolismEffects != null)
                    amount *= (float) (args.Quantity / args.MetabolismEffects.MetabolismRate);

                sys.ChangeHeat(args.SolutionEntity, amount, true, temp);
            }
        }
    }
}
