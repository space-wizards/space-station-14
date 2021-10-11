using Content.Server.Power.Components;
using Content.Server.PowerCell.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.Weapon
{
    /// <summary>
    /// Recharges the battery in a <see cref="ServerBatteryBarrelComponent"/>.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(BaseCharger))]
    public sealed class WeaponCapacitorChargerComponent : BaseCharger
    {
        public override string Name => "WeaponCapacitorCharger";

        public override bool IsEntityCompatible(IEntity entity)
        {
            return entity.TryGetComponent(out ServerBatteryBarrelComponent? battery) && battery.PowerCell != null ||
                   entity.TryGetComponent(out PowerCellSlotComponent? slot) && slot.HasCell;
        }

        protected override BatteryComponent? GetBatteryFrom(IEntity entity)
        {
            if (entity.TryGetComponent(out PowerCellSlotComponent? slot))
            {
                if (slot.Cell != null)
                {
                    return slot.Cell;
                }
            }

            if (entity.TryGetComponent(out ServerBatteryBarrelComponent? battery))
            {
                if (battery.PowerCell != null)
                {
                    return battery.PowerCell;
                }
            }

            return null;
        }
    }
}
