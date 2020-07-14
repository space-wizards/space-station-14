using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Power.Chargers
{
    /// <summary>
    /// Recharges an entity with a <see cref="BatteryComponent"/>.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(BaseCharger))]
    public sealed class PowerCellChargerComponent : BaseCharger
    {
        public override string Name => "PowerCellCharger";

        protected override bool IsEntityCompatible(IEntity entity)
        {
            return entity.HasComponent<BatteryComponent>();
        }

        protected override BatteryComponent GetBatteryFrom(IEntity entity)
        {
            return entity.GetComponent<BatteryComponent>();
        }
    }
}
