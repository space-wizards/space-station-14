using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Item;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
///     Huge plants, with min yield, max potency, and dramatically increased chemical quantity.
/// </summary>
public sealed partial class PlantJumbo : EntityEffect
{
    int _chemicalMultiplier = 3; // Originally intended to be 4, but that exceeds the normal beaker size too easily.

    public override void Effect(EntityEffectBaseArgs args)
    {
        // If this is occurring on a plant, we're going to halve the yield on it
        // to make up for the increase in chemicals.
        var botanySystem = args.EntityManager.System<BotanySystem>();
        if (args.EntityManager.TryGetComponent<PlantHolderComponent>(args.TargetEntity, out var plantHolder)
            && plantHolder.Seed != null)
        {
            // Stacking this effect will create nonsense levels of chemicals, so only apply the multiplier once.
            // Plant chemical output is Clamp(Min, Min + (Potency / PotencyDivisor), Max), so all 3 values need scaled
            if (!plantHolder.Seed.Mutations.Any(m => m.GetType() == typeof(PlantJumbo)))
            {
                botanySystem.SetYield(plantHolder.Seed, 1);

                var chemicals = plantHolder.Seed.Chemicals;
                foreach (var c in chemicals.Keys)
                {
                    SeedChemQuantity scq = chemicals[c];
                    scq.Min *= _chemicalMultiplier;
                    scq.Max *= _chemicalMultiplier;
                    scq.PotencyDivisor /= _chemicalMultiplier;
                    chemicals[c] = scq;
                }
            }
        }

        if (!args.EntityManager.TryGetComponent<ProduceComponent>(args.TargetEntity, out var produce) || produce.Seed == null)
            return;

        // Makes the fruit take up lots of space.
        var itemSys = args.EntityManager.System<SharedItemSystem>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var item = args.EntityManager.GetComponent<ItemComponent>(args.TargetEntity);
        itemSys.SetSize(args.TargetEntity, prototypeManager.Index<ItemSizePrototype>("Huge"));

        // Jumbo overrides the actual potency and yield value, so we set them on the produce before it's harvested.
        // If you want multiple Jumbo plants with a higher yield, you have to gamble on re-mutating it each time
        botanySystem.SetPotency(produce.Seed, 100);
        botanySystem.SetYield(produce.Seed, 1);

        if (args.EntityManager.TryGetComponent<FoodComponent>(args.TargetEntity, out var food))
        {
            for (int t = 0; t < food.Trash.Count; t++)
                if (prototypeManager.HasIndex(food.Trash[t].Id + "Jumbo"))
                {
                    var foodSys = args.EntityManager.System<FoodSystem>();
                    foodSys.ChangeTrash(args.TargetEntity, food, food.Trash[t], new EntProtoId(food.Trash[t].Id + "Jumbo"));
                }
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "TODO";
    }
}
