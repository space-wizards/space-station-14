using Content.Shared.Changeling;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Funkystation.EntityEffects.Effects;

public sealed partial class AdjustLingChemicals : EntityEffect
{
    [DataField]
    public float Amount;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-ling-chemicals",
            ("chance", Probability),
            ("amount", Amount),
            ("deltasign", Amount >= 0 ? 1 : -1));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<ChangelingComponent>(args.TargetEntity, out var lingComp))
            return;

        float scale = 1f;
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            scale = reagentArgs.Scale.Float();
        }

        float chemicalChange = Amount * scale;
        lingComp.Chemicals = MathF.Max(0f, lingComp.Chemicals + chemicalChange);

        lingComp.Chemicals = MathF.Min(lingComp.Chemicals, lingComp.MaxChemicals);

        args.EntityManager.Dirty(args.TargetEntity, lingComp);
    }
}