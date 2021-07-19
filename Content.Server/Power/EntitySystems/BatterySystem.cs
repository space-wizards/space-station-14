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

            SubscribeLocalEvent<BatteryComponent, NetworkBatteryPreSync>(PreSync);
            SubscribeLocalEvent<BatteryComponent, NetworkBatteryPostSync>(PostSync);
        }

        private void PreSync(EntityUid uid, BatteryComponent component, NetworkBatteryPreSync args)
        {
            var networkBattery = ComponentManager.GetComponent<PowerNetworkBatteryComponent>(uid);

            networkBattery.NetworkBattery.Capacity = component.MaxCharge;
            networkBattery.NetworkBattery.CurrentStorage = component.CurrentCharge;
        }

        private void PostSync(EntityUid uid, BatteryComponent component, NetworkBatteryPostSync args)
        {
            var networkBattery = ComponentManager.GetComponent<PowerNetworkBatteryComponent>(uid);

            component.CurrentCharge = networkBattery.NetworkBattery.CurrentStorage;
        }

        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BatteryComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
