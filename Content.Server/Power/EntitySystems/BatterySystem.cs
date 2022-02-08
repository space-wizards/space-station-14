using Content.Server.Power.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    public class BatterySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NetworkBatteryPreSync>(PreSync);
            SubscribeLocalEvent<NetworkBatteryPostSync>(PostSync);
        }

        private void PreSync(NetworkBatteryPreSync ev)
        {
            foreach (var (netBat, bat) in EntityManager.EntityQuery<PowerNetworkBatteryComponent, BatteryComponent>())
            {
                netBat.NetworkBattery.Capacity = bat.MaxCharge;
                netBat.NetworkBattery.CurrentStorage = bat.CurrentCharge;
            }
        }

        private void PostSync(NetworkBatteryPostSync ev)
        {
            foreach (var (netBat, bat) in EntityManager.EntityQuery<PowerNetworkBatteryComponent, BatteryComponent>())
            {
                bat.CurrentCharge = netBat.NetworkBattery.CurrentStorage;
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var (comp, batt) in EntityManager.EntityQuery<BatterySelfRechargerComponent, BatteryComponent>())
            {
                if (!comp.AutoRecharge) continue;
                if (batt.IsFullyCharged) continue;
                batt.CurrentCharge += comp.AutoRechargeRate * frameTime;
            }
        }
    }
}
