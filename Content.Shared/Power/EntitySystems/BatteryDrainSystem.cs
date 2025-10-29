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
        var query = EntityQueryEnumerator<BatteryComponent, BatteryDrainComponent>();
        while (query.MoveNext(out var uid, out var battery, out var drain))
        {
            Entity<BatteryDrainComponent> ent = (uid, drain);
            if (VerifyEntity(ent))
            {
                if (_batterySystem.TryUseCharge(uid, drain.DrainAmount * frameTime, battery))
                {
                    //???
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
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        //If there is a power cell, it should use PowerCellDrain
        if (HasComp<PowerCellComponent>(ent))
            return false;

        if (!_toggle.IsActivated(ent.Owner))
            return false;

        return true;
    }
}
