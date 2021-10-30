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
            foreach (var (bat, netBat) in EntityManager.EntityQuery<BatteryComponent, PowerNetworkBatteryComponent>())
            {
                netBat.NetworkBattery.Capacity = bat.MaxCharge;
                netBat.NetworkBattery.CurrentStorage = bat.CurrentCharge;
            }
        }

        private void PostSync(NetworkBatteryPostSync ev)
        {
            foreach (var (bat, netBat) in EntityManager.EntityQuery<BatteryComponent, PowerNetworkBatteryComponent>())
            {
                bat.CurrentCharge = netBat.NetworkBattery.CurrentStorage;
            }
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<BatteryComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
