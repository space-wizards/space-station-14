using Content.Server.Light.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Light.EntitySystems
{
    public sealed class LitOnPoweredSystem : EntitySystem
    {
        [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LitOnPoweredComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LitOnPoweredComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
        }

        private void OnPowerChanged(EntityUid uid, LitOnPoweredComponent component, ref PowerChangedEvent args)
        {
            if (TryComp<PointLightComponent>(uid, out var light))
                _pointLight.SetEnabled(uid, args.Powered, light);
        }

        private void OnPowerSupply(EntityUid uid, LitOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
        {
            if (TryComp<PointLightComponent>(uid, out var light))
                _pointLight.SetEnabled(uid, args.Supply, light);
        }
    }
}
