using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Light.EntitySystems
{
    public sealed class LitOnPoweredSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LitOnPoweredComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LitOnPoweredComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
        }

        private void OnPowerChanged(EntityUid uid, LitOnPoweredComponent component, ref PowerChangedEvent args)
        {
            if (EntityManager.TryGetComponent<PointLightComponent>(uid, out var light))
            {
                light.Enabled = args.Powered;
            }
        }

        private void OnPowerSupply(EntityUid uid, LitOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
        {
            if (EntityManager.TryGetComponent<PointLightComponent>(uid, out var light))
            {
                light.Enabled = args.Supply;
            }
        }
    }
}
