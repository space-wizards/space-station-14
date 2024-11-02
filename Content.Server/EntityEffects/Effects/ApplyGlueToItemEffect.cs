using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Item;
using Content.Shared.ReagentOnItem;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ApplyGlueToItemEffect : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-bleed-amount", ("chance", Probability),
            ("deltasign", MathF.Sign(2)));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var reagentOnItemSys = args.EntityManager.EntitySysManager.GetEntitySystem<ReagentOnItemSystem>();

        if (args is not EntityEffectApplyArgs reagentArgs ||
            reagentArgs.Quantity <= 0 ||
            !args.EntityManager.TryGetComponent<ItemComponent>(args.TargetEntity, out var itemComp))
            return;

        args.EntityManager.EnsureComponent<SpaceGlueOnItemComponent>(args.TargetEntity, out var comp);

        reagentOnItemSys.ApplyReagentEffectToItem((args.TargetEntity, itemComp), reagentArgs.Reagent, reagentArgs.Quantity, comp);
    }
}
