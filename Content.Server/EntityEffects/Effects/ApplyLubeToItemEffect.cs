using Content.Shared.EntityEffects;
using Content.Shared.Item;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.ReagentOnItem;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ApplyLubeToItemEffect : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var reagentOnItemSys = args.EntityManager.EntitySysManager.GetEntitySystem<ReagentOnItemSystem>();
        var nameMod = args.EntityManager.EntitySysManager.GetEntitySystem<NameModifierSystem>();

        if (args is not EntityEffectApplyArgs reagentArgs ||
            reagentArgs.Quantity <= 0 ||
            !args.EntityManager.TryGetComponent<ItemComponent>(args.TargetEntity, out var itemComp))
            return;

        args.EntityManager.EnsureComponent<SpaceLubeOnItemComponent>(args.TargetEntity, out var comp);

        var result = reagentOnItemSys.ApplyReagentEffectToItem((args.TargetEntity, itemComp), reagentArgs.Reagent, reagentArgs.Quantity, comp);

        if (!result)
        {
            args.EntityManager.RemoveComponent<SpaceLubeOnItemComponent>(args.TargetEntity);
            nameMod.RefreshNameModifiers((args.TargetEntity, null));
        }
    }
}
