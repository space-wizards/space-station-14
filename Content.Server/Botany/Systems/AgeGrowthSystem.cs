using Content.Server.Botany.Components;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;
public sealed class AgeGrowthSystem : PlantGrowthSystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolderSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AgeGrowthComponent, OnPlantGrowEvent>(OnPlantGrow);
    }

    private void OnPlantGrow(EntityUid uid, AgeGrowthComponent component, OnPlantGrowEvent args)
    {
        PlantHolderComponent? holder = null;
        Resolve<PlantHolderComponent>(uid, ref holder);

        if (holder == null || holder.Seed == null || holder.Dead)
            return;

        // Advance plant age here.
        if (holder.SkipAging > 0)
            holder.SkipAging--;
        else
        {
            if (_random.Prob(0.8f))
            {
                holder.Age += (int)(1 * HydroponicsSpeedMultiplier);
                holder.UpdateSpriteAfterUpdate = true;
            }
        }

        if (holder.Age > holder.Seed.Lifespan)
        {
            holder.Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
            if (holder.DrawWarnings)
                holder.UpdateSpriteAfterUpdate = true;
        }
        else if (holder.Age < 0) // Revert back to seed packet!
        {
            var packetSeed = holder.Seed;
            // will put it in the trays hands if it has any, please do not try doing this
            _botany.SpawnSeedPacket(packetSeed, Transform(uid).Coordinates, uid);
            _plantHolderSystem.RemovePlant(uid, holder);
            holder.ForceUpdate = true;
            _plantHolderSystem.Update(uid, holder);
        }

        // If enough time has passed since the plant was harvested, we're ready to harvest again!
        if (holder.Seed.ProductPrototypes.Count > 0)
        {
            if (holder.Age > holder.Seed.Production)
            {
                if (holder.Age - holder.LastProduce > holder.Seed.Production && !holder.Harvest)
                {
                    holder.Harvest = true;
                    holder.LastProduce = holder.Age;
                }
            }
            else
            {
                if (holder.Harvest)
                {
                    holder.Harvest = false;
                    holder.LastProduce = holder.Age;
                }
            }
        }
    }
}
