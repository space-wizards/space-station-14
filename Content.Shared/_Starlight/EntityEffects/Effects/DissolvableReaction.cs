using JetBrains.Annotations;
using Content.Shared.EntityEffects;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Content.Shared.Starlight.EntityEffects.Components;
using Content.Shared.Starlight.EntityEffects.EntitySystems;
using Content.Shared.Starlight.EntityEffects.Components;

namespace Content.Shared.Starlight.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class DissolvableReaction : EntityEffect
{
    [DataField]
    public float Multiplier = 0.05f;

    [DataField]
    public float MultiplierOnExisting = -1f;

    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-dissolvable-reaction", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out DissolvableComponent? dissolvable))
            return;

        // Sets the multiplier for FireStacks to MultiplierOnExisting is 0 or greater and target already has FireStacks
        var multiplier = dissolvable.DissolveStacks != 0f && MultiplierOnExisting >= 0 ? MultiplierOnExisting : Multiplier;
        var quantity = 1f;
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            quantity = reagentArgs.Quantity.Float();
            reagentArgs.EntityManager.System<SharedDissolvableSystem>().AdjustDissolveStacks(args.TargetEntity, quantity * multiplier, dissolvable);
            
            var coordinates = reagentArgs.EntityManager.System<SharedTransformSystem>().GetMapCoordinates(args.TargetEntity);
            if (reagentArgs.EntityManager.System<EntityLookupSystem>().GetEntitiesInRange<ThermiteComponent>(coordinates, 1f).Count == 0)
                reagentArgs.EntityManager.Spawn("ThermiteEntity", coordinates);
            
            if (reagentArgs.Reagent != null)
                reagentArgs.Source?.RemoveReagent(reagentArgs.Reagent.ID, reagentArgs.Quantity);
        }
        else
        {
            args.EntityManager.System<SharedDissolvableSystem>().AdjustDissolveStacks(args.TargetEntity, multiplier, dissolvable);
            
            var coordinates = args.EntityManager.System<SharedTransformSystem>().GetMapCoordinates(args.TargetEntity);
            if (args.EntityManager.System<EntityLookupSystem>().GetEntitiesInRange<ThermiteComponent>(coordinates, 1f).Count == 0)
                args.EntityManager.Spawn("ThermiteEntity", coordinates);
        }
    }
}