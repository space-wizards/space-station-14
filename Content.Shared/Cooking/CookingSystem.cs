using Robust.Shared.Timing;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cooking;

public sealed partial class CookingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CookingComponent>();
        
        while (query.MoveNext(out var uid, out var cook))
        {
            if(cook.NextUpdate > curTime)
                continue;
            
            cook.NextUpdate = curTime + TimeSpan.FromSeconds(1f);
            
            // Check if it should be cooking and reached the needed temperature
            if (!cook.IsCooking
                || !TryComp<TemperatureComponent>(uid, out var temp) 
                && temp != null 
                && temp.Temperature < cook.MinimumTemperature) // there should be a check to see if the entity needs to be hot or cold to cook
                continue;
            
            cook.CookingTime += cook.CookingSpeed; // Add the value to the cooking time and check if it's done
            Log.Debug($"cook time progress {cook.CookingTime}");
            
            if (cook.CookingTime >= cook.TimeCookedThreshold) // this should instead check if the entity is raw or cooked, and use the respective threshold 
            {
                Log.Debug("its done! do thing");
                // Few things need to happen
                // Go trough all recipes and save the recipe with the highest amount of matching ingredients
                // If no recipe matches then simply advance the cooking state of the entity raw > cooked > burnt
            }
        }
    }

    /// <summary>
    /// Start/stop cooking timer when the entity is placed inside a valid cooking appliance
    /// </summary>
    public void TryStartCooking(EntityUid ent, string type)
    {
        if (!TryComp<CookingComponent>(ent, out var cookComp))
            return;
        
        // this should check for the appliances on the entity yaml and apply a speed penalty multiplier if none of them match 
        
        cookComp.IsCooking = true;
    }

    /// <summary>
    /// Stop cooking
    /// </summary>
    public void StopCooking(EntityUid ent)
    {
        if(!TryComp<CookingComponent>(ent, out var cookComp))
            return;

        cookComp.IsCooking = false;
    }

    // This should replace the food item and translate all the needed details like chemicals, quality, etc
    public void CookFood(EntityUid uid, EntProtoId food)
    {
        /*
        if(!TryComp<CookingComponent>(uid, out  _))
          return;

        var trans = Transform(uid);
        var coords = trans.Coordinates;
        var rot = trans.LocalRotation;
        QueueDel(uid);
        var newEnt = SpawnAtPosition(food, coords);
        */
    }
    
}