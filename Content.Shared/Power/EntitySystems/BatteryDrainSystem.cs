using Content.Shared.Item.ItemToggle;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Robust.Shared.Timing;

namespace Content.Shared.Power.EntitySystems;
public sealed class BatteryDrainSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        //We get all the entities with Battery and BatteryDrain
        var query = EntityQueryEnumerator<BatteryComponent, BatteryDrainComponent>();
        while (query.MoveNext(out var uid, out var battery, out var drain))
        {
            Entity<BatteryDrainComponent> ent = (uid, drain);

            //Check for needed components / conditions
            if (VerifyEntity(ent))
            {
                //We drain
                if (_batterySystem.TryUseCharge(uid, drain.DrainAmount * frameTime, battery))
                {
                    //Not sure if necessary
                    Dirty(ent);
                }

            }
        }
    }


    /// <summary>
    /// Check if Entity has necessary conditions to continue
    /// </summary>
    /// <param name="ent"></param>
    /// <returns>Does the entity have the required conditions ?</returns>
    private bool VerifyEntity(Entity<BatteryDrainComponent> ent)
    {
        //If there is a power cell, it should use PowerCellDrain
        if (!HasComp<BatteryComponent>(ent) || HasComp<PowerCellComponent>(ent))
            return false;

        //If the item is turned off, don't drain
        if (!_toggle.IsActivated(ent.Owner))
            return false;

        return true;
    }
}
