using Content.Server.Power.Components;
using Content.Server.PowerCell.Components;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
        [Dependency] private readonly IEntityManager _entMan = default!;

        public override string Name => "WeaponCapacitorCharger";

        public override bool IsEntityCompatible(EntityUid entity)
        {
            return _entMan.TryGetComponent(entity, out ServerBatteryBarrelComponent? battery) && battery.PowerCell != null ||
                   _entMan.TryGetComponent(entity, out PowerCellSlotComponent? slot) && slot.HasCell;
        }

        protected override BatteryComponent? GetBatteryFrom(EntityUid entity)
        {
            if (_entMan.TryGetComponent(entity, out PowerCellSlotComponent? slot))
            {
                if (slot.Cell != null)
                {
                    return slot.Cell;
                }
            }

            if (_entMan.TryGetComponent(entity, out ServerBatteryBarrelComponent? battery))
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
