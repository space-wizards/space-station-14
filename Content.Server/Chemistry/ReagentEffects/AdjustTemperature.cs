using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    public sealed partial class AdjustTemperature : ReagentEffect
    {
        [DataField]
        public float Amount;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-adjust-temperature",
                ("chance", Probability),
                ("deltasign", MathF.Sign(Amount)),
                ("amount", MathF.Abs(Amount)));

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out TemperatureComponent? temp))
            {
                var sys = args.EntityManager.EntitySysManager.GetEntitySystem<TemperatureSystem>();
                var amount = Amount;

                amount *= args.Scale;

                sys.ChangeHeat(args.SolutionEntity, amount, true, temp);
            }
        }
    }
}
