using Content.Server.Botany.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Botany.Systems;

[ByRefEvent]
public readonly record struct OnPlantGrowEvent;

//May need to be PlantSystem, since this looks for PlantComponent.
public abstract class PlantGrowthSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] protected readonly IGameTiming _gameTiming = default!;

    public TimeSpan nextUpdate = TimeSpan.Zero;
    public TimeSpan updateDelay = TimeSpan.FromSeconds(15); //PlantHolder has a 15 second delay on cycles, but checks every 3 for sprite updates.

    public const float HydroponicsSpeedMultiplier = 1f;
    public const float HydroponicsConsumptionMultiplier = 2f;

    public override void Initialize()
    {
        base.Initialize();

        //This might be overcomplicating things. Maybe I should just create a new event here and
        //have each system listen for that instead?


        //List<EntitySystem> subsystems = new List<EntitySystem>();
        //Dictionary<Type, EntitySystem> subSystemDict = new Dictionary<Type, EntitySystem>();
        //foreach (var system in EntityManager.EntitySysManager.GetEntitySystemTypes())
        //{
        //    if (system is PlantGrowthSystem && system.Name != "PlantGrowthSystem")
        //    {
        //        subsystems.Add(system);

        //        //find component by name?
        //        EntityManager.getco
        //    }
        //}
    }

    // TO INVESTIGATE: can I figure out some way here to connect all the plant growth systems and components so
    // I don't have to give each one of those their own Update() method that listens for gameticks?
    // It would be much less code to do it here once instead of in each new system.
    // Maybe even if its forcing it by name, to prove its doable?

    public override void Update(float frameTime)
    {
        if (nextUpdate > _gameTiming.CurTime)
            return;

        //This does not work as a universal check. I do need a baseline PlantComponent.
        var query = EntityQueryEnumerator<PlantComponent>();
        while (query.MoveNext(out var uid, out var plantComponent))
        {
            int a = 1; //I want to see each entity once, not each component once.
            //Update(uid, unviableGrowthComponent);
            var plantGrow = new OnPlantGrowEvent();
            RaiseLocalEvent(uid, ref plantGrow);
        }
        nextUpdate = _gameTiming.CurTime + updateDelay;
    }




    public void AffectGrowth(int amount, PlantHolderComponent? component = null)
    {
        if (component == null || component.Seed == null)
            return;

        if (amount > 0)
        {
            if (component.Age < component.Seed.Maturation)
                component.Age += amount;
            else if (!component.Harvest && component.Seed.Yield <= 0f)
                component.LastProduce -= amount;
        }
        else
        {
            if (component.Age < component.Seed.Maturation)
                component.SkipAging++;
            else if (!component.Harvest && component.Seed.Yield <= 0f)
                component.LastProduce += amount;
        }
    }
}
