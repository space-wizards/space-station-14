using Content.Server.Medical;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Forces you to vomit.
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemVomit : ReagentEffect
    {
        /// How many units of satiation to add each time we vomit
        [DataField]
        public Dictionary<ProtoId<SatiationTypePrototype>, float> SatiationAmount = new() {
            { "Hunger", -8f },
            { "Thirst", -8f },
        };

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-chem-vomit", ("chance", Probability));

        public override void Effect(ReagentEffectArgs args)
        {
            if (args.Scale != 1f)
                return;

            var vomitSys = args.EntityManager.EntitySysManager.GetEntitySystem<VomitSystem>();

            vomitSys.Vomit(args.SolutionEntity, SatiationAmount);
        }
    }
}
