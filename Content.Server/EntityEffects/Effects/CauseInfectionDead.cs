// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using System.Linq;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class CauseInfectionDead : EntityEffect
{
    [DataField]
    public InfectionDeadStrainData StrainData = new InfectionDeadStrainData();
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cause-infection-dead", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;

        if (!args.EntityManager.System<InfectionDeadSystem>().IsInfectionPossible(args.TargetEntity))
            return;

        InfectionDeadStrainData? infectionData = null;

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            Console.WriteLine("ok2 ");
            var solution = reagentArgs.Source;

            if (solution == null)
                return;

            var contents = solution.Contents;

            foreach (var reagent in contents)
            {
                var dataList = reagent.Reagent.Data;
                if (dataList == null)
                    continue;

                infectionData = dataList.OfType<InfectionDeadStrainData>().FirstOrDefault();
                Console.WriteLine("ok3 ");
                Console.WriteLine(reagent.Reagent.Prototype);
            }
        }

        InfectionDeadComponent component = new InfectionDeadComponent(StrainData);

        if (infectionData != null)
        {
            component.StrainData = infectionData;
            Console.WriteLine("ok4");
        }

        entityManager.AddComponent(args.TargetEntity, component);
    }
}

