#nullable enable
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents.PowerReceiverUsers
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

        protected override bool IsEntityCompatible(IEntity entity)
        {
            return entity.HasComponent<ServerBatteryBarrelComponent>();
        }

        protected override BatteryComponent GetBatteryFrom(IEntity entity)
        {
            return entity.GetComponent<ServerBatteryBarrelComponent>().PowerCell;
        }
    }
}
