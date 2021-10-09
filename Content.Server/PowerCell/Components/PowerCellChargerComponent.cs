using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.PowerCell.Components
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

        public override bool IsEntityCompatible(IEntity entity)
        {
            return entity.HasComponent<BatteryComponent>();
        }

        protected override BatteryComponent GetBatteryFrom(IEntity entity)
        {
            return entity.GetComponent<BatteryComponent>();
        }
    }
}
