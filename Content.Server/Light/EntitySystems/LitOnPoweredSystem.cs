using Content.Server.Light.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Light.EntitySystems
{
    public sealed partial class LitOnPoweredSystem : EntitySystem
    {
        [Dependency] private SharedPointLightSystem _lights = default!;
        [Dependency] private PowerReceiverSystem _powerReceiver = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<LitOnPoweredComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<LitOnPoweredComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<LitOnPoweredComponent, PowerNetBatterySupplyEvent>(OnPowerSupply);
        }

        private void OnMapInit(EntityUid uid, LitOnPoweredComponent component, MapInitEvent args)
        {
            _lights.SetEnabled(uid, _powerReceiver.IsPowered(uid));
        }

        private void OnPowerChanged(EntityUid uid, LitOnPoweredComponent component, ref PowerChangedEvent args)
        {
            _lights.SetEnabled(uid, args.Powered);
        }

        private void OnPowerSupply(EntityUid uid, LitOnPoweredComponent component, ref PowerNetBatterySupplyEvent args)
        {
            _lights.SetEnabled(uid, args.Supply);
        }
    }
}
