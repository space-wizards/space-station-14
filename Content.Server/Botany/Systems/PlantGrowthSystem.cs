using Content.Server.Botany.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

[ByRefEvent]
public readonly record struct OnPlantGrowEvent;

/// <summary>
/// Base system for plant growth mechanics. Handles the core growth cycle and provides
/// common functionality for all plant growth systems.
/// </summary>
public abstract class PlantGrowthSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] protected readonly IGameTiming _gameTiming = default!;

    public TimeSpan nextUpdate = TimeSpan.Zero;
    public TimeSpan updateDelay = TimeSpan.FromSeconds(15); // PlantHolder has a 15 second delay on cycles

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        if (nextUpdate > _gameTiming.CurTime)
            return;

        // Query for plant holders that have seeds and are not dead
        var query = EntityQueryEnumerator<PlantHolderComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            // Only process plants that have seeds and are alive
            if (plantHolder.Seed == null || plantHolder.Dead)
                continue;

            // Check if it's time for this plant to grow
            if (_gameTiming.CurTime < plantHolder.LastCycle + plantHolder.CycleDelay)
                continue;

            var plantGrow = new OnPlantGrowEvent();
            RaiseLocalEvent(uid, ref plantGrow);
        }

        nextUpdate = _gameTiming.CurTime + updateDelay;
    }

    /// <summary>
    /// Affects the growth of a plant by modifying its age or production timing.
    /// </summary>
    public void AffectGrowth(int amount, PlantHolderComponent? component = null)
    {
        if (component == null || component.Seed == null)
            return;

        if (amount > 0)
        {
            if (component.Age < component.Seed.Maturation)
                component.Age += amount;
            else if (!component.Harvest && component.Seed.Yield > 0f)
                component.LastProduce -= amount;
        }
        else
        {
            if (component.Age < component.Seed.Maturation)
                component.SkipAging++;
            else if (!component.Harvest && component.Seed.Yield > 0f)
                component.LastProduce += amount;
        }
    }
}
