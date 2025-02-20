using Content.Server.DeadSpace.Drug.Components;
using Content.Server.DeadSpace.Drug;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;

namespace Content.Server.DeadSpace.Drug.Effects;

public sealed partial class DrugIntoxication : EntityEffect
{
    [DataField]
    public bool Innoculate;

    [DataField(required: true)]
    public float AddictionEffect = 0;

    [DataField]
    public float ToleranceEffect = 0;

    [DataField]
    public bool ScaleByQuantity;

    [DataField]
    public int DrugStrenght = 1;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Innoculate)
            return Loc.GetString("reagent-effect-guidebook-innoculate-drug-intoxication", ("chance", Probability));

        return Loc.GetString("reagent-effect-guidebook-drug-intoxication", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;

        var instantDrugAddication = entityManager.EnsureComponent<InstantDrugAddicationComponent>(args.TargetEntity);

        var scale = FixedPoint2.New(1);

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            scale = ScaleByQuantity ? reagentArgs.Quantity * reagentArgs.Scale : reagentArgs.Scale;
        }

        args.EntityManager.System<DrugAddicationSystem>().TakeDrug(
            args.TargetEntity,
            DrugStrenght,
            AddictionEffect * (float)scale,
            ToleranceEffect * (float)scale,
            instantDrugAddication);
    }
}

