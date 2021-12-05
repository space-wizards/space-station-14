using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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

        public override bool IsEntityCompatible(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<BatteryComponent>(entity);
        }

        protected override BatteryComponent GetBatteryFrom(EntityUid entity)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponent<BatteryComponent>(entity);
        }
    }
}
